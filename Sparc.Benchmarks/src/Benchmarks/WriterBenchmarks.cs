using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Sparc;
using Sparc.IO;

namespace Sparc.Benchmarks;

[MemoryDiagnoser]
public class WriterBenchmarks
{
	private IParameterWriter<int> _intWriter = null!;
	private IParameterWriter<double> _doubleWriter = null!;
	private IParameterWriter<decimal> _decimalWriter = null!;
	private IParameterWriter<Guid> _guidWriter = null!;
	private IParameterWriter<TimeSpan> _timeSpanWriter = null!;
	private IParameterWriter<string> _stringWriter = null!;
	private IParameterWriter<int[]> _intArrayWriter = null!;

	private int _intValue;
	private double _doubleValue;
	private decimal _decimalValue;
	private Guid _guidValue;
	private TimeSpan _timeSpanValue;
	private string _smallStringValue = null!;
	private string _string256Value = null!;
	private int[] _intArrayValue = null!;

	[GlobalSetup]
	public void Setup()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddSparc();
		var services = serviceCollection.BuildServiceProvider();

		_intWriter = services.GetRequiredService<IParameterWriter<int>>();
		_doubleWriter = services.GetRequiredService<IParameterWriter<double>>();
		_decimalWriter = services.GetRequiredService<IParameterWriter<decimal>>();
		_guidWriter = services.GetRequiredService<IParameterWriter<Guid>>();
		_timeSpanWriter = services.GetRequiredService<IParameterWriter<TimeSpan>>();
		_stringWriter = services.GetRequiredService<IParameterWriter<string>>();
		_intArrayWriter = (IParameterWriter<int[]>)services.GetParameterReaderOrWriterService(typeof(IParameterWriter<int[]>));

		_intValue = 42;
		_doubleValue = 123_456.789;
		_decimalValue = 7_922_816_251_426_433.375m;
		_guidValue = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
		_timeSpanValue = TimeSpan.FromDays(12) + TimeSpan.FromMilliseconds(345);
		_smallStringValue = "small-string-value";
		_string256Value = new string('x', 256);
		_intArrayValue = [1, 2, 3, 4, 5, 6, 7, 8];
	}

	[Benchmark(Baseline = true)]
	public void WriteInt32()
	{
		var writer = new PayloadWriter();
		try
		{
			_intWriter.Write(ref writer, _intValue);
		}
		finally
		{
			writer.Dispose();
		}
	}

	[Benchmark]
	public void WriteDouble()
	{
		var writer = new PayloadWriter();
		try
		{
			_doubleWriter.Write(ref writer, _doubleValue);
		}
		finally
		{
			writer.Dispose();
		}
	}

	[Benchmark]
	public void WriteDecimal()
	{
		var writer = new PayloadWriter();
		try
		{
			_decimalWriter.Write(ref writer, _decimalValue);
		}
		finally
		{
			writer.Dispose();
		}
	}

	[Benchmark]
	public void WriteGuid()
	{
		var writer = new PayloadWriter();
		try
		{
			_guidWriter.Write(ref writer, _guidValue);
		}
		finally
		{
			writer.Dispose();
		}
	}

	[Benchmark]
	public void WriteTimeSpan()
	{
		var writer = new PayloadWriter();
		try
		{
			_timeSpanWriter.Write(ref writer, _timeSpanValue);
		}
		finally
		{
			writer.Dispose();
		}
	}

	[Benchmark]
	public void WriteString16()
	{
		var writer = new PayloadWriter();
		try
		{
			_stringWriter.Write(ref writer, _smallStringValue);
		}
		finally
		{
			writer.Dispose();
		}
	}

	[Benchmark]
	public void WriteString256()
	{
		var writer = new PayloadWriter();
		try
		{
			_stringWriter.Write(ref writer, _string256Value);
		}
		finally
		{
			writer.Dispose();
		}
	}

	[Benchmark]
	public void WriteIntArray8()
	{
		var writer = new PayloadWriter();
		try
		{
			_intArrayWriter.Write(ref writer, _intArrayValue);
		}
		finally
		{
			writer.Dispose();
		}
	}
}
