using Sparc.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Sparc.IO.Readers;

internal sealed class GuidParameterReader(IParameterReader<string> stringReader) : IParameterReader<Guid>
{
	private readonly IParameterReader<string> _stringReader = stringReader;

	public Guid Read(ref PayloadReader reader)
	{
		var text = _stringReader.Read(ref reader);
		if (!Guid.TryParseExact(text, "D", out var result))
		{
			ThrowInvalidFormat(text);
		}

		return result;
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowInvalidFormat(string value)
	{
		throw new MalformedPayloadException($"Invalid UUID: '{value}'");
	}
}
