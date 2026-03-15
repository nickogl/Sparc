using Microsoft.Extensions.DependencyInjection;
using Sparc.IO;
using System.Buffers.Binary;

namespace Sparc.Tests;

public class IntegrationTests
{
	[Fact]
	public async Task Sparc_WhenTransportDispatchesInboundPayload_ServiceInvokesInjectedClientProxyAndConnectionCapturesOutboundSend()
	{
		var services = new ServiceCollection();
		services.AddSparcService<ApplicationService, TestConnection>();
		services.AddSparcClient<IApplicationClient>();
		using var serviceProvider = services.BuildServiceProvider();
		var dispatcher = serviceProvider.GetRequiredService<IServiceDispatcher<ApplicationService, TestConnection>>();
		var service = serviceProvider.GetRequiredService<ApplicationService>();
		var connection = new TestConnection();
		var number = -123456789;
		var text = "hello";
		var list = new List<int> { 1, 2, 3 };
		var dictionary = new Dictionary<string, int>
		{
			["a"] = 7,
			["b"] = 8
		};
		int? optional = 42;
		var cancellationToken = new CancellationTokenSource().Token;
		var inboundPayload = EncodeInboundPayload(serviceProvider, number, text, list, dictionary, optional);

		await dispatcher.DispatchAsync(1001, inboundPayload, connection, cancellationToken);
		var sent = await connection.OutboundSent.Task.WaitAsync(TimeSpan.FromSeconds(2));

		Assert.True(service.ClientInitialized);
		Assert.Equal(number, service.ReceivedNumber);
		Assert.Equal(text, service.ReceivedText);
		Assert.Equal(list, service.ReceivedList);
		Assert.Equal(dictionary.Count, service.ReceivedDictionary.Count);
		Assert.Equal(dictionary["a"], service.ReceivedDictionary["a"]);
		Assert.Equal(dictionary["b"], service.ReceivedDictionary["b"]);
		Assert.Equal(optional, service.ReceivedOptional);
		Assert.Equal(cancellationToken, service.ReceivedCancellationToken);
		Assert.Equal(1, connection.SendCallCount);
		Assert.Equal(2001, sent.OperationId);
		Assert.Equal(cancellationToken, sent.CancellationToken);
		Assert.NotEmpty(sent.Payload);
	}

	private static byte[] EncodeInboundPayload(
		IServiceProvider serviceProvider,
		int number,
		string text,
		List<int> list,
		Dictionary<string, int> dictionary,
		int? optional)
	{
		var payloadWriter = new PayloadWriter();
		try
		{
			GetWriter<int>(serviceProvider).Write(ref payloadWriter, number);
			GetWriter<string>(serviceProvider).Write(ref payloadWriter, text);
			GetWriter<List<int>>(serviceProvider).Write(ref payloadWriter, list);
			GetWriter<Dictionary<string, int>>(serviceProvider).Write(ref payloadWriter, dictionary);
			GetWriter<int?>(serviceProvider).Write(ref payloadWriter, optional);
			return payloadWriter.WrittenSpan.ToArray();
		}
		finally
		{
			payloadWriter.Dispose();
		}
	}

	private static IParameterWriter<T> GetWriter<T>(IServiceProvider serviceProvider)
	{
		var writerType = typeof(IParameterWriter<>).MakeGenericType(typeof(T));
		return (IParameterWriter<T>)serviceProvider.GetParameterReaderOrWriterService(writerType);
	}

	public interface IApplicationClient
	{
		[Operation(2001)]
		ValueTask NotifyAsync(
			IClientConnection connection,
			int number,
			string text,
			List<int> list,
			Dictionary<string, int> dictionary,
			int? optional,
			CancellationToken cancellationToken);
	}

	public sealed class ApplicationService(IClientProxyFactory<IApplicationClient> clientProxyFactory)
	{
		private readonly IApplicationClient _client = clientProxyFactory.Create();

		public bool ClientInitialized { get; } = true;
		public int ReceivedNumber { get; private set; }
		public string ReceivedText { get; private set; } = string.Empty;
		public List<int> ReceivedList { get; private set; } = [];
		public Dictionary<string, int> ReceivedDictionary { get; private set; } = [];
		public int? ReceivedOptional { get; private set; }
		public CancellationToken ReceivedCancellationToken { get; private set; }

		[Operation(1001)]
		public async ValueTask ProcessAsync(
			TestConnection connection,
			int number,
			string text,
			List<int> list,
			Dictionary<string, int> dictionary,
			int? optional,
			CancellationToken cancellationToken)
		{
			ReceivedNumber = number;
			ReceivedText = text;
			ReceivedList = list;
			ReceivedDictionary = dictionary;
			ReceivedOptional = optional;
			ReceivedCancellationToken = cancellationToken;

			await _client.NotifyAsync(connection, number, text, list, dictionary, optional, cancellationToken);
		}
	}

	public sealed class TestConnection : IClientConnection
	{
		public int SendCallCount { get; private set; }
		public TaskCompletionSource<OutboundSend> OutboundSent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
		{
			SendCallCount++;

			var span = payload.Span;
			int operationId = BinaryPrimitives.ReadInt32LittleEndian(span[..sizeof(int)]);
			var operationPayload = span[sizeof(int)..].ToArray();
			OutboundSent.TrySetResult(new OutboundSend(operationId, operationPayload, cancellationToken));
			return ValueTask.CompletedTask;
		}
	}

	public readonly record struct OutboundSend(int OperationId, byte[] Payload, CancellationToken CancellationToken);
}
