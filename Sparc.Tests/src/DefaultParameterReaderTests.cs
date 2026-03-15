using Microsoft.Extensions.DependencyInjection;
using Sparc.Exceptions;
using Sparc.IO;
using System.Buffers.Binary;
using System.Text;

namespace Sparc.Tests;

// NOTE: These tests also test DI already. This is useful when adding new default
// readers, as they will not work if one forgets to extend the DI extension methods.
public class DefaultReaderTests
{
	[Fact]
	public void Boolean_WhenPayloadIsValid_ReturnsValue()
	{
		var result = Read<bool>([1]);

		Assert.True(result);
	}

	[Fact]
	public void Boolean_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		Assert.Throws<TruncatedPayloadException>(() => Read<bool>([]));
	}

	[Fact]
	public void Byte_WhenPayloadIsValid_ReturnsValue()
	{
		var result = Read<byte>([0xAB]);

		Assert.Equal(0xAB, result);
	}

	[Fact]
	public void Byte_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		Assert.Throws<TruncatedPayloadException>(() => Read<byte>([]));
	}

	[Fact]
	public void SByte_WhenPayloadIsValid_ReturnsValue()
	{
		var result = Read<sbyte>([(byte)0xFE]);

		Assert.Equal((sbyte)-2, result);
	}

	[Fact]
	public void SByte_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		Assert.Throws<TruncatedPayloadException>(() => Read<sbyte>([]));
	}

	[Fact]
	public void Int16_WhenPayloadIsValid_ReturnsValue()
	{
		var payload = new byte[sizeof(short)];
		BinaryPrimitives.WriteInt16LittleEndian(payload, -1234);

		var result = Read<short>(payload);

		Assert.Equal((short)-1234, result);
	}

	[Fact]
	public void Int16_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		Assert.Throws<TruncatedPayloadException>(() => Read<short>([0x01]));
	}

	[Fact]
	public void UInt16_WhenPayloadIsValid_ReturnsValue()
	{
		var payload = new byte[sizeof(ushort)];
		BinaryPrimitives.WriteUInt16LittleEndian(payload, 65000);

		var result = Read<ushort>(payload);

		Assert.Equal((ushort)65000, result);
	}

	[Fact]
	public void UInt16_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		Assert.Throws<TruncatedPayloadException>(() => Read<ushort>([0x01]));
	}

	[Fact]
	public void Int32_WhenPayloadIsValid_ReturnsValue()
	{
		var payload = new byte[sizeof(int)];
		BinaryPrimitives.WriteInt32LittleEndian(payload, -123456789);

		var result = Read<int>(payload);

		Assert.Equal(-123456789, result);
	}

	[Fact]
	public void Int32_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		Assert.Throws<TruncatedPayloadException>(() => Read<int>([1, 2, 3]));
	}

	[Fact]
	public void UInt32_WhenPayloadIsValid_ReturnsValue()
	{
		var payload = new byte[sizeof(uint)];
		BinaryPrimitives.WriteUInt32LittleEndian(payload, 4_000_000_000);

		var result = Read<uint>(payload);

		Assert.Equal(4_000_000_000, result);
	}

	[Fact]
	public void UInt32_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		Assert.Throws<TruncatedPayloadException>(() => Read<uint>([1, 2, 3]));
	}

	[Fact]
	public void Int64_WhenPayloadIsValid_ReturnsValue()
	{
		var payload = new byte[sizeof(long)];
		BinaryPrimitives.WriteInt64LittleEndian(payload, -9_000_000_000_000_000_000);

		var result = Read<long>(payload);

		Assert.Equal(-9_000_000_000_000_000_000, result);
	}

	[Fact]
	public void Int64_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		Assert.Throws<TruncatedPayloadException>(() => Read<long>([1, 2, 3, 4, 5, 6, 7]));
	}

	[Fact]
	public void UInt64_WhenPayloadIsValid_ReturnsValue()
	{
		var payload = new byte[sizeof(ulong)];
		BinaryPrimitives.WriteUInt64LittleEndian(payload, 18_000_000_000_000_000_000);

		var result = Read<ulong>(payload);

		Assert.Equal(18_000_000_000_000_000_000, result);
	}

	[Fact]
	public void UInt64_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		Assert.Throws<TruncatedPayloadException>(() => Read<ulong>([1, 2, 3, 4, 5, 6, 7]));
	}

	[Fact]
	public void Int128_WhenPayloadIsValid_ReturnsValue()
	{
		var payload = new byte[16];
		var expected = Int128.Parse("-17014118346046923173168730371588410572");
		BinaryPrimitives.WriteInt128LittleEndian(payload, expected);

		var result = Read<Int128>(payload);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void Int128_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		Assert.Throws<TruncatedPayloadException>(() => Read<Int128>(new byte[15]));
	}

	[Fact]
	public void UInt128_WhenPayloadIsValid_ReturnsValue()
	{
		var payload = new byte[16];
		var expected = UInt128.Parse("34028236692093846346337460743176821145");
		BinaryPrimitives.WriteUInt128LittleEndian(payload, expected);

		var result = Read<UInt128>(payload);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void UInt128_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		Assert.Throws<TruncatedPayloadException>(() => Read<UInt128>(new byte[15]));
	}

	[Fact]
	public void Float_WhenPayloadIsValid_ReturnsValue()
	{
		var payload = new byte[sizeof(int)];
		BinaryPrimitives.WriteInt32LittleEndian(payload, BitConverter.SingleToInt32Bits(123.5f));

		var result = Read<float>(payload);

		Assert.Equal(123.5f, result);
	}

	[Fact]
	public void Float_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		Assert.Throws<TruncatedPayloadException>(() => Read<float>([1, 2, 3]));
	}

	[Fact]
	public void Double_WhenPayloadIsValid_ReturnsValue()
	{
		var payload = new byte[sizeof(long)];
		BinaryPrimitives.WriteInt64LittleEndian(payload, BitConverter.DoubleToInt64Bits(12345.6789));

		var result = Read<double>(payload);

		Assert.Equal(12345.6789, result);
	}

	[Fact]
	public void Double_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		Assert.Throws<TruncatedPayloadException>(() => Read<double>([1, 2, 3, 4, 5, 6, 7]));
	}

	[Fact]
	public void String_WhenPayloadIsValid_ReturnsValue()
	{
		var result = Read<string>(EncodeString("hello"));

		Assert.Equal("hello", result);
	}

	[Fact]
	public void String_WhenPayloadContainsInvalidUtf8_ThrowsMalformedPayloadException()
	{
		var payload = new byte[sizeof(int) + 2];
		BinaryPrimitives.WriteInt32LittleEndian(payload, 2);
		payload[4] = 0xC3;
		payload[5] = 0x28;

		Assert.Throws<MalformedPayloadException>(() => Read<string>(payload));
	}

	[Fact]
	public void Char_WhenPayloadIsValid_ReturnsValue()
	{
		var result = Read<char>(Encoding.UTF8.GetBytes("A"));

		Assert.Equal('A', result);
	}

	[Fact]
	public void Char_WhenPayloadContainsInvalidUtf8_ThrowsMalformedPayloadException()
	{
		Assert.Throws<MalformedPayloadException>(() => Read<char>([0xC3, 0x28]));
	}

	[Fact]
	public void Char_WhenPayloadContainsNonBmpRune_ThrowsMalformedPayloadException()
	{
		Assert.Throws<MalformedPayloadException>(() => Read<char>(Encoding.UTF8.GetBytes("😀")));
	}

	[Fact]
	public void Decimal_WhenPayloadIsValid_ReturnsValue()
	{
		var result = Read<decimal>(EncodeString("12345.6789"));

		Assert.Equal(12345.6789m, result);
	}

	[Fact]
	public void Decimal_WhenPayloadContainsInvalidNumber_ThrowsMalformedPayloadException()
	{
		Assert.Throws<MalformedPayloadException>(() => Read<decimal>(EncodeString("not-a-decimal")));
	}

	[Fact]
	public void Guid_WhenPayloadIsValid_ReturnsValue()
	{
		var expected = Guid.NewGuid();

		var result = Read<Guid>(EncodeString(expected.ToString("D")));

		Assert.Equal(expected, result);
	}

	[Fact]
	public void Guid_WhenPayloadContainsInvalidValue_ThrowsMalformedPayloadException()
	{
		Assert.Throws<MalformedPayloadException>(() => Read<Guid>(EncodeString("definitely-not-a-guid")));
	}

	[Fact]
	public void DateTime_WhenPayloadIsValid_ReturnsValue()
	{
		var expected = new DateTime(2026, 3, 15, 10, 11, 12, DateTimeKind.Utc).AddTicks(1234);

		var result = Read<DateTime>(EncodeString(expected.ToString("O")));

		Assert.Equal(expected, result);
	}

	[Fact]
	public void DateTime_WhenPayloadContainsInvalidValue_ThrowsMalformedPayloadException()
	{
		Assert.Throws<MalformedPayloadException>(() => Read<DateTime>(EncodeString("2026-99-99T99:99:99.9999999Z")));
	}

	[Fact]
	public void DateTimeOffset_WhenPayloadIsValid_ReturnsValue()
	{
		var expected = new DateTimeOffset(2026, 3, 15, 10, 11, 12, TimeSpan.FromHours(2)).AddTicks(1234);

		var result = Read<DateTimeOffset>(EncodeString(expected.ToString("O")));

		Assert.Equal(expected, result);
	}

	[Fact]
	public void DateTimeOffset_WhenPayloadContainsInvalidValue_ThrowsMalformedPayloadException()
	{
		Assert.Throws<MalformedPayloadException>(() => Read<DateTimeOffset>(EncodeString("2026-99-99T99:99:99.9999999+99:99")));
	}

	[Fact]
	public void TimeSpan_WhenPayloadIsValid_ReturnsValue()
	{
		var payload = new byte[sizeof(long)];
		BinaryPrimitives.WriteInt64LittleEndian(payload, 1500);

		var result = Read<TimeSpan>(payload);

		Assert.Equal(TimeSpan.FromMilliseconds(1500), result);
	}

	[Fact]
	public void TimeSpan_WhenPayloadOverflows_ThrowsMalformedPayloadException()
	{
		var payload = new byte[sizeof(long)];
		BinaryPrimitives.WriteInt64LittleEndian(payload, long.MaxValue);

		Assert.Throws<MalformedPayloadException>(() => Read<TimeSpan>(payload));
	}

	[Fact]
	public void Array_WhenPayloadIsValid_ReturnsValues()
	{
		var payload = Concat(
			EncodeInt32(3),
			EncodeInt32(10),
			EncodeInt32(20),
			EncodeInt32(30));

		var result = Read<int[]>(payload);

		Assert.Equal([10, 20, 30], result);
	}

	[Fact]
	public void Array_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		var payload = Concat(
			EncodeInt32(2),
			EncodeInt32(10));

		Assert.Throws<TruncatedPayloadException>(() => Read<int[]>(payload));
	}

	[Fact]
	public void List_WhenPayloadIsValid_ReturnsValues()
	{
		var payload = Concat(
			EncodeInt32(3),
			EncodeInt32(1),
			EncodeInt32(2),
			EncodeInt32(3));

		var result = Read<List<int>>(payload);

		Assert.Equal([1, 2, 3], result);
	}

	[Fact]
	public void List_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		var payload = Concat(
			EncodeInt32(2),
			EncodeInt32(10));

		Assert.Throws<TruncatedPayloadException>(() => Read<List<int>>(payload));
	}

	[Fact]
	public void Dictionary_WhenPayloadIsValid_ReturnsValues()
	{
		var payload = Concat(
			EncodeInt32(2),
			EncodeString("a"), EncodeInt32(1),
			EncodeString("b"), EncodeInt32(2));

		var result = Read<Dictionary<string, int>>(payload);

		Assert.Equal(2, result.Count);
		Assert.Equal(1, result["a"]);
		Assert.Equal(2, result["b"]);
	}

	[Fact]
	public void Dictionary_WhenPayloadContainsDuplicateKey_ThrowsMalformedPayloadException()
	{
		var payload = Concat(
			EncodeInt32(2),
			EncodeString("a"), EncodeInt32(1),
			EncodeString("a"), EncodeInt32(2));

		Assert.Throws<MalformedPayloadException>(() => Read<Dictionary<string, int>>(payload));
	}

	[Fact]
	public void Nullable_WhenPrefixIsZero_ReturnsNull()
	{
		var result = Read<int?>([0]);

		Assert.Null(result);
	}

	[Fact]
	public void Nullable_WhenPrefixIsOne_ReturnsValue()
	{
		var payload = Concat([1], EncodeInt32(42));

		var result = Read<int?>(payload);

		Assert.Equal(42, result);
	}

	[Fact]
	public void Nullable_WhenPrefixIsInvalid_ThrowsMalformedPayloadException()
	{
		Assert.Throws<MalformedPayloadException>(() => Read<int?>([2]));
	}

	private static T Read<T>(byte[] payload)
	{
		using var serviceProvider = CreateServiceProvider();
		var service = (IParameterReader<T>)serviceProvider.GetParameterReaderOrWriterService(typeof(IParameterReader<T>));
		var reader = new PayloadReader(payload);
		return service.Read(ref reader);
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
		int length = 0;
		foreach (var part in parts)
		{
			length += part.Length;
		}

		var result = new byte[length];
		int offset = 0;
		foreach (var part in parts)
		{
			part.CopyTo(result.AsSpan(offset));
			offset += part.Length;
		}
		return result;
	}
}
