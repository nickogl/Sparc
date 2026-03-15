using Sparc.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Sparc.IO.Readers;

internal sealed class NullableParameterReader<T>(IParameterReader<T> valueReader) : IParameterReader<T?>
	where T : struct
{
	private readonly IParameterReader<T> _valueReader = valueReader;

	public T? Read(ref PayloadReader reader)
	{
		var prefix = reader.Read(1)[0];
		if (prefix == 0)
		{
			return default;
		}
		if (prefix != 1)
		{
			ThrowInvalidPrefix(prefix);
		}

		return _valueReader.Read(ref reader);
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowInvalidPrefix(byte prefix)
	{
		throw new MalformedPayloadException($"Invalid prefix '{prefix}', must be 0 (null) or 1 (non-null)");
	}
}
