using System.Buffers.Binary;

namespace Sparc.IO.Writers;

internal sealed class Int16ParameterWriter : IParameterWriter<short>
{
	public void Write(ref PayloadWriter writer, short value)
	{
		var target = writer.GetSpan(sizeof(short));
		BinaryPrimitives.WriteInt16LittleEndian(target, value);
		writer.Advance(sizeof(short));
	}
}
