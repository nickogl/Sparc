namespace Sparc;

/// <summary>
/// Creates typed client proxies that write operation payloads and send them via the provided connection.
/// </summary>
/// <typeparam name="TClient">The consumer-defined client contract interface.</typeparam>
public interface IClientProxyFactory<TClient> where TClient : class
{
	/// <summary>
	/// Creates a proxy implementing <typeparamref name="TClient"/>.
	/// </summary>
	TClient Create();
}
