using Sparc.IO;
using System.Buffers.Binary;
using System.Text;

namespace Sparc.Tests;

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0060 // Remove unused parameter

public partial class ServiceDispatcherTests
{
	private class TestConnection : IClientConnection
	{
		public int SessionId { get; init; }
		public int SendCallCount { get; private set; }
		public ReadOnlyMemory<byte>? LastPayload { get; private set; }
		public CancellationToken? LastCancellationToken { get; private set; }

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

	private sealed class ValidChatService
	{
		public ValueTask SendMessageAsync(TestConnection connection, string message) => ValueTask.CompletedTask;

		public ValueTask PingAsync(TestConnection connection, CancellationToken cancellationToken) => ValueTask.CompletedTask;
	}

	private sealed class SpyChatService
	{
		public int SendMessageCallCount { get; private set; }
		public TestConnection? LastConnection { get; private set; }
		public CancellationToken LastCancellationToken { get; private set; }

		[Operation(1)]
		public ValueTask SendMessageAsync(TestConnection connection, string message)
		{
			SendMessageCallCount++;
			LastConnection = connection;
			return ValueTask.CompletedTask;
		}

		[Operation(2)]
		public ValueTask PingAsync(TestConnection connection, CancellationToken cancellationToken)
		{
			LastConnection = connection;
			LastCancellationToken = cancellationToken;
			return ValueTask.CompletedTask;
		}
	}

	private sealed class DuplicateOperationIdService
	{
		[Operation(1)]
		public ValueTask AAsync(TestConnection connection) => ValueTask.CompletedTask;

		[Operation(1)]
		public ValueTask BAsync(TestConnection connection) => ValueTask.CompletedTask;
	}

	private sealed class InvalidReturnTypeService
	{
		[Operation(1)]
		public Task InvalidAsync(TestConnection connection) => Task.CompletedTask;
	}

	private sealed class InvalidFirstParameterService
	{
		[Operation(1)]
		public ValueTask InvalidAsync(string notAConnection) => ValueTask.CompletedTask;
	}

	private sealed class InvalidCancellationPlacementService
	{
#pragma warning disable CA1068 // CancellationToken parameters must come last
		[Operation(1)]
		public ValueTask InvalidAsync(TestConnection connection, CancellationToken cancellationToken, string message) => ValueTask.CompletedTask;
#pragma warning restore CA1068 // CancellationToken parameters must come last
	}

	private sealed class OverloadedService
	{
		[Operation(1)]
		public ValueTask SendAsync(TestConnection connection, string message) => ValueTask.CompletedTask;

		[Operation(2)]
		public ValueTask SendAsync(TestConnection connection, int number) => ValueTask.CompletedTask;
	}

	private interface IBaseDuplicateInheritedService<TConnection>
		where TConnection : IClientConnection
	{
		[Operation(1)]
		ValueTask BaseAsync(TConnection connection);
	}

	private interface IDuplicateInheritedOperationIdService<TConnection> : IBaseDuplicateInheritedService<TConnection>
		where TConnection : IClientConnection
	{
		[Operation(1)]
		ValueTask DerivedAsync(TConnection connection);
	}

	private sealed class DuplicateInheritedOperationIdService : IDuplicateInheritedOperationIdService<TestConnection>
	{
		public ValueTask BaseAsync(TestConnection connection) => ValueTask.CompletedTask;
		public ValueTask DerivedAsync(TestConnection connection) => ValueTask.CompletedTask;
	}

	private sealed class ThrowingService
	{
		private readonly Exception _exception;

		public ThrowingService(Exception exception)
		{
			_exception = exception;
		}

		[Operation(1)]
		public ValueTask ThrowAsync(TestConnection connection) => ValueTask.FromException(_exception);
	}

	private interface IStaticOperationService<TConnection>
		where TConnection : IClientConnection
	{
		[Operation(1)]
		static abstract ValueTask InvalidAsync(TConnection connection);
	}

	private sealed class StaticOperationService : IStaticOperationService<TestConnection>
	{
		public static ValueTask InvalidAsync(TestConnection connection) => ValueTask.CompletedTask;
	}

	private sealed class ParameterlessOperationService
	{
		[Operation(1)]
		public ValueTask InvalidAsync() => ValueTask.CompletedTask;
	}

	private sealed class ByRefConnectionService
	{
		[Operation(1)]
		public ValueTask InvalidAsync(ref TestConnection connection) => ValueTask.CompletedTask;
	}

	private sealed class ByRefParameterService
	{
		[Operation(1)]
		public ValueTask InvalidAsync(TestConnection connection, ref int value) => ValueTask.CompletedTask;
	}

	private sealed class IntParameterService
	{
		[Operation(1)]
		public ValueTask SendAsync(TestConnection connection, int value) => ValueTask.CompletedTask;
	}
}

#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CA1822 // Mark members as static
