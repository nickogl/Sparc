using System.Buffers;

namespace Sparc.IO;

/// <summary>
/// Forward-only payload writer used by generated client proxy code.
/// </summary>
public ref struct PayloadWriter : IDisposable
{
	private byte[] _buffer;
	private int _written;

	/// <summary>Returns the portion of the internal buffer that has been written.</summary>
	public readonly ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _written);

	/// <summary>Returns the portion of the internal buffer that has been written.</summary>
	public readonly ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _written);

	/// <summary>Returns the number of bytes written.</summary>
	public readonly int Written => _written;

	/// <summary>
	/// Creates a new payload writer.
	/// </summary>
	public PayloadWriter() : this(256)
	{
	}

	/// <summary>
	/// Creates a new payload writer with the provided initial buffer size.
	/// </summary>
	public PayloadWriter(int initialBufferSize)
	{
		_buffer = ArrayPool<byte>.Shared.Rent(initialBufferSize);
		_written = 0;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (_buffer is not null)
		{
			new DisposeToken(ref this).Dispose();
			_buffer = null!;
			_written = 0;
		}
	}

	/// <summary>
	/// Returns a writable span of at least <paramref name="sizeHint"/> bytes.
	/// Implementations may grow the underlying pooled buffer as needed.
	/// </summary>
	public Span<byte> GetSpan(int sizeHint = 0)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);

		EnsureCapacity(sizeHint);
		return _buffer.AsSpan(_written);
	}

	/// <summary>
	/// Advances the writer by the number of bytes that were written into the span
	/// returned by <see cref="GetSpan"/>.
	/// </summary>
	public void Advance(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(_written + count, _buffer.Length, nameof(count));

		_written += count;
	}

	private void EnsureCapacity(int additionalCount)
	{
		int required = _written + additionalCount;
		if (required <= _buffer.Length)
		{
			return;
		}

		int newLength = _buffer.Length;
		while (newLength < required)
		{
			newLength *= 2;
		}

		byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newLength);
		_buffer.AsSpan(0, _written).CopyTo(newBuffer);
		ArrayPool<byte>.Shared.Return(_buffer);
		_buffer = newBuffer;
	}

	/// <summary>
	/// Shared disposer implementation for this type and async methods that need
	/// an escape hatch (ref structs obviously cannot survive across await boundaries)
	/// without duplicating/leaking implementation details.
	/// </summary>
	internal readonly struct DisposeToken(ref PayloadWriter writer) : IDisposable
	{
		private readonly byte[] _buffer = writer._buffer;

		public void Dispose()
		{
			ArrayPool<byte>.Shared.Return(_buffer);
		}
	}
}
