using System.Buffers.Binary;

namespace Sparc.IO.Readers;

internal sealed class UInt128ParameterReader : IParameterReader<UInt128>
{
	public UInt128 Read(ref PayloadReader reader)
	{
		var source = reader.Read(sizeof(long) + sizeof(long));
		return BinaryPrimitives.ReadUInt128LittleEndian(source);
	}
}
