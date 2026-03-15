using System.Buffers.Binary;

namespace Sparc.IO.Readers;

internal sealed class ArrayParameterReader<T>(IParameterReader<T> itemReader) : IParameterReader<T[]>
{
	private readonly IParameterReader<T> _itemReader = itemReader;

	public T[] Read(ref PayloadReader reader)
	{
		var lengthSpan = reader.Read(sizeof(int));
		var length = BinaryPrimitives.ReadInt32LittleEndian(lengthSpan);

		var result = new T[length];
		for (int i = 0; i < length; i++)
		{
			result[i] = _itemReader.Read(ref reader);
		}
		return result;
	}
}
