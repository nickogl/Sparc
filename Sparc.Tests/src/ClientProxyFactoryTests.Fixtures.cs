using Sparc.IO;
using System.Buffers.Binary;
using System.Text;

namespace Sparc.Tests;

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0060 // Remove unused parameter

public partial class ClientProxyFactoryTests
{
	private class TestConnection : IClientConnection
	{
		public int SessionId { get; init; }
		public int SendCallCount { get; protected set; }
		public ReadOnlyMemory<byte>? LastPayload { get; protected set; }
		public CancellationToken? LastCancellationToken { get; protected set; }

		public virtual ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
		{
			SendCallCount++;
			LastPayload = payload;
			LastCancellationToken = cancellationToken;
			return ValueTask.CompletedTask;
		}
	}

	private sealed class ThrowingConnection : TestConnection
	{
		private readonly Exception _exception;

		public ThrowingConnection(Exception exception)
		{
			_exception = exception;
		}

		public override ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
			=> ValueTask.FromException(_exception);
	}

	private sealed class AsyncConnection : TestConnection
	{
		private readonly TaskCompletionSource _sendCompletionSource = new();

		public override ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
		{
			SendCallCount++;
			LastPayload = payload;
			LastCancellationToken = cancellationToken;
			return new ValueTask(_sendCompletionSource.Task);
		}

		public void CompleteSend()
		{
			_sendCompletionSource.SetResult();
		}
	}

	public interface IValidChatClient
	{
		[Operation(1)]
		ValueTask MessageReceivedAsync(IClientConnection connection, string message, CancellationToken cancellationToken);
	}

	public interface IDuplicateOperationIdClient
	{
		[Operation(1)]
		ValueTask AAsync(IClientConnection connection, CancellationToken cancellationToken);

		[Operation(1)]
		ValueTask BAsync(IClientConnection connection, CancellationToken cancellationToken);
	}

	public interface IInvalidReturnTypeClient
	{
		[Operation(1)]
		Task InvalidAsync(IClientConnection connection, CancellationToken cancellationToken);
	}

	public interface IInvalidFirstParameterClient
	{
		[Operation(1)]
		ValueTask InvalidAsync(string notAConnection, CancellationToken cancellationToken);
	}

	public interface IMissingClientCancellationTokenClient
	{
		[Operation(1)]
		ValueTask InvalidAsync(IClientConnection connection, string message);
	}

	public interface IMisplacedClientCancellationTokenClient
	{
		[Operation(1)]
		ValueTask InvalidAsync(IClientConnection connection, CancellationToken cancellationToken, string message);
	}

	public interface IBaseClientContract
	{
		[Operation(1)]
		ValueTask BaseAsync(IClientConnection connection, CancellationToken cancellationToken);
	}

	public interface IDerivedClientContract : IBaseClientContract
	{
		[Operation(2)]
		ValueTask DerivedAsync(IClientConnection connection, CancellationToken cancellationToken);
	}

	private sealed class InvalidClientClass
	{
		[Operation(1)]
		public ValueTask InvalidAsync(IClientConnection connection, CancellationToken cancellationToken)
			=> ValueTask.CompletedTask;
	}

	public interface IMissingOperationAttributeClient
	{
		ValueTask MissingOperationAsync(IClientConnection connection, CancellationToken cancellationToken);
	}

	public interface ITooFewParametersClient
	{
		[Operation(1)]
		ValueTask InvalidAsync(IClientConnection connection);
	}

	public interface IByRefConnectionClient
	{
		[Operation(1)]
		ValueTask InvalidAsync(ref IClientConnection connection, CancellationToken cancellationToken);
	}

	public interface IByRefParameterClient
	{
		[Operation(1)]
		ValueTask InvalidAsync(IClientConnection connection, ref int value, CancellationToken cancellationToken);
	}

	public interface IOverloadedClient
	{
		[Operation(1)]
		ValueTask SendAsync(IClientConnection connection, string message, CancellationToken cancellationToken);

		[Operation(2)]
		ValueTask SendAsync(IClientConnection connection, int number, CancellationToken cancellationToken);
	}

	public interface IMetricsClientA
	{
		[Operation(1)]
		ValueTask SharedAsync(IClientConnection connection, CancellationToken cancellationToken);

		[Operation(2)]
		ValueTask SharedWithValueAsync(IClientConnection connection, int value, CancellationToken cancellationToken);
	}

	public interface IMetricsClientB
	{
		[Operation(1)]
		ValueTask SharedAsync(IClientConnection connection, CancellationToken cancellationToken);

		[Operation(2)]
		ValueTask SharedWithValueAsync(IClientConnection connection, int value, CancellationToken cancellationToken);
	}

	public interface IBroadcastClient
	{
		[Operation(101)]
		ValueTask BroadcastAsync(IEnumerable<IClientConnection> connections, CancellationToken cancellationToken);
	}

	private sealed class StringParameterWriter : IParameterWriter<string>
	{
		public void Write(ref PayloadWriter writer, string value)
		{
			var span = writer.GetSpan(sizeof(int));
			var stringLength = Encoding.UTF8.GetByteCount(value);
			BinaryPrimitives.WriteInt32LittleEndian(span, stringLength);
			writer.Advance(sizeof(int));

			span = writer.GetSpan(stringLength);
			Encoding.UTF8.GetBytes(value, span);
			writer.Advance(stringLength);
		}
	}

	private sealed class Int32ParameterWriter : IParameterWriter<int>
	{
		public void Write(ref PayloadWriter writer, int value)
		{
			var span = writer.GetSpan(sizeof(int));
			BinaryPrimitives.WriteInt32LittleEndian(span, value);
			writer.Advance(sizeof(int));
		}
	}

	private class BroadcastOperationIdConnection : IClientConnection
	{
		public int SendCallCount { get; private set; }
		public int? OperationId { get; private set; }
		public CancellationToken LastCancellationToken { get; private set; }

		public virtual ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
		{
			SendCallCount++;
			OperationId = BinaryPrimitives.ReadInt32LittleEndian(payload.Span[..sizeof(int)]);
			LastCancellationToken = cancellationToken;
			return ValueTask.CompletedTask;
		}
	}

	private sealed class BroadcastPendingConnection : BroadcastOperationIdConnection
	{
		private readonly TaskCompletionSource _sendCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public override ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
		{
			_ = base.SendAsync(payload, cancellationToken);
			return new ValueTask(_sendCompletionSource.Task);
		}

		public void CompleteSend()
		{
			_sendCompletionSource.SetResult();
		}

		public void FailSend(Exception exception)
		{
			_sendCompletionSource.SetException(exception);
		}
	}

	private sealed class BroadcastSyncThrowConnection(Exception exception) : IClientConnection
	{
		private readonly Exception _exception = exception;

		public ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
		{
			throw _exception;
		}
	}

}

#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CA1822 // Mark members as static
