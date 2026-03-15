namespace Sparc.Exceptions;

/// <summary>
/// Base type for all exceptions caused by a malformed payload.
/// </summary>
public class MalformedPayloadException : Exception
{
	/// <inheritdoc/>
	public MalformedPayloadException()
	{
	}

	/// <inheritdoc/>
	public MalformedPayloadException(string message) : base(message)
	{
	}

	/// <inheritdoc/>
	public MalformedPayloadException(string message, Exception inner) : base(message, inner)
	{
	}
}
