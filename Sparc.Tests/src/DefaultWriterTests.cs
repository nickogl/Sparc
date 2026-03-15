using Microsoft.Extensions.DependencyInjection;
using Sparc.IO;
using System.Buffers.Binary;
using System.Text;

namespace Sparc.Tests;

public class DefaultWriterTests
{
	[Fact]
	public void WriteBoolean_WhenValueIsTrue_WritesOneByte()
	{
		var payload = Write(true);

		Assert.Equal([1], payload);
	}

	[Fact]
	public void WriteBoolean_WhenValueIsFalse_WritesZeroByte()
	{
		var payload = Write(false);

		Assert.Equal([0], payload);
	}

	[Fact]
	public void WriteByte_WhenValueIsProvided_WritesSingleByte()
	{
		var payload = Write((byte)0xAB);

		Assert.Equal([0xAB], payload);
	}

	[Fact]
	public void WriteSByte_WhenValueIsProvided_WritesSingleByte()
	{
		var payload = Write((sbyte)-2);

		Assert.Equal([0xFE], payload);
	}

	[Fact]
	public void WriteInt16_WhenValueIsProvided_WritesLittleEndian()
	{
		var payload = Write((short)-1234);

		Assert.Equal([0x2E, 0xFB], payload);
	}

	[Fact]
	public void WriteUInt16_WhenValueIsProvided_WritesLittleEndian()
	{
		var payload = Write((ushort)65000);

		Assert.Equal([0xE8, 0xFD], payload);
	}

	[Fact]
	public void WriteInt32_WhenValueIsProvided_WritesLittleEndian()
	{
		var payload = Write(-123456789);

		Assert.Equal([0xEB, 0x32, 0xA4, 0xF8], payload);
	}

	[Fact]
	public void WriteUInt32_WhenValueIsProvided_WritesLittleEndian()
	{
		var payload = Write(4_000_000_000u);

		Assert.Equal([0x00, 0x28, 0x6B, 0xEE], payload);
	}

	[Fact]
	public void WriteInt64_WhenValueIsProvided_WritesLittleEndian()
	{
		var payload = Write(0x0102030405060708L);

		Assert.Equal([0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01], payload);
	}

	[Fact]
	public void WriteUInt64_WhenValueIsProvided_WritesLittleEndian()
	{
		var payload = Write(0x99AABBCCDDEEFF00UL);

		Assert.Equal([0x00, 0xFF, 0xEE, 0xDD, 0xCC, 0xBB, 0xAA, 0x99], payload);
	}

	[Fact]
	public void WriteInt128_WhenValueIsProvided_WritesLittleEndian()
	{
		var value = (Int128)(-1);

		var payload = Write(value);

		Assert.Equal(
			[0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF],
			payload);
	}

	[Fact]
	public void WriteUInt128_WhenValueIsProvided_WritesLittleEndian()
	{
		var value = ((UInt128)0x1122334455667788UL << 64) | 0x99AABBCCDDEEFF00UL;

		var payload = Write(value);

		Assert.Equal(
			[0x00, 0xFF, 0xEE, 0xDD, 0xCC, 0xBB, 0xAA, 0x99, 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11],
			payload);
	}

	[Fact]
	public void WriteFloat_WhenValueIsProvided_WritesIeeeBitsAsLittleEndian()
	{
		var payload = Write(123.5f);

		Assert.Equal([0x00, 0x00, 0xF7, 0x42], payload);
	}

	[Fact]
	public void WriteDouble_WhenValueIsProvided_WritesIeeeBitsAsLittleEndian()
	{
		var payload = Write(1.5d);

		Assert.Equal([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF8, 0x3F], payload);
	}

	[Fact]
	public void WriteString_WhenValueIsProvided_WritesLengthPrefixedUtf8()
	{
		var payload = Write("hello");

		Assert.Equal(EncodeString("hello"), payload);
	}

