using Sparc.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Sparc.IO.Readers;

internal sealed class DecimalParameterReader(IParameterReader<string> stringReader) : IParameterReader<decimal>
{
	private readonly IParameterReader<string> _stringReader = stringReader;

	public decimal Read(ref PayloadReader reader)
	{
		var text = _stringReader.Read(ref reader);
		if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			ThrowInvalidFormat(text);
		}

		return result;
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowInvalidFormat(string value)
	{
		throw new MalformedPayloadException($"Invalid decimal number: '{value}'");
	}
}
