namespace Sparc.IO;

/// <summary>
/// Forward-only payload reader used by generated service dispatch code.
/// </summary>
public ref struct PayloadReader(ReadOnlySpan<byte> buffer)
{
	private readonly ReadOnlySpan<byte> _buffer = buffer;
	private int _consumed = 0;

	/// <summary>Number of bytes consumed thus far.</summary>
	public readonly int Consumed => _consumed;

	/// <summary>Returns true if the cursor reached the end of the payload.</summary>
	public readonly bool End => _consumed == _buffer.Length;

	/// <summary>Returns the available span strictly for processing delimiter-based payloads.</summary>
	/// <remarks>Use <see cref="Advance"/> to communicate the amount of actual bytes read.</remarks>
	public readonly ReadOnlySpan<byte> AvailableSpan => _buffer[_consumed..];

	/// <summary>
	/// Advances the reader by the number of bytes that were read from the span
	/// returned by <see cref="AvailableSpan"/>.
	/// </summary>
	/// <remarks>
	/// Use only for delimiter-based payload processing.
	/// </remarks>
	public void Advance(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(_consumed + count, _buffer.Length, nameof(count));

		_consumed += count;
	}

	/// <summary>
	/// Reads a slice of the requested length and advances the cursor.
	/// </summary>
	/// <remarks>
	/// If your payload is delimiter-based, use <see cref="AvailableSpan"/> together with <see cref="Advance"/>.
	/// </remarks>
	/// <exception cref="TruncatedPayloadException">
	/// Thrown when the payload does not contain enough remaining bytes.
	/// </exception>
	public ReadOnlySpan<byte> Read(int length)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(length);
		if (_consumed + length > _buffer.Length)
		{
			throw new TruncatedPayloadException(_buffer.Length, _consumed, length);
		}

		var slice = _buffer.Slice(_consumed, length);
		_consumed += length;
		return slice;
	}
}
