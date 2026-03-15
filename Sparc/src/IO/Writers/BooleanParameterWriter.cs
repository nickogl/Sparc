namespace Sparc.IO.Writers;

internal sealed class BooleanParameterWriter : IParameterWriter<bool>
{
	public void Write(ref PayloadWriter writer, bool value)
	{
		var target = writer.GetSpan(1);
		target[0] = value ? (byte)1 : (byte)0;
		writer.Advance(1);
	}
}
