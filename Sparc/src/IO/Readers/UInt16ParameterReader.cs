using System.Buffers.Binary;

namespace Sparc.IO.Readers;

internal sealed class UInt16ParameterReader : IParameterReader<ushort>
{
	public ushort Read(ref PayloadReader reader)
	{
		var source = reader.Read(sizeof(ushort));
		return BinaryPrimitives.ReadUInt16LittleEndian(source);
	}
}
