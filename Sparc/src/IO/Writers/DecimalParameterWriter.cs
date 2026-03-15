using System.Globalization;

namespace Sparc.IO.Writers;

internal sealed class DecimalParameterWriter(IParameterWriter<string> stringWriter) : IParameterWriter<decimal>
{
	private readonly IParameterWriter<string> _stringWriter = stringWriter;

	public void Write(ref PayloadWriter writer, decimal value)
	{
		_stringWriter.Write(ref writer, value.ToString("G29", CultureInfo.InvariantCulture));
	}
}
