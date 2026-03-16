using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Benchmarks;

[MemoryDiagnoser]
public class BroadcastBenchmarks
{
	[Params(1_000, 10_000, 100_000)]
	public int ClientCount { get; set; }

	[Params(0, 25, 50, 100)]
	public int SyncCompletionPercent { get; set; }

	private IBroadcastContract _proxy = null!;
	private IClientConnection[] _connections = null!;
	private byte[] _payload256 = null!;

	[GlobalSetup]
	public void Setup()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddSparcClient<IBroadcastContract>();
		var services = serviceCollection.BuildServiceProvider();
		var factory = services.GetRequiredService<IClientProxyFactory<IBroadcastContract>>();
		_proxy = factory.Create();

		_payload256 = new byte[252];
		for (int i = 0; i < _payload256.Length; i++)
		{
			_payload256[i] = (byte)((i * 31) % byte.MaxValue);
		}

		int syncCount = ClientCount * SyncCompletionPercent / 100;
		_connections = new IClientConnection[ClientCount];
		for (int i = 0; i < syncCount; i++)
		{
			_connections[i] = new SyncBenchmarkConnection();
		}
		for (int i = syncCount; i < ClientCount; i++)
		{
			_connections[i] = new DeferredBenchmarkConnection();
		}
	}

	[Benchmark]
	public ValueTask BroadcastToManyConnections()
	{
		return _proxy.BroadcastAsync(_connections, _payload256, default);
	}

	public interface IBroadcastContract
	{
		[Operation(3001, InitialBufferSize = 256)]
		ValueTask BroadcastAsync(IEnumerable<IClientConnection> connections, byte[] payload, CancellationToken cancellationToken);
	}
}
