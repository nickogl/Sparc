using System.Buffers.Binary;

namespace Sparc.IO.Writers;

internal sealed class UInt16ParameterWriter : IParameterWriter<ushort>
{
	public void Write(ref PayloadWriter writer, ushort value)
	{
		var target = writer.GetSpan(sizeof(ushort));
		BinaryPrimitives.WriteUInt16LittleEndian(target, value);
		writer.Advance(sizeof(ushort));
	}
}
