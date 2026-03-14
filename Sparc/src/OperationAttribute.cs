namespace Sparc;

/// <summary>
/// Marks a contract method as a remotely invokable operation. IDs must be unique
/// within a single client/service contract hierarchy, not globally.
/// </summary>
/// <remarks>
/// The operation must return a <see cref="ValueTask"/> and take an application-
/// defined connection type implementing <see cref="IClientConnection"/> as its
/// first parameter, followed by an arbitrary number of RPC parameters. It may
/// also take an optional <see cref="CancellationToken"/> as its last parameter.
/// For client proxies the cancellation token parameter is required.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class OperationAttribute(int operationId) : Attribute
{
	/// <summary>Numeric identifier used to select the target operation.</summary>
	public int OperationId { get; } = operationId;

	/// <summary>
	/// Set the initial size of the buffer used to write this operation's parameters
	/// to call an operation on the client. This does nothing for services, however,
	/// your transport layer can use this value for buffers when receiving data.
	/// </summary>
	/// <remarks>
	/// This is just an optimization to minimize re-pooling memory when the buffer
	/// runs out of space. For operations with fixed payload size, simply set this
	/// to the fixed payload size. For operations with dynamic payload sizes, set 
	/// this to a value that allows most operations to only pool memory once. It
	/// should be based on 
	/// </remarks>
	public int InitialBufferSize { get; set; } = 256;
}
