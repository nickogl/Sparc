using System.Buffers.Binary;

namespace Sparc.IO.Readers;

internal sealed class UInt32ParameterReader : IParameterReader<uint>
{
	public uint Read(ref PayloadReader reader)
	{
		var source = reader.Read(sizeof(uint));
		return BinaryPrimitives.ReadUInt32LittleEndian(source);
	}
}