	[Fact]
	public void WriteString_WhenValueIsNull_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => Write<string>(null!));
	}

	[Fact]
	public void WriteString_WhenValueContainsInvalidUtf16_ThrowsArgumentException()
	{
		var invalid = "\uD800";

		Assert.Throws<ArgumentException>(() => Write(invalid));
	}

	[Fact]
	public void WriteChar_WhenValueIsBmpCharacter_WritesUtf8Sequence()
	{
		var payload = Write('A');

		Assert.Equal(Encoding.UTF8.GetBytes("A"), payload);
	}

	[Fact]
	public void WriteChar_WhenValueIsInvalidSurrogate_ThrowsArgumentException()
	{
		Assert.Throws<ArgumentException>(() => Write('\uD800'));
	}

	[Fact]
	public void WriteDecimal_WhenValueIsProvided_WritesInvariantText()
	{
		var value = 12345.6789m;

		var payload = Write(value);

		Assert.Equal(EncodeString(value.ToString("G29", System.Globalization.CultureInfo.InvariantCulture)), payload);
	}

	[Fact]
	public void WriteGuid_WhenValueIsProvided_WritesCanonicalDFormat()
	{
		var value = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");

		var payload = Write(value);

		Assert.Equal(EncodeString(value.ToString("D")), payload);
	}

	[Fact]
	public void WriteDateTime_WhenValueIsProvided_WritesRoundTripText()
	{
		var value = new DateTime(2026, 3, 15, 10, 11, 12, DateTimeKind.Utc).AddTicks(1234);

		var payload = Write(value);

		Assert.Equal(EncodeString(value.ToString("O", System.Globalization.CultureInfo.InvariantCulture)), payload);
	}

	[Fact]
	public void WriteDateTimeOffset_WhenValueIsProvided_WritesRoundTripText()
	{
		var value = new DateTimeOffset(2026, 3, 15, 10, 11, 12, TimeSpan.FromHours(2)).AddTicks(1234);

		var payload = Write(value);

		Assert.Equal(EncodeString(value.ToString("O", System.Globalization.CultureInfo.InvariantCulture)), payload);
	}

	[Fact]
	public void WriteTimeSpan_WhenValueIsProvided_WritesRoundedMilliseconds()
	{
		var value = TimeSpan.FromTicks(12_345);

		var payload = Write(value);

		Assert.Equal([0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00], payload);
	}

	[Fact]
	public void WriteTimeSpan_WhenValueIsNegative_WritesRoundedMillisecondsAwayFromZero()
	{
		var value = TimeSpan.FromTicks(-15_000);

		var payload = Write(value);

		Assert.Equal([0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF], payload);
	}

	[Fact]
	public void WriteArray_WhenValuesAreProvided_WritesLengthAndItems()
	{
		var payload = Write(new[] { 10, 20, 30 });

		Assert.Equal(Concat(
			EncodeInt32(3),
			EncodeInt32(10),
			EncodeInt32(20),
			EncodeInt32(30)), payload);
	}

	[Fact]
	public void WriteArray_WhenValueIsNull_ThrowsNullReferenceException()
	{
		Assert.Throws<NullReferenceException>(() => Write<int[]>(null!));
	}

	[Fact]
	public void WriteList_WhenValuesAreProvided_WritesLengthAndItems()
	{
		var payload = Write(new List<int> { 1, 2, 3 });

		Assert.Equal(Concat(
			EncodeInt32(3),
			EncodeInt32(1),
			EncodeInt32(2),
			EncodeInt32(3)), payload);
	}

	[Fact]
	public void WriteList_WhenValueIsNull_ThrowsNullReferenceException()
	{
		Assert.Throws<NullReferenceException>(() => Write<List<int>>(null!));
	}

	[Fact]
	public void WriteDictionary_WhenValueIsNull_ThrowsNullReferenceException()
	{
		Assert.Throws<NullReferenceException>(() => Write<Dictionary<string, int>>(null!));
	}

	[Fact]
	public void WriteNullable_WhenValueIsNull_WritesZeroPrefix()
	{
		var payload = Write<int?>(null);

		Assert.Equal([0], payload);
	}

	[Fact]
	public void WriteNullable_WhenValueIsPresent_WritesOnePrefixAndValue()
	{
		var payload = Write<int?>(42);

		Assert.Equal(Concat([1], EncodeInt32(42)), payload);
	}

	private static byte[] Write<T>(T value)
	{
		using var serviceProvider = CreateServiceProvider();
		var writerServiceType = typeof(IParameterWriter<>).MakeGenericType(typeof(T));
		var writer = (IParameterWriter<T>)serviceProvider.GetParameterReaderOrWriterService(writerServiceType);
		var payloadWriter = new PayloadWriter();
		try
		{
			writer.Write(ref payloadWriter, value);
			return payloadWriter.WrittenSpan.ToArray();
		}
		finally
		{
			payloadWriter.Dispose();
		}
	}

	private static ServiceProvider CreateServiceProvider()
	{
		var services = new ServiceCollection();
		services.AddSparc();
		return services.BuildServiceProvider();
	}

	private static byte[] EncodeInt32(int value)
	{
		var buffer = new byte[sizeof(int)];
		BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
		return buffer;
	}

	private static byte[] EncodeString(string value)
	{
		int byteCount = Encoding.UTF8.GetByteCount(value);
		var result = new byte[sizeof(int) + byteCount];
		BinaryPrimitives.WriteInt32LittleEndian(result, byteCount);
		Encoding.UTF8.GetBytes(value, result.AsSpan(sizeof(int)));
		return result;
	}

	private static byte[] Concat(params byte[][] parts)
	{
		int totalLength = 0;
		foreach (var part in parts)
		{
			totalLength += part.Length;
		}

		var result = new byte[totalLength];
		int offset = 0;
		foreach (var part in parts)
		{
			part.CopyTo(result.AsSpan(offset));
			offset += part.Length;
		}

		return result;
	}
}
