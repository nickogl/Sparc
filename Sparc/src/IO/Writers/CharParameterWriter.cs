using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sparc.IO.Writers;

internal sealed class CharParameterWriter : IParameterWriter<char>
{
	public void Write(ref PayloadWriter writer, char value)
	{
		if (!Rune.TryCreate(value, out var rune))
		{
			ThrowInvalidCharacter(value);
		}

		var target = writer.GetSpan(4);
		bool success = rune.TryEncodeToUtf8(target, out int written);
		Debug.Assert(success);

		writer.Advance(written);
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowInvalidCharacter(char value)
	{
		throw new ArgumentException($"Character U+{(int)value:X4} is an invalid UTF-16 surrogate");
	}
}
