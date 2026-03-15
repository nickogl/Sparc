using Sparc.Exceptions;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sparc.IO.Readers;

internal sealed class StringParameterReader : IParameterReader<string>
{
	private static readonly Encoding StrictUtf8Encoding = new UTF8Encoding(
		encoderShouldEmitUTF8Identifier: false,
		throwOnInvalidBytes: true);

	public string Read(ref PayloadReader reader)
	{
		var span = reader.Read(sizeof(int));
		var byteLength = BinaryPrimitives.ReadInt32LittleEndian(span);
		span = reader.Read(byteLength);
		return DecodeUtf8String(span);
	}

	private static string DecodeUtf8String(ReadOnlySpan<byte> bytes)
	{
		try
		{
			return StrictUtf8Encoding.GetString(bytes);
		}
		catch (DecoderFallbackException e)
		{
			ThrowInvalidUtf8(e);
			throw;
		}
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowInvalidUtf8(Exception inner)
	{
		throw new MalformedPayloadException("Invalid UTF-8 string");
	}
}
