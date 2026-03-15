using System.Buffers.Binary;

namespace Sparc.IO.Writers;

internal sealed class FloatParameterWriter : IParameterWriter<float>
{
	public void Write(ref PayloadWriter writer, float value)
	{
		var target = writer.GetSpan(sizeof(int));
		BinaryPrimitives.WriteInt32LittleEndian(target, BitConverter.SingleToInt32Bits(value));
		writer.Advance(sizeof(int));
	}
}
