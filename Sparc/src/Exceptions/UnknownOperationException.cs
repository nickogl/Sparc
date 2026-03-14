namespace Sparc.Exceptions;

/// <summary>
/// Thrown when an incoming operation ID is unknown to the service contract.
/// </summary>
public sealed class UnknownOperationException : MalformedPayloadException
{
	/// <summary>
	/// Create a new <see cref="UnknownOperationException"/>.
	/// </summary>
	/// <param name="serviceType">Type of service that does not contain the operation.</param>
	/// <param name="operationId">ID of the operation that does not exist on <paramref name="serviceType"/>.</param>
	public UnknownOperationException(Type serviceType, int operationId)
		: base($"Service '{serviceType.FullName}' does not define an operation with ID {operationId}")
	{
	}
}
