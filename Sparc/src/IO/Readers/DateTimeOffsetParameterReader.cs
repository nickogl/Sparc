using Sparc.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Sparc.IO.Readers;

internal sealed class DateTimeOffsetParameterReader(IParameterReader<string> stringReader) : IParameterReader<DateTimeOffset>
{
	private readonly IParameterReader<string> _stringReader = stringReader;

	public DateTimeOffset Read(ref PayloadReader reader)
	{
		var text = _stringReader.Read(ref reader);
		if (!DateTimeOffset.TryParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
		{
			ThrowInvalidFormat(text);
		}

		return result;
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowInvalidFormat(string value)
	{
		throw new MalformedPayloadException($"Invalid DateTimeOffset in round-trip format: '{value}'");
	}
}
