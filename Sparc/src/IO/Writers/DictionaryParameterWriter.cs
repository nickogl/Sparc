using System.Buffers.Binary;

namespace Sparc.IO.Writers;

internal sealed class DictionaryParameterWriter<TKey, TValue>(
	IParameterWriter<TKey> keyWriter,
	IParameterWriter<TValue> valueWriter) : IParameterWriter<Dictionary<TKey, TValue>>
	where TKey : notnull
{
	private readonly IParameterWriter<TKey> _keyWriter = keyWriter;
	private readonly IParameterWriter<TValue> _valueWriter = valueWriter;

	public void Write(ref PayloadWriter writer, Dictionary<TKey, TValue> value)
	{
		var lengthTarget = writer.GetSpan(sizeof(int));
		BinaryPrimitives.WriteInt32LittleEndian(lengthTarget, value.Count);
		writer.Advance(sizeof(int));

		foreach (var (key, entryValue) in value)
		{
			_keyWriter.Write(ref writer, key);
			_valueWriter.Write(ref writer, entryValue);
		}
	}
}
