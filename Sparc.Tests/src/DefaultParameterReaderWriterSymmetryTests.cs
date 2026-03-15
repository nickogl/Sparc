using Microsoft.Extensions.DependencyInjection;
using Sparc.IO;

namespace Sparc.Tests;

public class DefaultReaderWriterSymmetryTests
{
	[Fact]
	public void Boolean_WhenValueIsProvided_ReturnsSameValue()
	{
		bool value = true;

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void Byte_WhenValueIsProvided_ReturnsSameValue()
	{
		byte value = 123;

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void SByte_WhenValueIsProvided_ReturnsSameValue()
	{
		sbyte value = -12;

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void Int16_WhenValueIsProvided_ReturnsSameValue()
	{
		short value = -1234;

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void UInt16_WhenValueIsProvided_ReturnsSameValue()
	{
		ushort value = 65000;

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void Int32_WhenValueIsProvided_ReturnsSameValue()
	{
		int value = -123456789;

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void UInt32_WhenValueIsProvided_ReturnsSameValue()
	{
		uint value = 4_000_000_000;

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void Int64_WhenValueIsProvided_ReturnsSameValue()
	{
		long value = -9_000_000_000_000_000_000;

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void UInt64_WhenValueIsProvided_ReturnsSameValue()
	{
		ulong value = 18_000_000_000_000_000_000;

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void Int128_WhenValueIsProvided_ReturnsSameValue()
	{
		var value = Int128.Parse("-17014118346046923173168730371588410572");

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void UInt128_WhenValueIsProvided_ReturnsSameValue()
	{
		var value = UInt128.Parse("34028236692093846346337460743176821145");

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void Float_WhenValueIsProvided_ReturnsSameValue()
	{
		float value = 123.5f;

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void Double_WhenValueIsProvided_ReturnsSameValue()
	{
		double value = 12345.6789d;

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void Decimal_WhenValueIsProvided_ReturnsSameValue()
	{
		decimal value = 12345.6789m;

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void String_WhenValueIsProvided_ReturnsSameValue()
	{
		string value = "hello-äöü-世界";

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void Char_WhenValueIsProvided_ReturnsSameValue()
	{
		char value = 'Ä';

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void Guid_WhenValueIsProvided_ReturnsSameValue()
	{
		var value = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void DateTime_WhenValueIsProvided_ReturnsSameValue()
	{
		var value = new DateTime(2026, 3, 15, 10, 11, 12, DateTimeKind.Utc).AddTicks(1234);

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void DateTimeOffset_WhenValueIsProvided_ReturnsSameValue()
	{
		var value = new DateTimeOffset(2026, 3, 15, 10, 11, 12, TimeSpan.FromHours(2)).AddTicks(1234);

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void TimeSpan_WhenValueIsMillisecondPrecision_ReturnsSameValue()
	{
		var value = TimeSpan.FromMilliseconds(1234);

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void Array_WhenValueIsProvided_ReturnsSameValue()
	{
		var value = new[] { 1, 2, 3 };

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void List_WhenValueIsProvided_ReturnsSameValue()
	{
		var value = new List<int> { 1, 2, 3 };

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	[Fact]
	public void Dictionary_WhenValueIsProvided_ReturnsSameValues()
	{
		var value = new Dictionary<string, int>
		{
			["a"] = 1,
			["b"] = 2
		};

		var result = Roundtrip(value);

		Assert.Equal(value.Count, result.Count);
		Assert.Equal(value["a"], result["a"]);
		Assert.Equal(value["b"], result["b"]);
	}

	[Fact]
	public void Nullable_WhenValueIsNull_ReturnsNull()
	{
		int? value = null;

		var result = Roundtrip(value);

		Assert.Null(result);
	}

	[Fact]
	public void Nullable_WhenValueIsPresent_ReturnsSameValue()
	{
		int? value = 42;

		var result = Roundtrip(value);

		Assert.Equal(value, result);
	}

	private static T Roundtrip<T>(T value)
	{
		using var serviceProvider = CreateServiceProvider();
		var writer = GetWriter<T>(serviceProvider);
		var reader = GetReader<T>(serviceProvider);

		var payloadWriter = new PayloadWriter();
		byte[] payload;
		try
		{
			writer.Write(ref payloadWriter, value);
			payload = payloadWriter.WrittenSpan.ToArray();
		}
		finally
		{
			payloadWriter.Dispose();
		}

		var payloadReader = new PayloadReader(payload);
		return reader.Read(ref payloadReader);
	}

	private static IParameterWriter<T> GetWriter<T>(ServiceProvider serviceProvider)
	{
		var writerType = typeof(IParameterWriter<>).MakeGenericType(typeof(T));
		return (IParameterWriter<T>)serviceProvider.GetParameterReaderOrWriterService(writerType);
	}

	private static IParameterReader<T> GetReader<T>(ServiceProvider serviceProvider)
	{
		var readerType = typeof(IParameterReader<>).MakeGenericType(typeof(T));
		return (IParameterReader<T>)serviceProvider.GetParameterReaderOrWriterService(readerType);
	}

	private static ServiceProvider CreateServiceProvider()
	{
		var services = new ServiceCollection();
		services.AddSparc();
		return services.BuildServiceProvider();
	}
}
