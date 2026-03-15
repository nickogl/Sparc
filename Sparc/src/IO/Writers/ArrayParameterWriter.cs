using System.Buffers.Binary;

namespace Sparc.IO.Writers;

internal sealed class ArrayParameterWriter<T>(IParameterWriter<T> itemWriter) : IParameterWriter<T[]>
{
	private readonly IParameterWriter<T> _itemWriter = itemWriter;

	public void Write(ref PayloadWriter writer, T[] value)
	{
		var lengthTarget = writer.GetSpan(sizeof(int));
		BinaryPrimitives.WriteInt32LittleEndian(lengthTarget, value.Length);
		writer.Advance(sizeof(int));

		foreach (var item in value)
		{
			_itemWriter.Write(ref writer, item);
		}
	}
}
