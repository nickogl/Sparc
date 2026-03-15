using System.Buffers.Binary;

namespace Sparc.IO.Writers;

internal sealed class ListParameterWriter<T>(IParameterWriter<T> itemWriter) : IParameterWriter<List<T>>
{
	private readonly IParameterWriter<T> _itemWriter = itemWriter;

	public void Write(ref PayloadWriter writer, List<T> value)
	{
		var lengthTarget = writer.GetSpan(sizeof(int));
		BinaryPrimitives.WriteInt32LittleEndian(lengthTarget, value.Count);
		writer.Advance(sizeof(int));

		foreach (var item in value)
		{
			_itemWriter.Write(ref writer, item);
		}
	}
}
