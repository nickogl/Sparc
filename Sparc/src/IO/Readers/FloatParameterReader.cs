using System.Buffers.Binary;

namespace Sparc.IO.Readers;

internal sealed class FloatParameterReader : IParameterReader<float>
{
	public float Read(ref PayloadReader reader)
	{
		var source = reader.Read(sizeof(int));
		int bits = BinaryPrimitives.ReadInt32LittleEndian(source);
		return BitConverter.Int32BitsToSingle(bits);
	}
}
