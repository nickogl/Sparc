using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sparc.IO.Writers;

internal sealed class StringParameterWriter : IParameterWriter<string>
{
	private static readonly Encoding StrictUtf8Encoding = new UTF8Encoding(
		encoderShouldEmitUTF8Identifier: false,
		throwOnInvalidBytes: true);

	public void Write(ref PayloadWriter writer, string value)
	{
		int byteCount = GetByteCount(value);
		var target = writer.GetSpan(sizeof(int));
		BinaryPrimitives.WriteInt32LittleEndian(target, byteCount);
		writer.Advance(sizeof(int));

		target = writer.GetSpan(byteCount);
		StrictUtf8Encoding.GetBytes(value, target);
		writer.Advance(byteCount);
	}

	private static int GetByteCount(string value)
	{
		try
		{
			return StrictUtf8Encoding.GetByteCount(value);
		}
		catch (EncoderFallbackException inner)
		{
			ThrowInvalidUtf16String(inner);
			throw;
		}
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowInvalidUtf16String(Exception inner)
	{
		throw new ArgumentException("String contains invalid UTF-16 data", inner);
	}
}
