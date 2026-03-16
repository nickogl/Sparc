using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Sparc.IO;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Sparc.Benchmarks;

/// <summary>
/// Benchmarks the raw RPC overhead for sending payloads to clients.
/// Features such as metrics are thus disabled for a fair comparison.
/// </summary>
[MemoryDiagnoser]
public class ProxyBenchmarks
{
	private const int ZeroParametersOperationId = 2001;
	private const int OneParameterOperationId = 2002;
	private const int MultipleParametersOperationId = 2003;

	private IProxyContract _proxy = null!;
	private SyncBenchmarkConnection _syncConnection = null!;
	private IParameterWriter<int> _intWriter = null!;

	[GlobalSetup]
	public void Setup()
	{
		// NOTE: Library consumers should not pay for what they do not need. To accurately
		// determine the overhead of just the RPC, we disable recording metrics here.
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddMetrics(options => options.DisableMetrics("Sparc"));
		serviceCollection.AddSparcClient<IProxyContract>();
		var services = serviceCollection.BuildServiceProvider();

		var factory = services.GetRequiredService<IClientProxyFactory<IProxyContract>>();
		_proxy = factory.Create();
		_syncConnection = new SyncBenchmarkConnection();
		_intWriter = services.GetRequiredService<IParameterWriter<int>>();
	}

	[Benchmark(Baseline = true)]
	public ValueTask ProxyBaselineZeroParameters()
	{
		var writer = new PayloadWriter();
		WriteOperationId(ref writer, ZeroParametersOperationId);
		return WrapSend(_syncConnection, ref writer);
	}

	[Benchmark]
	public ValueTask ProxyZeroParameters()
	{
		return _proxy.ZeroParametersAsync(_syncConnection, default);
	}

	[Benchmark]
	public ValueTask ProxyBaselineOneParameter()
	{
		var writer = new PayloadWriter();
		WriteOperationId(ref writer, OneParameterOperationId);
		_intWriter.Write(ref writer, 1);
		return WrapSend(_syncConnection, ref writer);
	}

	[Benchmark]
	public ValueTask ProxyOneParameter()
	{
		return _proxy.OneParameterAsync(_syncConnection, 1, default);
	}

	[Benchmark]
	public ValueTask ProxyBaselineMultipleParameters()
	{
		var writer = new PayloadWriter();
		WriteOperationId(ref writer, MultipleParametersOperationId);
		_intWriter.Write(ref writer, 1);
		_intWriter.Write(ref writer, 2);
		_intWriter.Write(ref writer, 3);
		_intWriter.Write(ref writer, 4);
		return WrapSend(_syncConnection, ref writer);
	}

	[Benchmark]
	public ValueTask ProxyMultipleParameters()
	{
		return _proxy.MultipleParametersAsync(_syncConnection, 1, 2, 3, 4, default);
	}

	public interface IProxyContract
	{
		[Operation(ZeroParametersOperationId)]
		ValueTask ZeroParametersAsync(IClientConnection connection, CancellationToken cancellationToken);

		[Operation(OneParameterOperationId)]
		ValueTask OneParameterAsync(IClientConnection connection, int value, CancellationToken cancellationToken);

		[Operation(MultipleParametersOperationId)]
		ValueTask MultipleParametersAsync(IClientConnection connection, int a, int b, int c, int d, CancellationToken cancellationToken);
	}

	private static void WriteOperationId(ref PayloadWriter writer, int operationId)
	{
		var span = writer.GetSpan(sizeof(int));
		BinaryPrimitives.WriteInt32LittleEndian(span, operationId);
		writer.Advance(sizeof(int));
	}

	private static ValueTask WrapSend(SyncBenchmarkConnection connection, ref PayloadWriter writer)
	{
		// NOTE: Wrapping the send mirrors what we have in the generated proxy, making comparison fairer
		var sendTask = connection.SendAsync(writer.WrittenMemory, default);
		Debug.Assert(sendTask.IsCompletedSuccessfully);
		writer.Dispose();
		return sendTask;
	}
}
