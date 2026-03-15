using System.Buffers.Binary;

namespace Sparc.IO.Readers;

internal sealed class ListParameterReader<T>(IParameterReader<T> itemReader) : IParameterReader<List<T>>
{
	private readonly IParameterReader<T> _itemReader = itemReader;

	public List<T> Read(ref PayloadReader reader)
	{
		var lengthSpan = reader.Read(sizeof(int));
		var length = BinaryPrimitives.ReadInt32LittleEndian(lengthSpan);

		var result = new List<T>(capacity: length);
		for (int i = 0; i < length; i++)
		{
			var item = _itemReader.Read(ref reader);
			result.Add(item);
		}
		return result;
	}
}
