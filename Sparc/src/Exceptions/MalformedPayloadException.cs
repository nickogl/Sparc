namespace Sparc.Exceptions;

/// <summary>
/// Base type for all exceptions caused by a malformed payload.
/// </summary>
public abstract class MalformedPayloadException : Exception
{
	/// <inheritdoc/>
	protected MalformedPayloadException()
	{
	}

	/// <inheritdoc/>
	protected MalformedPayloadException(string message) : base(message)
	{
	}
}
