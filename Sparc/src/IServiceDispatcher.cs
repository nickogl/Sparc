using Sparc.Exceptions;

namespace Sparc;

/// <summary>
/// Dispatches incoming operation payloads to a bound service instance.
/// </summary>
/// <typeparam name="TService">The consumer-defined service contract implementation type.</typeparam>
/// <typeparam name="TConnection">The concrete connection type used by the transport/application.</typeparam>
public interface IServiceDispatcher<TService, TConnection>
	where TService : class
	where TConnection : IClientConnection
{
	/// <summary>
	/// Dispatches an incoming operation payload to the bound service instance.
	/// </summary>
	/// <remarks>
	/// The transport layer is responsible for reading the operation ID (32-bit little endian).
	/// This allows the transport layer to implement some additional operation-dependent
	/// functionality, like enforcing different payload size limits for them.
	/// </remarks>
	/// <param name="operationId">ID of the operation to invoke.</param>
	/// <param name="payload">Full payload to read parameters from.</param>
	/// <param name="connection">Connection of the client who invokes the operation.</param>
	/// <param name="cancellationToken">Token to monitor for cancellation.</param>
	/// <exception cref="TruncatedPayloadException">Payload was too small for expected parameters.</exception>
	/// <exception cref="UnconsumedDataException">Payload contained leftover data after reading all parameters.</exception>
	/// <exception cref="UnknownOperationException"><typeparamref name="TService"/> does not contain an operation with ID <paramref name="operationId"/>.</exception>
	ValueTask DispatchAsync(
		int operationId,
		ReadOnlySpan<byte> payload,
		TConnection connection,
		CancellationToken cancellationToken = default);
}
