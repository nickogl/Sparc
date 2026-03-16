using System.Threading.Tasks.Sources;

namespace Sparc.Benchmarks;

internal sealed class DeferredBenchmarkConnection : IClientConnection, IValueTaskSource, IThreadPoolWorkItem
{
	private ManualResetValueTaskSourceCore<bool> _source = new() { RunContinuationsAsynchronously = true };

	public ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
	{
		_source.Reset();
		ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
		return new ValueTask(this, _source.Version);
	}

	void IValueTaskSource.GetResult(short token)
	{
		_source.GetResult(token);
	}

	ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
	{
		return _source.GetStatus(token);
	}

	void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
	{
		_source.OnCompleted(continuation, state, token, flags);
	}

	void IThreadPoolWorkItem.Execute()
	{
		_source.SetResult(result: true);
	}
}
