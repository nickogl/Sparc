using System.Buffers.Binary;

namespace Sparc.IO.Writers;

internal sealed class Int128ParameterWriter : IParameterWriter<Int128>
{
	public void Write(ref PayloadWriter writer, Int128 value)
	{
		var target = writer.GetSpan(sizeof(long) + sizeof(long));
		BinaryPrimitives.WriteInt128LittleEndian(target, value);
		writer.Advance(sizeof(long) + sizeof(long));
	}
}
