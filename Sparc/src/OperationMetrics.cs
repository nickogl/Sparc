using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
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
	private const string MeterName = "Sparc";
	private const string PayloadSizeInstrumentName = "rpc.operation.payload.size";

	private readonly Histogram<int>? _payloadSize;

	public OperationMetrics(IMeterFactory meterFactory, IOptions<MetricsOptions> options)
	{
		var meter = meterFactory.Create(MeterName);
		var optionsValue = options.Value;

		// NOTE: Building a TagList and evaluating the rules in a hot path is expensive.
		// Library consumers should not pay for what they do not need, so if they disabled
		// the instrument, we cache this information and skip everything entirely.
		if (IsInstrumentEnabled(PayloadSizeInstrumentName, optionsValue))
		{
			_payloadSize = meter.CreateHistogram<int>(
				name: PayloadSizeInstrumentName,
				unit: "By",
				description: "Serialized operation payload size in bytes, labelled by kind (inbound|outbound), contract, operation and operation_id");
		}
	}

	public void RecordInboundPayloadSize(
		string contract,
		string operation,
		object operationId,
		int payloadSize)
	{
		if (_payloadSize is not null)
		{
			RecordPayloadSize(_payloadSize, "inbound", contract, operation, operationId, payloadSize);
		}
	}

	public void RecordOutboundPayloadSize(
		string contract,
		string operation,
		object operationId,
		int payloadSize)
	{
		if (_payloadSize is not null)
		{
			RecordPayloadSize(_payloadSize, "outbound", contract, operation, operationId, payloadSize);
		}
	}

	private static void RecordPayloadSize(
		Histogram<int> instrument,
		string kind,
		string contract,
		string operation,
		object operationId,
		int payloadSize)
	{
		Debug.Assert(operationId is int);

		var tags = new TagList()
		{
			{ "kind", kind },
			{ "contract", contract },
			{ "operation", operation },
			{ "operation_id", operationId },
		};
		instrument.Record(payloadSize, in tags);
	}

	private static bool IsInstrumentEnabled(string instrumentName, MetricsOptions options)
	{
		var result = AllEnabledByDefault(options);
		foreach (var rule in options.Rules)
		{
			if (rule.MeterName is not null)
			{
				if (rule.MeterName.Equals($"{MeterName}.*", StringComparison.Ordinal) && rule.InstrumentName is null)
				{
					result = rule.Enable;
				}
				else if (
					rule.MeterName.Equals(MeterName, StringComparison.Ordinal)
					&& (rule.InstrumentName is null || rule.InstrumentName.Equals(instrumentName, StringComparison.Ordinal)))
				{
					result = rule.Enable;
				}
			}
		}

		return result;
	}

	private static bool AllEnabledByDefault(MetricsOptions options)
	{
		foreach (var rule in options.Rules)
		{
			if (rule.MeterName is null)
			{
				return rule.Enable;
			}
		}

		return true;
	}
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
