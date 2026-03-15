using Sparc.IO;
using System.Buffers.Binary;
using System.Reflection;
using System.Reflection.Emit;

namespace Sparc;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// Unfortunately we have to make these helpers public to be able to call them
// from our dynamic module. This is the most pragmatic approach without needing
// to either create another library or generate async state machines ourselves.
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public static class ClientProxyFactoryHelpers
{
	internal readonly static Lock ModuleBuilderLock = new();
	internal readonly static ModuleBuilder ModuleBuilder =
		AssemblyBuilder
			.DefineDynamicAssembly(new("Sparc.ClientProxy"), AssemblyBuilderAccess.Run)
			.DefineDynamicModule("Sparc.ClientProxy");

	internal readonly static MethodInfo RecordOutboundPayloadSizeMethod =
		typeof(OperationMetrics).GetMethod(nameof(OperationMetrics.RecordOutboundPayloadSize))!;
	internal readonly static MethodInfo PayloadWriterGetWrittenMethod =
		typeof(PayloadWriter).GetProperty(nameof(PayloadWriter.Written))!.GetGetMethod()!;

	public static void WriteOperationId(ref PayloadWriter writer, int operationId)
	{
		var span = writer.GetSpan(sizeof(int));
		BinaryPrimitives.WriteInt32LittleEndian(span, operationId);
		writer.Advance(sizeof(int));
	}

	public static ValueTask WrappedSendAsync(IClientConnection connection, ref PayloadWriter writer, CancellationToken cancellationToken)
	{
		var pendingWrite = connection.SendAsync(writer.WrittenMemory, cancellationToken);
		return pendingWrite.IsCompletedSuccessfully
			? WrappedSendAsyncFast(ref writer)
			: WrappedSendAsyncSlow(pendingWrite, new(ref writer));
	}

	private static ValueTask WrappedSendAsyncFast(ref PayloadWriter writer)
	{
		writer.Dispose();
		return ValueTask.CompletedTask;
	}

	private static async ValueTask WrappedSendAsyncSlow(ValueTask pendingWrite, PayloadWriter.DisposeToken bufferDisposer)
	{
		try
		{
			await pendingWrite.ConfigureAwait(false);
		}
		finally
		{
			bufferDisposer.Dispose();
		}
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
