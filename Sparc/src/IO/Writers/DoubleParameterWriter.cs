using System.Buffers.Binary;

namespace Sparc.IO.Writers;

internal sealed class DoubleParameterWriter : IParameterWriter<double>
{
	public void Write(ref PayloadWriter writer, double value)
	{
		var target = writer.GetSpan(sizeof(long));
		BinaryPrimitives.WriteInt64LittleEndian(target, BitConverter.DoubleToInt64Bits(value));
		writer.Advance(sizeof(long));
	}
}
