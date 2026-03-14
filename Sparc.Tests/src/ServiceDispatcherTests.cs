using Microsoft.Extensions.DependencyInjection;
using Sparc.Exceptions;
using Sparc.IO;
using System.Buffers.Binary;
using System.Text;

namespace Sparc.Tests;

public partial class ServiceDispatcherTests
{
	[Fact]
	public void ServiceDispatcher_WhenServiceContractHasUniqueOperationIds_DoesNotThrow()
	{
		var service = new ValidChatService();

		var exception = Record.Exception(() => CreateDispatcher<ValidChatService, TestConnection>(service));

		Assert.Null(exception);
	}

	[Fact]
	public void ServiceDispatcher_WhenServiceContractHasDuplicateOperationIds_Throws()
	{
		var service = new DuplicateOperationIdService();

		Assert.ThrowsAny<Exception>(() => CreateDispatcher<DuplicateOperationIdService, TestConnection>(service));
	}

	[Fact]
	public void ServiceDispatcher_WhenServiceMethodDoesNotReturnValueTask_Throws()
	{
		var service = new InvalidReturnTypeService();

		Assert.ThrowsAny<Exception>(() => CreateDispatcher<InvalidReturnTypeService, TestConnection>(service));
	}

	[Fact]
	public void ServiceDispatcher_WhenServiceMethodDoesNotTakeConnectionAsFirstParameter_Throws()
	{
		var service = new InvalidFirstParameterService();

		Assert.ThrowsAny<Exception>(() => CreateDispatcher<InvalidFirstParameterService, TestConnection>(service));
	}


	[Fact]
	public void ServiceDispatcher_WhenCancellationTokenIsTrailing_DoesNotThrow()
	{
		var service = new ValidChatService();

		var exception = Record.Exception(() => CreateDispatcher<ValidChatService, TestConnection>(service));

		Assert.Null(exception);
	}

	[Fact]
	public void ServiceDispatcher_WhenCancellationTokenIsNotTrailing_Throws()
	{
		var service = new InvalidCancellationPlacementService();

		Assert.ThrowsAny<Exception>(() => CreateDispatcher<InvalidCancellationPlacementService, TestConnection>(service));
	}

	[Fact]
	public void ServiceDispatcher_WhenServiceUsesOverloadsWithDifferentOperationIds_DoesNotThrow()
	{
		var service = new OverloadedService();

		var exception = Record.Exception(() => CreateDispatcher<OverloadedService, TestConnection>(service));

		Assert.Null(exception);
	}

	[Fact]
	public void ServiceDispatcher_WhenInheritedContractHierarchyContainsDuplicateOperationIds_Throws()
	{
		var service = new DuplicateInheritedOperationIdService();

		Assert.ThrowsAny<Exception>(() => CreateDispatcher<IDuplicateInheritedOperationIdService<TestConnection>, TestConnection>(service));
	}

	[Fact]
	public void ServiceDispatcher_WhenServiceMethodIsStatic_Throws()
	{
		var service = new StaticOperationService();

		Assert.ThrowsAny<Exception>(() => CreateDispatcher<StaticOperationService, TestConnection>(service));
	}

	[Fact]
	public void ServiceDispatcher_WhenServiceMethodHasNoParameters_Throws()
	{
		var service = new ParameterlessOperationService();

		Assert.ThrowsAny<Exception>(() => CreateDispatcher<ParameterlessOperationService, TestConnection>(service));
	}

	[Fact]
	public void ServiceDispatcher_WhenServiceMethodTakesConnectionByReference_Throws()
	{
		var service = new ByRefConnectionService();

		Assert.ThrowsAny<Exception>(() => CreateDispatcher<ByRefConnectionService, TestConnection>(service));
	}

	[Fact]
	public void ServiceDispatcher_WhenServiceMethodTakesParameterByReference_Throws()
	{
		var service = new ByRefParameterService();

		Assert.ThrowsAny<Exception>(() => CreateDispatcher<ByRefParameterService, TestConnection>(service));
	}

	[Fact]
	public void ServiceDispatcher_WhenParameterReaderIsMissing_Throws()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddSingleton(new IntParameterService());
		serviceCollection.AddSingleton<IParameterReader<string>, StringParameterReader>();

