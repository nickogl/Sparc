using System.Buffers.Binary;

namespace Sparc.IO.Writers;

internal sealed class Int32ParameterWriter : IParameterWriter<int>
{
	public void Write(ref PayloadWriter writer, int value)
	{
		var target = writer.GetSpan(sizeof(int));
		BinaryPrimitives.WriteInt32LittleEndian(target, value);
		writer.Advance(sizeof(int));
	}
}
