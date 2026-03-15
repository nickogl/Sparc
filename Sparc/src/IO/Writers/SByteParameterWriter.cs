namespace Sparc.IO.Writers;

internal sealed class SByteParameterWriter : IParameterWriter<sbyte>
{
	public void Write(ref PayloadWriter writer, sbyte value)
	{
		var target = writer.GetSpan(1);
		target[0] = (byte)value;
		writer.Advance(1);
	}
}
