namespace Sparc.IO.Writers;

internal sealed class GuidParameterWriter(IParameterWriter<string> stringWriter) : IParameterWriter<Guid>
{
	private readonly IParameterWriter<string> _stringWriter = stringWriter;

	public void Write(ref PayloadWriter writer, Guid value)
	{
		_stringWriter.Write(ref writer, value.ToString("D"));
	}
}