		Assert.ThrowsAny<Exception>(() =>
			new ServiceDispatcher<IntParameterService, TestConnection>(serviceCollection.BuildServiceProvider()));
	}

	[Fact]
	public async Task DispatchAsync_WhenOperationIdMatches_InvokesTargetServiceMethod()
	{
		var service = new SpyChatService();
		var dispatcher = CreateDispatcher<SpyChatService, TestConnection>(service);
		var connection = new TestConnection();
		var payload = EncodeStringParameter("test");

		await dispatcher.DispatchAsync(1, [.. payload], connection, CancellationToken.None);

		Assert.Equal(1, service.SendMessageCallCount);
	}

	[Fact]
	public async Task DispatchAsync_WhenMethodHasConnectionParameter_PassesSameConnectionInstance()
	{
		var service = new SpyChatService();
		var dispatcher = CreateDispatcher<SpyChatService, TestConnection>(service);
		var connection = new TestConnection { SessionId = 123 };
		var payload = EncodeStringParameter(string.Empty);

		await dispatcher.DispatchAsync(1, payload, connection, CancellationToken.None);

		Assert.Same(connection, service.LastConnection);
	}

	[Fact]
	public async Task DispatchAsync_WhenMethodHasCancellationToken_PassesSameToken()
	{
		var service = new SpyChatService();
		var dispatcher = CreateDispatcher<SpyChatService, TestConnection>(service);
		var token = new CancellationTokenSource().Token;
		var connection = new TestConnection { SessionId = 123 };

		await dispatcher.DispatchAsync(2, [], connection, token);

		Assert.Equal(token, service.LastCancellationToken);
	}

	[Fact]
	public async Task DispatchAsync_WhenOperationIdIsUnknown_ThrowsUnknownOperationException()
	{
		var service = new SpyChatService();
		var dispatcher = CreateDispatcher<SpyChatService, TestConnection>(service);
		var connection = new TestConnection();

		await Assert.ThrowsAsync<UnknownOperationException>(() => dispatcher.DispatchAsync(999, [], connection, default).AsTask());
	}

	[Fact]
	public async Task DispatchAsync_WhenPayloadHasExcessBytes_ThrowsUnconsumedDataException()
	{
		var service = new SpyChatService();
		var dispatcher = CreateDispatcher<SpyChatService, TestConnection>(service);
		var connection = new TestConnection();
		var payload = EncodeStringParameter("A").ToList();

		payload.Add(65);

		await Assert.ThrowsAsync<UnconsumedDataException>(() => dispatcher.DispatchAsync(1, [.. payload], connection, default).AsTask());
	}

	[Fact]
	public async Task DispatchAsync_WhenServiceThrows_BubblesSameException()
	{
		var service = new ThrowingService(new InvalidOperationException("boom"));
		var dispatcher = CreateDispatcher<ThrowingService, TestConnection>(service);
		var connection = new TestConnection();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(1, [], connection, default).AsTask());
		Assert.Equal("boom", exception.Message);
	}

	private static byte[] EncodeStringParameter(string value)
	{
		var byteCount = Encoding.UTF8.GetByteCount(value);
		var data = new byte[sizeof(int) + byteCount];
		BinaryPrimitives.WriteInt32LittleEndian(data, byteCount);
		Encoding.UTF8.GetBytes(value, data.AsSpan(sizeof(int)));
		return data;
	}

	private static ServiceDispatcher<TService, TConnection> CreateDispatcher<TService, TConnection>(TService service)
		where TService : class
		where TConnection : IClientConnection
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddSingleton(service);
		serviceCollection.AddSingleton<IParameterReader<string>, StringParameterReader>();
		serviceCollection.AddSingleton<IParameterReader<int>, Int32ParameterReader>();
		return new ServiceDispatcher<TService, TConnection>(serviceCollection.BuildServiceProvider());
	}

	private sealed class StringParameterReader : IParameterReader<string>
	{
		public string Read(ref PayloadReader reader)
		{
			var stringLength = BinaryPrimitives.ReadInt32LittleEndian(reader.Read(sizeof(int)));
			var stringData = reader.Read(stringLength);
			return Encoding.UTF8.GetString(stringData);
		}
	}

	private sealed class Int32ParameterReader : IParameterReader<int>
	{
		public int Read(ref PayloadReader reader)
		{
			return BinaryPrimitives.ReadInt32LittleEndian(reader.Read(sizeof(int)));
		}
	}
}
