namespace Sparc.IO.Writers;

internal sealed class ByteParameterWriter : IParameterWriter<byte>
{
	public void Write(ref PayloadWriter writer, byte value)
	{
		var target = writer.GetSpan(1);
		target[0] = value;
		writer.Advance(1);
	}
}
