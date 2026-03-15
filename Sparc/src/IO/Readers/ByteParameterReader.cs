namespace Sparc.IO.Readers;

internal sealed class ByteParameterReader : IParameterReader<byte>
{
	public byte Read(ref PayloadReader reader)
	{
		return reader.Read(1)[0];
	}
}
