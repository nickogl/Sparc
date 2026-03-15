using System.Buffers.Binary;

namespace Sparc.IO.Writers;

internal sealed class UInt32ParameterWriter : IParameterWriter<uint>
{
	public void Write(ref PayloadWriter writer, uint value)
	{
		var target = writer.GetSpan(sizeof(uint));
		BinaryPrimitives.WriteUInt32LittleEndian(target, value);
		writer.Advance(sizeof(uint));
	}
}
