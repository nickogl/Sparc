using System.Buffers.Binary;

namespace Sparc.IO.Readers;

internal sealed class UInt64ParameterReader : IParameterReader<ulong>
{
	public ulong Read(ref PayloadReader reader)
	{
		var source = reader.Read(sizeof(ulong));
		return BinaryPrimitives.ReadUInt64LittleEndian(source);
	}
}
