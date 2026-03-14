namespace Sparc;

/// <summary>
/// Minimal transport-facing abstraction representing a client connection.
/// </summary>
/// <remarks>
/// Transport-specific implementations may expose additional data (for example a connection ID).
/// </remarks>
public interface IClientConnection
{
	/// <summary>
	/// Sends a raw payload to the associated client connection.
	/// </summary>
	ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default);
}
