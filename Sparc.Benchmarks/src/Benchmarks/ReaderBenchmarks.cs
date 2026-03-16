using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Sparc;
using Sparc.IO;

namespace Sparc.Benchmarks;

[MemoryDiagnoser]
public class ReaderBenchmarks
{
	private IParameterReader<int> _intReader = null!;
	private IParameterReader<double> _doubleReader = null!;
	private IParameterReader<decimal> _decimalReader = null!;
	private IParameterReader<Guid> _guidReader = null!;
	private IParameterReader<TimeSpan> _timeSpanReader = null!;
	private IParameterReader<string> _stringReader = null!;
	private IParameterReader<int[]> _intArrayReader = null!;

	private byte[] _intPayload = null!;
	private byte[] _doublePayload = null!;
	private byte[] _decimalPayload = null!;
	private byte[] _guidPayload = null!;
	private byte[] _timeSpanPayload = null!;
	private byte[] _smallStringPayload = null!;
	private byte[] _string256Payload = null!;
	private byte[] _intArrayPayload = null!;

	[GlobalSetup]
	public void Setup()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddSparc();
		var services = serviceCollection.BuildServiceProvider();

		_intReader = services.GetRequiredService<IParameterReader<int>>();
		_doubleReader = services.GetRequiredService<IParameterReader<double>>();
		_decimalReader = services.GetRequiredService<IParameterReader<decimal>>();
		_guidReader = services.GetRequiredService<IParameterReader<Guid>>();
		_timeSpanReader = services.GetRequiredService<IParameterReader<TimeSpan>>();
		_stringReader = services.GetRequiredService<IParameterReader<string>>();
		_intArrayReader = (IParameterReader<int[]>)services.GetParameterReaderOrWriterService(typeof(IParameterReader<int[]>));

		var intWriter = services.GetRequiredService<IParameterWriter<int>>();
		var doubleWriter = services.GetRequiredService<IParameterWriter<double>>();
		var decimalWriter = services.GetRequiredService<IParameterWriter<decimal>>();
		var guidWriter = services.GetRequiredService<IParameterWriter<Guid>>();
		var timeSpanWriter = services.GetRequiredService<IParameterWriter<TimeSpan>>();
		var stringWriter = services.GetRequiredService<IParameterWriter<string>>();
		var intArrayWriter = (IParameterWriter<int[]>)services.GetParameterReaderOrWriterService(typeof(IParameterWriter<int[]>));

		var intValue = 42;
		var doubleValue = 123_456.789;
		var decimalValue = 7_922_816_251_426_433.375m;
		var guidValue = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
		var timeSpanValue = TimeSpan.FromDays(12) + TimeSpan.FromMilliseconds(345);
		var smallStringValue = "small-string-value";
		var string256Value = new string('x', 256);
		var intArrayValue = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };

		var writer = new PayloadWriter();
		try
		{
			intWriter.Write(ref writer, intValue);
			_intPayload = writer.WrittenSpan.ToArray();
		}
		finally
		{
			writer.Dispose();
		}

		writer = new PayloadWriter();
		try
		{
			doubleWriter.Write(ref writer, doubleValue);
			_doublePayload = writer.WrittenSpan.ToArray();
		}
		finally
		{
			writer.Dispose();
		}

		writer = new PayloadWriter();
		try
		{
			decimalWriter.Write(ref writer, decimalValue);
			_decimalPayload = writer.WrittenSpan.ToArray();
		}
		finally
		{
			writer.Dispose();
		}

		writer = new PayloadWriter();
		try
		{
			guidWriter.Write(ref writer, guidValue);
			_guidPayload = writer.WrittenSpan.ToArray();
		}
		finally
		{
			writer.Dispose();
		}

		writer = new PayloadWriter();
		try
		{
			timeSpanWriter.Write(ref writer, timeSpanValue);
			_timeSpanPayload = writer.WrittenSpan.ToArray();
		}
		finally
		{
			writer.Dispose();
		}

		writer = new PayloadWriter();
		try
		{
			stringWriter.Write(ref writer, smallStringValue);
			_smallStringPayload = writer.WrittenSpan.ToArray();
		}
		finally
		{
			writer.Dispose();
		}

		writer = new PayloadWriter();
		try
		{
			stringWriter.Write(ref writer, string256Value);
			_string256Payload = writer.WrittenSpan.ToArray();
		}
		finally
		{
			writer.Dispose();
		}

		writer = new PayloadWriter();
		try
		{
			intArrayWriter.Write(ref writer, intArrayValue);
			_intArrayPayload = writer.WrittenSpan.ToArray();
		}
		finally
		{
			writer.Dispose();
		}
	}

	[Benchmark(Baseline = true)]
	public int ReadInt32()
	{
		var reader = new PayloadReader(_intPayload);
		return _intReader.Read(ref reader);
	}

	[Benchmark]
	public double ReadDouble()
	{
		var reader = new PayloadReader(_doublePayload);
		return _doubleReader.Read(ref reader);
	}

	[Benchmark]
	public decimal ReadDecimal()
	{
		var reader = new PayloadReader(_decimalPayload);
		return _decimalReader.Read(ref reader);
	}

	[Benchmark]
	public Guid ReadGuid()
	{
		var reader = new PayloadReader(_guidPayload);
		return _guidReader.Read(ref reader);
	}

	[Benchmark]
	public TimeSpan ReadTimeSpan()
	{
		var reader = new PayloadReader(_timeSpanPayload);
		return _timeSpanReader.Read(ref reader);
	}

	[Benchmark]
	public string ReadString16()
	{
		var reader = new PayloadReader(_smallStringPayload);
		return _stringReader.Read(ref reader);
	}

	[Benchmark]
	public string ReadString256()
	{
		var reader = new PayloadReader(_string256Payload);
		return _stringReader.Read(ref reader);
	}

	[Benchmark]
	public int[] ReadIntArray8()
	{
		var reader = new PayloadReader(_intArrayPayload);
		return _intArrayReader.Read(ref reader);
	}
}
