using System.Buffers.Binary;

namespace Sparc.IO.Writers;

internal sealed class UInt64ParameterWriter : IParameterWriter<ulong>
{
	public void Write(ref PayloadWriter writer, ulong value)
	{
		var target = writer.GetSpan(sizeof(ulong));
		BinaryPrimitives.WriteUInt64LittleEndian(target, value);
		writer.Advance(sizeof(ulong));
	}
}
