using System.Buffers.Binary;

namespace Sparc.IO.Readers;

internal sealed class Int64ParameterReader : IParameterReader<long>
{
	public long Read(ref PayloadReader reader)
	{
		var source = reader.Read(sizeof(long));
		return BinaryPrimitives.ReadInt64LittleEndian(source);
	}
}
