using Sparc.Exceptions;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sparc.IO.Readers;

internal sealed class CharParameterReader : IParameterReader<char>
{
	public char Read(ref PayloadReader reader)
	{
		var status = Rune.DecodeFromUtf8(reader.AvailableSpan, out var result, out int count);
		if (status == OperationStatus.NeedMoreData)
		{
			ThrowPayloadTruncated(ref reader, count);
		}
		if (status == OperationStatus.InvalidData)
		{
			ThrowInvalidUtf8(count);
		}
		if (!result.IsBmp)
		{
			ThrowOutsideBasicMultilingualPlane(result);
		}

		reader.Advance(count);

		return (char)result.Value;
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowPayloadTruncated(ref PayloadReader reader, int count)
	{
		throw new TruncatedPayloadException(reader.Consumed + reader.AvailableSpan.Length, reader.Consumed, count + 1);
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowInvalidUtf8(int count)
	{
		throw new MalformedPayloadException($"Invalid UTF-8 character, aborted after reading {count} bytes");
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowOutsideBasicMultilingualPlane(Rune rune)
	{
		throw new MalformedPayloadException($"Parsed character U+{rune.Value:X4} is not in the basic multilingual plane");
	}
}
