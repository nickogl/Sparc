using Sparc.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Sparc.IO.Readers;

internal sealed class TimeSpanParameterReader(IParameterReader<long> i64Reader) : IParameterReader<TimeSpan>
{
	private readonly IParameterReader<long> _i64Reader = i64Reader;

	public TimeSpan Read(ref PayloadReader reader)
	{
		long milliseconds = _i64Reader.Read(ref reader);
		long ticks = MillisecondsToTicks(milliseconds);
		return TimeSpan.FromTicks(ticks);
	}

	private static long MillisecondsToTicks(long milliseconds)
	{
		try
		{
			return checked(milliseconds * TimeSpan.TicksPerMillisecond);
		}
		catch (OverflowException)
		{
			ThrowLimitExceeded(milliseconds);
			throw;
		}
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowLimitExceeded(long milliseconds)
	{
		throw new MalformedPayloadException($"Invalid TimeSpan milliseconds value '{milliseconds}'");
	}
}
