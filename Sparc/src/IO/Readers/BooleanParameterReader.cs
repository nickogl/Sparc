using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Sparc.IO.Readers;

internal sealed class BooleanParameterReader : IParameterReader<bool>
{
	public bool Read(ref PayloadReader reader)
	{
		var span = reader.Read(1);
		// NOTE: .NET interally represents false as 0 and true as everything else
		// By interpreting the byte directly as a bool we are avoiding a branch.
		return Unsafe.As<byte, bool>(ref MemoryMarshal.GetReference(span));
	}
}
