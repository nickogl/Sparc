using System.Buffers.Binary;

namespace Sparc.IO.Readers;

internal sealed class Int16ParameterReader : IParameterReader<short>
{
	public short Read(ref PayloadReader reader)
	{
		var source = reader.Read(sizeof(short));
		return BinaryPrimitives.ReadInt16LittleEndian(source);
	}
}
