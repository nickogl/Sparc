using System.Runtime.CompilerServices;

namespace Sparc.Benchmarks;

internal sealed class SyncBenchmarkConnection : IClientConnection
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	public ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
	{
		return ValueTask.CompletedTask;
	}
}
