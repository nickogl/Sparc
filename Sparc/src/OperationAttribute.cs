using System.Reflection;

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
	/// your transport layer can use this value for buffers when receiving data. This
	/// value also includes the operation ID (4 bytes) at the beginning of the payload.
	/// </summary>
	/// <remarks>
	/// This is just an optimization to minimize re-pooling memory when the buffer
	/// runs out of space. For operations with fixed payload size, simply set this
	/// to the fixed payload size. For operations with dynamic payload sizes, set 
	/// this to a value that allows most operations to only pool memory once. It
	/// should be tuned based on the recorded metrics in the wild.
	/// </remarks>
	public int InitialBufferSize { get; set; } = 256;

	/// <summary>
	/// Set the maximum payload size for inbound messages. Sparc does not use this
	/// value; instead, your transport layer should enforce this while reading the
	/// payload. It is just included at the consumer's convenience.
	/// </summary>
	public int? MaximumPayloadSize { get; set; }

	/// <summary>
	/// Discover all operations of a service <typeparamref name="T"/> across its
	/// inheritance hierarchy and all its implemented interfaces.
	/// </summary>
	/// <remarks>
	/// Consumers can use this method in their transport layer and cache metadata
	/// for message payload size enforcement or buffer sizing, for example.
	/// </remarks>
	/// <typeparam name="T">Service type whose operations to discover.</typeparam>
	/// <returns>An iterator over all methods and their operation attributes.</returns>
	public static IEnumerable<(MethodInfo, OperationAttribute)> FindOperations<T>()
	{
		foreach (var implementedInterface in typeof(T).GetInterfaces().Append(typeof(T)))
		{
			foreach (var method in implementedInterface.GetMethods())
			{
				var operation = method.GetCustomAttribute<OperationAttribute>();
				if (operation is not null)
				{
					yield return (method, operation);
				}
			}
		}
	}
}
