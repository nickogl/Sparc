using System.Buffers.Binary;

namespace Sparc.IO.Writers;

internal sealed class UInt128ParameterWriter : IParameterWriter<UInt128>
{
	public void Write(ref PayloadWriter writer, UInt128 value)
	{
		var target = writer.GetSpan(sizeof(long) + sizeof(long));
		BinaryPrimitives.WriteUInt128LittleEndian(target, value);
		writer.Advance(sizeof(long) + sizeof(long));
	}
}
