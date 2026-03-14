using Sparc.Exceptions;
using Sparc.IO;

namespace Sparc.Tests;

public class PayloadReaderTests
{
	[Fact]
	public void Read_WhenLengthFits_ReturnsRequestedSliceAndAdvancesConsumed()
	{
		var buffer = new byte[] { 1, 2, 3, 4, 5 };
		var reader = new PayloadReader(buffer);

		var slice = reader.Read(3);

		Assert.Equal(3, reader.Consumed);
		Assert.False(reader.End);
		Assert.Equal(new byte[] { 1, 2, 3 }, slice.ToArray());
	}

	[Fact]
	public void Advance_WhenLengthFits_SkipsBytes()
	{
		var buffer = new byte[] { 10, 11, 12, 13, 14 };
		var reader = new PayloadReader(buffer);

		reader.Advance(2);
		var slice = reader.Read(2);

		Assert.Equal(4, reader.Consumed);
		Assert.Equal(new byte[] { 12, 13 }, slice.ToArray());
	}

	[Fact]
	public void End_WhenAllBytesConsumed_ReturnsTrue()
	{
		var buffer = new byte[] { 1, 2, 3 };
		var reader = new PayloadReader(buffer);

		reader.Read(3);

		Assert.True(reader.End);
		Assert.Equal(3, reader.Consumed);
	}

	[Fact]
	public void Read_WhenLengthIsZero_ReturnsEmptySpanAndDoesNotAdvance()
	{
		var buffer = new byte[] { 1, 2, 3 };
		var reader = new PayloadReader(buffer);

		var slice = reader.Read(0);

		Assert.Empty(slice.ToArray());
		Assert.Equal(0, reader.Consumed);
		Assert.False(reader.End);
	}

	[Fact]
	public void Read_WhenPayloadIsTruncated_ThrowsTruncatedPayloadException()
	{
		Assert.Throws<TruncatedPayloadException>(() =>
		{
			var buffer = new byte[] { 1, 2 };
			var reader = new PayloadReader(buffer);
			reader.Read(3);
		});
	}

	[Fact]
	public void Advance_WhenPayloadIsTruncated_ThrowsArgumentOutOfRangeException()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			var buffer = new byte[] { 1, 2 };
			var reader = new PayloadReader([1, 2]);
			reader.Advance(3);
		});
	}
}
