using System.Globalization;

namespace Sparc.IO.Writers;

internal sealed class DateTimeParameterWriter(IParameterWriter<string> stringWriter) : IParameterWriter<DateTime>
{
	private readonly IParameterWriter<string> _stringWriter = stringWriter;

	public void Write(ref PayloadWriter writer, DateTime value)
	{
		_stringWriter.Write(ref writer, value.ToString("O", CultureInfo.InvariantCulture));
	}
}
