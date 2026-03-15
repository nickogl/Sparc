using System.Globalization;

namespace Sparc.IO.Writers;

internal sealed class DateTimeOffsetParameterWriter(IParameterWriter<string> stringWriter) : IParameterWriter<DateTimeOffset>
{
	private readonly IParameterWriter<string> _stringWriter = stringWriter;

	public void Write(ref PayloadWriter writer, DateTimeOffset value)
	{
		_stringWriter.Write(ref writer, value.ToString("O", CultureInfo.InvariantCulture));
	}
}
