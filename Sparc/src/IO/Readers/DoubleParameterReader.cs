using System.Buffers.Binary;

namespace Sparc.IO.Readers;

internal sealed class DoubleParameterReader : IParameterReader<double>
{
	public double Read(ref PayloadReader reader)
	{
		var source = reader.Read(sizeof(long));
		long bits = BinaryPrimitives.ReadInt64LittleEndian(source);
		return BitConverter.Int64BitsToDouble(bits);
	}
}
