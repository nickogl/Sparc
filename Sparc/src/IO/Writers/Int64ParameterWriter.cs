using System.Buffers.Binary;

namespace Sparc.IO.Writers;

internal sealed class Int64ParameterWriter : IParameterWriter<long>
{
	public void Write(ref PayloadWriter writer, long value)
	{
		var target = writer.GetSpan(sizeof(long));
		BinaryPrimitives.WriteInt64LittleEndian(target, value);
		writer.Advance(sizeof(long));
	}
}
