using Microsoft.Extensions.DependencyInjection;
using Sparc.IO;

namespace Sparc.Tests;

public partial class ClientProxyFactoryTests
{
	[Fact]
	public void ClientProxyFactory_WhenClientContractHasUniqueOperationIds_DoesNotThrow()
	{
		var exception = Record.Exception(CreateClientProxyFactory<IValidChatClient>);

		Assert.Null(exception);
	}

	[Fact]
	public void ClientProxyFactory_WhenClientContractHasDuplicateOperationIds_Throws()
	{
		Assert.ThrowsAny<Exception>(CreateClientProxyFactory<IDuplicateOperationIdClient>);
	}

	[Fact]
	public void ClientProxyFactory_WhenClientMethodDoesNotReturnValueTask_Throws()
	{
		Assert.ThrowsAny<Exception>(CreateClientProxyFactory<IInvalidReturnTypeClient>);
	}

	[Fact]
	public void ClientProxyFactory_WhenClientMethodDoesNotTakeConnectionAsFirstParameter_Throws()
	{
		Assert.ThrowsAny<Exception>(CreateClientProxyFactory<IInvalidFirstParameterClient>);
	}

	[Fact]
	public void ClientProxyFactory_WhenClientMethodHasNoCancellationToken_Throws()
	{
		Assert.ThrowsAny<Exception>(CreateClientProxyFactory<IMissingClientCancellationTokenClient>);
	}

	[Fact]
	public void ClientProxyFactory_WhenClientMethodHasMisplacedCancellationToken_Throws()
	{
		Assert.ThrowsAny<Exception>(CreateClientProxyFactory<IMisplacedClientCancellationTokenClient>);
	}

	[Fact]
	public void ClientProxyFactory_WhenContractInheritsOperationsFromBaseInterface_DoesNotThrow()
	{
		var exception = Record.Exception(CreateClientProxyFactory<IDerivedClientContract>);

		Assert.Null(exception);
	}

	[Fact]
	public void Create_WhenCalled_ReturnsImplementationOfClientContract()
	{
		var factory = CreateClientProxyFactory<IValidChatClient>();

		var proxy = factory.Create();

		Assert.IsType<IValidChatClient>(proxy, exactMatch: false);
	}

	[Fact]
	public async Task Proxy_WhenMethodIsInvoked_CallsSendAsyncOnProvidedConnection()
	{
		var factory = CreateClientProxyFactory<IValidChatClient>();
		var proxy = factory.Create();
		var connection = new TestConnection();

		await proxy.MessageReceivedAsync(connection, "hello", default);

		Assert.Equal(1, connection.SendCallCount);
	}

	[Fact]
	public async Task Proxy_WhenMethodIsInvoked_DoesNotSerializeConnectionParameter()
	{
		var factory = CreateClientProxyFactory<IValidChatClient>();
		var proxy = factory.Create();
		var connection = new TestConnection();

		await proxy.MessageReceivedAsync(connection, "hello", default);

		// 1 (operation ID) little endian, 5 (string length) little endian, hello
		Assert.Equal(connection.LastPayload?.ToArray(), [1, 0, 0, 0, 5, 0, 0, 0, 104, 101, 108, 108, 111]);
	}

	[Fact]
	public async Task Proxy_WhenSendAsyncThrows_BubblesSameException()
	{
		var factory = CreateClientProxyFactory<IValidChatClient>();
		var proxy = factory.Create();
		var connection = new ThrowingConnection(new InvalidOperationException("send failed"));

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.MessageReceivedAsync(connection, "hello", default).AsTask());

