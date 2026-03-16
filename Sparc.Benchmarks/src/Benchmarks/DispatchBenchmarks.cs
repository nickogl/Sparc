using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Sparc.IO;
using System.Runtime.CompilerServices;

namespace Sparc.Benchmarks;

/// <summary>
/// Benchmarks the raw RPC overhead for dispatching calls to services.
/// Features such as metrics are thus disabled for a fair comparison.
/// </summary>
[MemoryDiagnoser]
public class DispatchBenchmarks
{
	private const int ZeroParametersOperationId = 1001;
	private const int OneParameterOperationId = 1002;
	private const int MultipleParametersOperationId = 1003;

	private ServiceProvider _services = null!;
	private IServiceDispatcher<DispatchService, SyncBenchmarkConnection> _dispatcher = null!;
	private DispatchService _service = null!;
	private IParameterReader<int> _intReader = null!;
	private SyncBenchmarkConnection _connection = null!;
	private byte[] _oneParameterPayload = null!;
	private byte[] _multipleParametersPayload = null!;

	[GlobalSetup]
	public void Setup()
	{
		// NOTE: Library consumers should not pay for what they do not need. To accurately
		// determine the overhead of just the RPC, we disable recording metrics here.
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddMetrics(options => options.DisableMetrics("Sparc"));
		serviceCollection.AddSparcService<DispatchService, SyncBenchmarkConnection>();
		_services = serviceCollection.BuildServiceProvider();

		_dispatcher = _services.GetRequiredService<IServiceDispatcher<DispatchService, SyncBenchmarkConnection>>();
		_service = _services.GetRequiredService<DispatchService>();
		_intReader = _services.GetRequiredService<IParameterReader<int>>();
		_connection = new SyncBenchmarkConnection();
		_oneParameterPayload = BuildPayload(1);
		_multipleParametersPayload = BuildPayload(1, 2, 3, 4);
	}

	[Benchmark(Baseline = true)]
	public ValueTask BaselineDecodeInvokeZeroParameters()
	{
		return _service.ZeroParametersAsync(_connection, default);
	}

	[Benchmark]
	public ValueTask DispatchZeroParameters()
	{
		return _dispatcher.DispatchAsync(ZeroParametersOperationId, [], _connection, default);
	}

	[Benchmark]
	public ValueTask BaselineDecodeInvokeOneParameter()
	{
		var reader = new PayloadReader(_oneParameterPayload);
		int value = _intReader.Read(ref reader);
		return _service.OneParameterAsync(_connection, value, default);
	}

	[Benchmark]
	public ValueTask DispatchOneParameter()
	{
		return _dispatcher.DispatchAsync(OneParameterOperationId, _oneParameterPayload, _connection, default);
	}

	[Benchmark]
	public ValueTask BaselineDecodeInvokeMultipleParameters()
	{
		var reader = new PayloadReader(_multipleParametersPayload);
		int a = _intReader.Read(ref reader);
		int b = _intReader.Read(ref reader);
		int c = _intReader.Read(ref reader);
		int d = _intReader.Read(ref reader);
		return _service.MultipleParametersAsync(_connection, a, b, c, d, default);
	}

	[Benchmark]
	public ValueTask DispatchMultipleParameters()
	{
		return _dispatcher.DispatchAsync(MultipleParametersOperationId, _multipleParametersPayload, _connection, default);
	}

	private byte[] BuildPayload(params int[] values)
	{
		var writer = new PayloadWriter();
		try
		{
			var intWriter = _services.GetRequiredService<IParameterWriter<int>>();
			foreach (var value in values)
			{
				intWriter.Write(ref writer, value);
			}

			return writer.WrittenSpan.ToArray();
		}
		finally
		{
			writer.Dispose();
		}
	}

	internal sealed class DispatchService
	{
		public int LastSum { get; private set; }

		[Operation(ZeroParametersOperationId)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public ValueTask ZeroParametersAsync(SyncBenchmarkConnection connection, CancellationToken cancellationToken)
		{
			LastSum = 0;
			return ValueTask.CompletedTask;
		}

		[Operation(OneParameterOperationId)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public ValueTask OneParameterAsync(SyncBenchmarkConnection connection, int value, CancellationToken cancellationToken)
		{
			LastSum = value;
			return ValueTask.CompletedTask;
		}

		[Operation(MultipleParametersOperationId)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public ValueTask MultipleParametersAsync(SyncBenchmarkConnection _, int a, int b, int c, int d, CancellationToken __)
		{
			LastSum = a + b + c + d;
			return ValueTask.CompletedTask;
		}
	}
}
