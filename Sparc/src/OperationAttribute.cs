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
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class OperationAttribute(int operationId) : Attribute
{
	/// <summary>Numeric identifier used to select the target operation.</summary>
	public int OperationId { get; } = operationId;
}
