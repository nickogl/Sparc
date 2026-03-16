using Microsoft.Extensions.DependencyInjection;
using Sparc.IO;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Sparc;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// Unfortunately we have to make these helpers public to be able to call them
// from our dynamic module. This is the most pragmatic approach without needing
// to either create another library or generate async state machines ourselves.
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public static class ClientProxyFactoryHelpers
{
	internal readonly static Lock ModuleBuilderLock = new();
	internal readonly static ModuleBuilder ModuleBuilder =
		AssemblyBuilder
			.DefineDynamicAssembly(new("Sparc.ClientProxy"), AssemblyBuilderAccess.Run)
			.DefineDynamicModule("Sparc.ClientProxy");

	internal readonly static MethodInfo GetTypeFromHandleMethod =
		typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), [typeof(RuntimeTypeHandle)])!;
	internal readonly static MethodInfo GetRequiredServiceMethod =
		typeof(ServiceProviderServiceExtensions)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Single(m =>
				m.Name == nameof(ServiceProviderServiceExtensions.GetRequiredService) &&
				m.IsGenericMethodDefinition &&
				m.GetParameters().Length == 1);
	internal readonly static ConstructorInfo ObjectConstructor =
		typeof(object).GetConstructor(Type.EmptyTypes)!;
	internal readonly static ConstructorInfo PayloadWriterConstructor =
		typeof(PayloadWriter).GetConstructor([typeof(int)])!;

	internal readonly static MethodInfo GetParameterReaderOrWriterMethod =
		typeof(ClientProxyFactoryHelpers).GetMethod(nameof(ResolveWriterService))!;
	internal readonly static MethodInfo WriteOperationIdMethod =
		typeof(ClientProxyFactoryHelpers).GetMethod(nameof(WriteOperationId))!;
	internal readonly static MethodInfo WrappedSendMethod =
		typeof(ClientProxyFactoryHelpers).GetMethod(nameof(WrappedSendAsync))!;
	internal readonly static MethodInfo BroadcastMethod =
		typeof(ClientProxyFactoryHelpers).GetMethod(nameof(BroadcastAsync))!;

	internal readonly static ConcurrentDictionary<Type, MethodInfo> GetRequiredServiceByType = [];
	internal readonly static ConcurrentDictionary<Type, MethodInfo> ParameterWriteMethodByType = [];

	internal readonly static MethodInfo RecordOutboundPayloadSizeMethod =
		typeof(OperationMetrics).GetMethod(nameof(OperationMetrics.RecordOutboundPayloadSize))!;
	internal readonly static MethodInfo PayloadWriterGetWrittenMethod =
		typeof(PayloadWriter).GetProperty(nameof(PayloadWriter.Written))!.GetGetMethod()!;

	private const int BroadcastDefaultInitialBufferSize = 64;

	public static object ResolveWriterService<T>(IServiceProvider services)
	{
		return ServiceProviderExtensions.GetParameterReaderOrWriterService(services, typeof(T));
	}

	public static void WriteOperationId(ref PayloadWriter writer, int operationId)
	{
		var span = writer.GetSpan(sizeof(int));
		BinaryPrimitives.WriteInt32LittleEndian(span, operationId);
		writer.Advance(sizeof(int));
	}

	public static ValueTask WrappedSendAsync(IClientConnection connection, ref PayloadWriter writer, CancellationToken cancellationToken)
	{
		var pendingWrite = connection.SendAsync(writer.WrittenMemory, cancellationToken);
		return pendingWrite.IsCompletedSuccessfully
			? WrappedSendAsyncFast(ref writer)
			: WrappedSendAsyncSlow(pendingWrite, new(ref writer));
	}

	public static ValueTask BroadcastAsync(IEnumerable<IClientConnection> connections, ref PayloadWriter writer, CancellationToken cancellationToken)
	{
		var bufferSize = GetBroadcastBufferSize(connections);
		var pendingWrites = new BroadcastBuffer<ValueTask>(bufferSize);
		var errors = new BroadcastBuffer<Exception>(bufferSize);
		foreach (var connection in connections)
		{
			QueueSendTask(connection, ref writer);
		}

		return pendingWrites.Written == 0
			? BroadcastAsyncFast(ref writer, ref errors)
			: BroadcastAsyncSlow(new(ref writer), pendingWrites, errors);

		// NOTE: Moving this to a separate function eliminates an allocation without affecting throughput
		void QueueSendTask(IClientConnection connection, ref PayloadWriter writer)
		{
			try
			{
				var pendingWrite = connection.SendAsync(writer.WrittenMemory, cancellationToken);
				if (!pendingWrite.IsCompletedSuccessfully)
				{
					pendingWrites.Add(pendingWrite);
				}
			}
			catch (Exception e)
			{
				errors.Add(e);
			}
		}
	}

	private static ValueTask WrappedSendAsyncFast(ref PayloadWriter writer)
	{
		writer.Dispose();
		return ValueTask.CompletedTask;
	}

	// NOTE: Avoid async-path allocation in broadcast hot path (validated by benchmarks)
	[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
	private static async ValueTask WrappedSendAsyncSlow(ValueTask pendingWrite, PayloadWriter.DisposeToken token)
	{
		try
		{
			await pendingWrite.ConfigureAwait(false);
		}
		finally
		{
			token.Dispose();
		}
	}

	private static ValueTask BroadcastAsyncFast(ref PayloadWriter writer, ref BroadcastBuffer<Exception> errors)
	{
		// All writes completed synchronously, safe to release
		writer.Dispose();

		if (errors.Written > 0)
		{
			ThrowBroadcastFailed(errors);
		}

		return ValueTask.CompletedTask;

	}

	// NOTE: Avoid async-path allocation in broadcast hot path (validated by benchmarks)
	[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
	private static async ValueTask BroadcastAsyncSlow(
		PayloadWriter.DisposeToken token,
		BroadcastBuffer<ValueTask> pendingWrites,
		BroadcastBuffer<Exception> errors)
	{
		for (int i = 0; i < pendingWrites.Written; i++)
		{
			try
			{
				await pendingWrites.Values[i].ConfigureAwait(false);
			}
			catch (Exception e)
			{
				errors.Add(e);
			}
		}

		// Now safe to release since all writes are complete and no longer access the data
		token.Dispose();
		pendingWrites.Dispose();

		if (errors.Written > 0)
		{
			ThrowBroadcastFailed(errors);
		}
	}

	private static int GetBroadcastBufferSize(IEnumerable<IClientConnection> connections)
	{
		return connections is ICollection<IClientConnection> sequence
			? sequence.Count
			: BroadcastDefaultInitialBufferSize;
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowBroadcastFailed(BroadcastBuffer<Exception> buffer)
	{
		var errors = buffer.Copy();
		buffer.Dispose();
		throw new AggregateException("One or more broadcast sends failed", errors);
	}

	private struct BroadcastBuffer<T>(int size)
	{
		private readonly int _size = size;
		private T[]? _values;
		private int _written = 0;

		public readonly T[] Values => _values ?? [];
		public readonly int Written => _written;

		public readonly void Dispose()
		{
			if (_values is not null)
			{
				ArrayPool<T>.Shared.Return(_values, clearArray: true);
			}
		}

		public readonly T[] Copy()
		{
			var copy = new T[Written];
			Values.AsSpan(0, Written).CopyTo(copy);
			return copy;
		}

		public void Add(T value)
		{
			var buffer = EnsureCapacity();
			buffer[_written++] = value;
		}

		private T[] EnsureCapacity()
		{
			if (_values is null)
			{
				return _values = ArrayPool<T>.Shared.Rent(_size);
			}
			if (_written < _values.Length)
			{
				return _values;
			}

			var newValues = ArrayPool<T>.Shared.Rent(_values.Length * 2);
			_values.AsSpan(0, _written).CopyTo(newValues);
			ArrayPool<T>.Shared.Return(_values, clearArray: true);
			return _values = newValues;
		}
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