		Assert.Equal("send failed", exception.Message);
	}

	[Fact]
	public void ClientProxyFactory_WhenClientTypeIsNotInterface_Throws()
	{
		Assert.ThrowsAny<Exception>(CreateClientProxyFactory<InvalidClientClass>);
	}

	[Fact]
	public void ClientProxyFactory_WhenClientMethodIsMissingOperationAttribute_Throws()
	{
		Assert.ThrowsAny<Exception>(CreateClientProxyFactory<IMissingOperationAttributeClient>);
	}

	[Fact]
	public void ClientProxyFactory_WhenClientMethodHasTooFewParameters_Throws()
	{
		Assert.ThrowsAny<Exception>(CreateClientProxyFactory<ITooFewParametersClient>);
	}

	[Fact]
	public void ClientProxyFactory_WhenConnectionParameterIsPassedByReference_Throws()
	{
		Assert.ThrowsAny<Exception>(CreateClientProxyFactory<IByRefConnectionClient>);
	}

	[Fact]
	public void ClientProxyFactory_WhenNonConnectionParameterIsPassedByReference_Throws()
	{
		Assert.ThrowsAny<Exception>(CreateClientProxyFactory<IByRefParameterClient>);
	}

	[Fact]
	public async Task Proxy_WhenMethodIsInvoked_ForwardsCancellationTokenToConnection()
	{
		var factory = CreateClientProxyFactory<IValidChatClient>();
		var proxy = factory.Create();
		var connection = new TestConnection();
		var cancellationToken = new CancellationToken(canceled: true);

		await proxy.MessageReceivedAsync(connection, "hello", cancellationToken);

		Assert.Equal(cancellationToken, connection.LastCancellationToken);
	}

	[Fact]
	public async Task Proxy_WhenMethodHasNoEffectiveParameters_SerializesOnlyOperationId()
	{
		var factory = CreateClientProxyFactory<IBaseClientContract>();
		var proxy = factory.Create();
		var connection = new TestConnection();

		await proxy.BaseAsync(connection, default);

		Assert.Equal(connection.LastPayload?.ToArray(), [1, 0, 0, 0]);
	}

	[Fact]
	public void ClientProxyFactory_WhenClientContractHasOverloadedMethods_DoesNotThrow()
	{
		var exception = Record.Exception(CreateClientProxyFactory<IOverloadedClient>);

		Assert.Null(exception);
	}

	[Fact]
	public async Task Proxy_WhenOverloadedStringMethodIsInvoked_SerializesCorrectPayload()
	{
		var factory = CreateClientProxyFactory<IOverloadedClient>();
		var proxy = factory.Create();
		var connection = new TestConnection();

		await proxy.SendAsync(connection, "hello", default);

		// 1 (operation ID) little endian, 5 (string length) little endian, hello
		Assert.Equal(connection.LastPayload?.ToArray(), [1, 0, 0, 0, 5, 0, 0, 0, 104, 101, 108, 108, 111]);
	}

	[Fact]
	public async Task Proxy_WhenOverloadedIntMethodIsInvoked_SerializesCorrectPayload()
	{
		var factory = CreateClientProxyFactory<IOverloadedClient>();
		var proxy = factory.Create();
		var connection = new TestConnection();

		await proxy.SendAsync(connection, 42, default);

		// 2 (operation ID) little endian, 42 little endian
		Assert.Equal(connection.LastPayload?.ToArray(), [2, 0, 0, 0, 42, 0, 0, 0]);
	}

	[Fact]
	public void ClientProxyFactory_WhenParameterWriterIsNotRegistered_Throws()
	{
		var serviceCollection = new ServiceCollection();
		var serviceProvider = serviceCollection.BuildServiceProvider();

		Assert.ThrowsAny<Exception>(() => new ClientProxyFactory<IValidChatClient>(serviceProvider));
	}

	[Fact]
	public void Create_WhenCalledMultipleTimes_ReturnsSameInstance()
	{
		var factory = CreateClientProxyFactory<IValidChatClient>();

		var first = factory.Create();
		var second = factory.Create();

		Assert.Same(first, second);
	}

	[Fact]
	public async Task Proxy_WhenContractInheritsOperations_ImplementsBaseAndDerivedMethods()
	{
		var factory = CreateClientProxyFactory<IDerivedClientContract>();
		var proxy = factory.Create();
		var connection = new TestConnection();

		await proxy.BaseAsync(connection, default);
		Assert.Equal(connection.LastPayload?.ToArray(), [1, 0, 0, 0]);

		await proxy.DerivedAsync(connection, default);
		Assert.Equal(connection.LastPayload?.ToArray(), [2, 0, 0, 0]);

		Assert.Equal(2, connection.SendCallCount);
	}

	[Fact]
	public async Task Proxy_WhenSendAsyncDoesNotCompleteSynchronously_ReturnsIncompleteValueTaskAndCompletesLater()
	{
		var factory = CreateClientProxyFactory<IValidChatClient>();
		var proxy = factory.Create();
		var connection = new AsyncConnection();

		var sendTask = proxy.MessageReceivedAsync(connection, "hello", default);

		Assert.False(sendTask.IsCompleted);

		connection.CompleteSend();

		// This also tests that the async path safely returns the buffer back to the memory pool
		await sendTask;

		Assert.Equal(1, connection.SendCallCount);
		Assert.Equal(connection.LastPayload?.ToArray(), [1, 0, 0, 0, 5, 0, 0, 0, 104, 101, 108, 108, 111]);
	}

	[Fact]
	public async Task Proxy_WhenOperationsAreInvoked_RecordsOutboundMetricsWithExpectedLabelsAndValues()
	{
		using var metrics = new PayloadSizeMetricListener();
		var firstFactory = CreateClientProxyFactory<IMetricsClientA>();
		var firstProxy = firstFactory.Create();
		var secondFactory = CreateClientProxyFactory<IMetricsClientB>();
		var secondProxy = secondFactory.Create();
		var connection = new TestConnection();

		await firstProxy.SharedAsync(connection, default);
		await firstProxy.SharedWithValueAsync(connection, 42, default);
		await secondProxy.SharedAsync(connection, default);
		await secondProxy.SharedWithValueAsync(connection, 42, default);

		var measurements = metrics.Measurements.Where(m =>
			m.Kind == "outbound" &&
			(m.Contract == nameof(IMetricsClientA) || m.Contract == nameof(IMetricsClientB))).ToArray();

		Assert.Equal(4, measurements.Length);
		Assert.Contains(measurements, m => m.Contract == nameof(IMetricsClientA)
			&& m.Operation == nameof(IMetricsClientA.SharedAsync)
			&& m.OperationId == 1
			&& m.PayloadSize == sizeof(int));
		Assert.Contains(measurements, m => m.Contract == nameof(IMetricsClientA)
			&& m.Operation == nameof(IMetricsClientA.SharedWithValueAsync)
			&& m.OperationId == 2
			&& m.PayloadSize == sizeof(int) * 2);
		Assert.Contains(measurements, m => m.Contract == nameof(IMetricsClientB)
			&& m.Operation == nameof(IMetricsClientB.SharedAsync)
			&& m.OperationId == 1
			&& m.PayloadSize == sizeof(int));
		Assert.Contains(measurements, m => m.Contract == nameof(IMetricsClientB)
			&& m.Operation == nameof(IMetricsClientB.SharedWithValueAsync)
			&& m.OperationId == 2
			&& m.PayloadSize == sizeof(int) * 2);
	}

	private static ClientProxyFactory<TClient> CreateClientProxyFactory<TClient>() where TClient : class
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddSingleton<IParameterWriter<string>, StringParameterWriter>();
		serviceCollection.AddSingleton<IParameterWriter<int>, Int32ParameterWriter>();
		serviceCollection.AddMetrics();
		serviceCollection.AddSingleton<OperationMetrics>();
		return new ClientProxyFactory<TClient>(serviceCollection.BuildServiceProvider());
	}
}
