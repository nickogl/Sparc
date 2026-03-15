using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Sparc;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// Unfortunately we have to make these helpers public to be able to call them
// from our dynamic module. This is the most pragmatic approach without needing
// to either create another library or duplicate the metric for every client proxy.
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class OperationMetrics
{
	private readonly Histogram<int> _operationPayloadSize;

	public OperationMetrics(IMeterFactory meterFactory)
	{
		var meter = meterFactory.Create("Sparc");
		_operationPayloadSize = meter.CreateHistogram<int>(
			name: "rpc.operation.payload.size",
			unit: "By",
			description: "Serialized operation payload size in bytes, labelled by kind (inbound|outbound), contract, operation and operation_id");
	}

	public void RecordInboundPayloadSize(
		string contract,
		string operation,
		int operationId,
		int payloadSize)
	{
		RecordPayloadSize("inbound", contract, operation, operationId, payloadSize);
	}

	public void RecordOutboundPayloadSize(
		string contract,
		string operation,
		int operationId,
		int payloadSize)
	{
		RecordPayloadSize("outbound", contract, operation, operationId, payloadSize);
	}

	private void RecordPayloadSize(
		string kind,
		string contract,
		string operation,
		int operationId,
		int payloadSize)
	{
		var tags = new TagList()
		{
			{ "kind", kind },
			{ "contract", contract },
			{ "operation", operation },
			{ "operation_id", operationId },
		};
		_operationPayloadSize.Record(payloadSize, in tags);
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
