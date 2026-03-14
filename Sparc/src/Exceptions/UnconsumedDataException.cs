namespace Sparc.Exceptions;

/// <summary>
/// Thrown when an incoming payload has unconsumed data after processing.
/// </summary>
public sealed class UnconsumedDataException : MalformedPayloadException
{
	/// <summary>
	/// Create a new <see cref="UnconsumedDataException"/>.
	/// </summary>
	public UnconsumedDataException(int operationId)
		: base($"Payload contains unconsumed data after reading all parameters for operation with ID {operationId}")
	{
	}
}
