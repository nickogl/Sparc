namespace Sparc.IO.Writers;

internal sealed class TimeSpanParameterWriter(IParameterWriter<long> i64Writer) : IParameterWriter<TimeSpan>
{
	private readonly IParameterWriter<long> _i64Writer = i64Writer;

	public void Write(ref PayloadWriter writer, TimeSpan value)
	{
		long milliseconds = RoundToNearestMillisecond(value.Ticks);
		_i64Writer.Write(ref writer, milliseconds);
	}

	private static long RoundToNearestMillisecond(long ticks)
	{
		// Midpoints are rounded away from zero to avoid systematic truncation bias.
		long adjusted = ticks >= 0
			? ticks + (TimeSpan.TicksPerMillisecond / 2)
			: ticks - (TimeSpan.TicksPerMillisecond / 2);
		return adjusted / TimeSpan.TicksPerMillisecond;
	}
}
