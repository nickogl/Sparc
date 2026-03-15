using System.Buffers.Binary;

namespace Sparc.IO.Readers;

internal sealed class Int32ParameterReader : IParameterReader<int>
{
	public int Read(ref PayloadReader reader)
	{
		var source = reader.Read(sizeof(int));
		return BinaryPrimitives.ReadInt32LittleEndian(source);
	}
}
