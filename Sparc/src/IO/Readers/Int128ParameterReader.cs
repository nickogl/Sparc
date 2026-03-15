using System.Buffers.Binary;

namespace Sparc.IO.Readers;

internal sealed class Int128ParameterReader : IParameterReader<Int128>
{
	public Int128 Read(ref PayloadReader reader)
	{
		var source = reader.Read(sizeof(long) + sizeof(long));
		return BinaryPrimitives.ReadInt128LittleEndian(source);
	}
}
