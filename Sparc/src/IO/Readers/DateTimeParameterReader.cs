using Sparc.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Sparc.IO.Readers;

internal sealed class DateTimeParameterReader(IParameterReader<string> stringReader) : IParameterReader<DateTime>
{
	private readonly IParameterReader<string> _stringReader = stringReader;

	public DateTime Read(ref PayloadReader reader)
	{
		var text = _stringReader.Read(ref reader);
		if (!DateTime.TryParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
		{
			ThrowInvalidFormat(text);
		}

		return result;
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowInvalidFormat(string value)
	{
		throw new MalformedPayloadException($"Invalid DateTime in round-trip format: '{value}'");
	}
}
