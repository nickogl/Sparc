namespace Sparc.IO.Readers;

internal sealed class SByteParameterReader : IParameterReader<sbyte>
{
	public sbyte Read(ref PayloadReader reader)
	{
		return (sbyte)reader.Read(1)[0];
	}
}
