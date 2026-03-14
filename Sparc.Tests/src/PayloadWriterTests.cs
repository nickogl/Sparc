
using Sparc.IO;

namespace Sparc.Tests;

public class PayloadWriterTests
{
	[Fact]
	public void GetSpanAndAdvance_WhenWritingMultipleSegments_PreservesOrder()
	{
		var writer = new PayloadWriter();

		var first = writer.GetSpan(2);
		first[0] = 1;
		first[1] = 2;
		writer.Advance(2);

		var second = writer.GetSpan(3);
		second[0] = 3;
		second[1] = 4;
		second[2] = 5;
		writer.Advance(3);

		Assert.Equal(5, writer.WrittenSpan.Length);
		Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, writer.WrittenSpan.ToArray());
	}

	[Fact]
	public void Advance_WhenCountIsZero_DoesNotChangeWritten()
	{
		var writer = new PayloadWriter();

		writer.Advance(0);

		Assert.Equal(0, writer.WrittenSpan.Length);
	}

	[Fact]
	public void Written_WhenMultipleSegmentsAdvanced_EqualsTotalAdvancedBytes()
	{
		var writer = new PayloadWriter();

		writer.GetSpan(4);
		writer.Advance(4);

		writer.GetSpan(6);
		writer.Advance(6);

		Assert.Equal(10, writer.WrittenSpan.Length);
	}

	[Fact]
	public void WrittenSpan_WhenReservedBufferExceedsAdvancedBytes_ReturnsOnlyWrittenBytes()
	{
		var writer = new PayloadWriter(initialBufferSize: 128);

		var span = writer.GetSpan(10);
		span[0] = 42;
		span[1] = 43;
		writer.Advance(2);

		Assert.Equal(2, writer.WrittenSpan.Length);
		Assert.Equal(new byte[] { 42, 43 }, writer.WrittenSpan.ToArray());
	}
}
