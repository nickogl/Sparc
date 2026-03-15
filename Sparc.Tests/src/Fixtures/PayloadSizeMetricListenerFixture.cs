using System.Diagnostics.Metrics;

namespace Sparc.Tests;

internal sealed class PayloadSizeMetricListener : IDisposable
{
	private readonly MeterListener _listener = new();
	private readonly Lock _measurementsLock = new();
	private readonly List<Measurement> _measurements = [];

	public PayloadSizeMetricListener()
	{
		_listener.InstrumentPublished = (instrument, listener) =>
		{
			if (instrument.Meter.Name == "Sparc" && instrument.Name == "rpc.operation.payload.size")
			{
				listener.EnableMeasurementEvents(instrument);
			}
		};
		_listener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
		_listener.Start();
	}

	public IReadOnlyList<Measurement> Measurements
	{
		get
		{
			lock (_measurementsLock)
			{
				return [.. _measurements];
			}
		}
	}

	public void Dispose()
	{
		_listener.Dispose();
	}

	private void OnMeasurementRecorded(Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
	{
		if (instrument.Name != "rpc.operation.payload.size")
		{
			return;
		}

		var kind = ReadStringTag(tags, "kind");
		var contract = ReadStringTag(tags, "contract");
		var operation = ReadStringTag(tags, "operation");
		var operationId = ReadIntTag(tags, "operation_id");

		lock (_measurementsLock)
		{
			_measurements.Add(new(kind, contract, operation, operationId, measurement));
		}
	}

	private static string ReadStringTag(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key)
	{
		for (int i = 0; i < tags.Length; i++)
		{
			if (tags[i].Key == key && tags[i].Value is string value)
			{
				return value;
			}
		}

		throw new InvalidOperationException($"Missing tag '{key}'.");
	}

	private static int ReadIntTag(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key)
	{
		for (int i = 0; i < tags.Length; i++)
		{
			if (tags[i].Key == key && tags[i].Value is int value)
			{
				return value;
			}
		}

		throw new InvalidOperationException($"Missing tag '{key}'.");
	}

	internal readonly record struct Measurement(
		string Kind,
		string Contract,
		string Operation,
		int OperationId,
		int PayloadSize);
}
