namespace Sparc;

/// <summary>
/// Thrown when an incoming payload is truncated for the expected operation contract.
/// </summary>
public sealed class TruncatedPayloadException : Exception
{
	/// <summary>
	/// Create a new <see cref="TruncatedPayloadException"/>.
	/// </summary>
	/// <param name="payloadSize">Size of the payload in bytes.</param>
	/// <param name="consumed">Amount of bytes already consumed.</param>
	/// <param name="attempted">Attempted number of bytes to read.</param>
	public TruncatedPayloadException(int payloadSize, int consumed, int attempted)
		: base($"Attempted to read {attempted} bytes at position {consumed}, but payload size was only {payloadSize} bytes")
	{
	}
}
