using Sparc.Exceptions;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Sparc.IO.Readers;

internal sealed class DictionaryParameterReader<TKey, TValue> : IParameterReader<Dictionary<TKey, TValue>>
	where TKey : notnull
{
	private readonly IParameterReader<TKey> _keyReader;
	private readonly IParameterReader<TValue> _valueReader;

	public DictionaryParameterReader(IParameterReader<TKey> keyReader, IParameterReader<TValue> valueReader)
	{
		_keyReader = keyReader;
		_valueReader = valueReader;
	}

	public Dictionary<TKey, TValue> Read(ref PayloadReader reader)
	{
		var lengthSpan = reader.Read(sizeof(int));
		var length = BinaryPrimitives.ReadInt32LittleEndian(lengthSpan);

		var result = new Dictionary<TKey, TValue>(capacity: length);
		for (int i = 0; i < length; i++)
		{
			var key = _keyReader.Read(ref reader);
			var value = _valueReader.Read(ref reader);
			if (!result.TryAdd(key, value))
			{
				ThrowDuplicateKey(key);
			}
		}
		return result;
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowDuplicateKey(TKey key)
	{
		throw new MalformedPayloadException($"Duplicate key '{key}'");
	}
}
