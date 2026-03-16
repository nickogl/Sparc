using Microsoft.Extensions.DependencyInjection;
using Sparc.IO;
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
		List<Task>? pendingWrites = null;
		List<Exception>? errors = null;
		foreach (var connection in connections)
		{
			try
			{
				var pendingWrite = connection.SendAsync(writer.WrittenMemory, cancellationToken);
				if (!pendingWrite.IsCompletedSuccessfully)
				{
					(pendingWrites ??= []).Add(pendingWrite.AsTask());
				}
			}
			catch (Exception e)
			{
				(errors ??= []).Add(e);
			}
		}

		return pendingWrites is null
			? BroadcastAsyncFast(ref writer, errors)
			: BroadcastAsyncSlow(pendingWrites, errors, new(ref writer));
	}

	private static ValueTask WrappedSendAsyncFast(ref PayloadWriter writer)
	{
		writer.Dispose();
		return ValueTask.CompletedTask;
	}

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

	private static ValueTask BroadcastAsyncFast(ref PayloadWriter writer, List<Exception>? errors)
	{
		writer.Dispose();

		if (errors is not null)
		{
			ThrowBroadcastFailed(errors);
		}

		return ValueTask.CompletedTask;

	}

	private static async ValueTask BroadcastAsyncSlow(List<Task> pendingWrites, List<Exception>? errors, PayloadWriter.DisposeToken token)
	{
		try
		{
			foreach (var pendingWrite in pendingWrites)
			{
				try
				{
					await pendingWrite.ConfigureAwait(false);
				}
				catch (Exception e)
				{
					(errors ??= []).Add(e);
				}
			}
		}
		finally
		{
			token.Dispose();
		}

		if (errors is not null)
		{
			ThrowBroadcastFailed(errors);
		}
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowBroadcastFailed(List<Exception> errors)
	{
		throw new AggregateException("One or more broadcast sends failed", errors);
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
