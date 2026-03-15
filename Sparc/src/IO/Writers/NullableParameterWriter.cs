namespace Sparc.IO.Writers;

internal sealed class NullableParameterWriter<T>(IParameterWriter<T> valueWriter) : IParameterWriter<T?>
	where T : struct
{
	private readonly IParameterWriter<T> _valueWriter = valueWriter;

	public void Write(ref PayloadWriter writer, T? value)
	{
		var prefixTarget = writer.GetSpan(1);
		if (!value.HasValue)
		{
			prefixTarget[0] = 0;
			writer.Advance(1);
			return;
		}

		prefixTarget[0] = 1;
		writer.Advance(1);
		_valueWriter.Write(ref writer, value.Value);
	}
}
