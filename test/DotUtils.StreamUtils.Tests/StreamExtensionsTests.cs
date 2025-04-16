using DotUtils.StreamUtils;
using FluentAssertions;
using Moq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Xunit;
namespace DotUtils.StreamUtils.Tests;


public class StreamExtensionsTests
{
    [Fact]
    public void ReadAtLeast_ThrowsOnEndOfStream()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var buffer = new byte[10];

        Assert.Throws<InvalidDataException>(() => stream.ReadAtLeast(buffer, 0, 10, throwOnEndOfStream: true));
    }

    [Fact]
    public void ReadToEnd_OnSeekableStream()
    {
        using MemoryStream ms1 = new(new byte[] { 1, 2, 3, 4, 5 });

        ms1.ReadByte();
        ms1.ReadByte();

        ms1.ReadToEnd().Should().BeEquivalentTo(new[] { 3, 4, 5 });

        // cannot read anymore
        ms1.ReadByte().Should().Be(-1);
    }

    [Fact]
    public void ReadToEnd_OnNonseekableStream()
    {
        using MemoryStream ms1 = new();
        GZipStream zipStream = new GZipStream(ms1, CompressionMode.Compress);
        zipStream.Write(new byte[] { 1, 2, 3, 4, 5 });
        zipStream.Flush();

        ms1.Position = 0;
        using GZipStream unzipStream = new GZipStream(ms1, CompressionMode.Decompress);

        unzipStream.ReadByte();
        unzipStream.ReadByte();

        unzipStream.ReadToEnd().Should().BeEquivalentTo(new[] { 3, 4, 5 });

        // cannot read anymore
        unzipStream.ReadByte().Should().Be(-1);
    }
    #region SkipBytes Tests

    /// <summary>
    /// Tests that SkipBytes returns 0 when the number of bytes to skip is 0.
    /// </summary>
    [Fact]
    public void SkipBytes_WithZeroBytes_ReturnsZero()
    {
        // Arrange
        byte[] data = { 1, 2, 3, 4, 5 };
        using MemoryStream ms = new MemoryStream(data);
        long initialPosition = ms.Position;
        long bytesToSkip = 0;

        // Act
        long skipped = ms.SkipBytes(bytesToSkip);

        // Assert
        skipped.Should().Be(0, "skipping 0 bytes should return 0");
        ms.Position.Should().Be(initialPosition, "position should not change when skipping 0 bytes");
    }

    /// <summary>
    /// Tests that SkipBytes successfully skips the specified number of bytes when enough data is available.
    /// Using the overload with explicit byte count.
    /// </summary>
    [Fact]
    public void SkipBytes_WithValidByteCount_SkipsCorrectly()
    {
        // Arrange
        byte[] data = new byte[5000];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(i % 256);
        }
        using MemoryStream ms = new MemoryStream(data);
        int bytesToSkip = 3000;
        long initialPosition = ms.Position;

        // Act
        long skipped = ms.SkipBytes(bytesToSkip);

        // Assert
        skipped.Should().Be(bytesToSkip, "should skip the exact number of available bytes");
        ms.Position.Should().Be(initialPosition + skipped, "stream position should be advanced by the number of skipped bytes");
    }

    /// <summary>
    /// Tests that SkipBytes with throwOnEndOfStream true throws InvalidDataException if the stream ends before skipping the desired number of bytes.
    /// </summary>
    [Fact]
    public void SkipBytes_WithInsufficientDataAndThrowTrue_ThrowsInvalidDataException()
    {
        // Arrange
        byte[] data = { 10, 20, 30 };
        using MemoryStream ms = new MemoryStream(data);
        int bytesToSkip = 5;

        // Act
        Action act = () => ms.SkipBytes(bytesToSkip, true);

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("Unexpected end of stream.", "stream did not have enough data and throwOnEndOfStream is true");
    }

    /// <summary>
    /// Tests that SkipBytes with throwOnEndOfStream false returns the number of bytes actually read when the stream ends early.
    /// </summary>
    [Fact]
    public void SkipBytes_WithInsufficientDataAndThrowFalse_ReturnsPartialReadCount()
    {
        // Arrange
        byte[] data = { 10, 20, 30 };
        using MemoryStream ms = new MemoryStream(data);
        int bytesToSkip = 5;
        long expectedSkipped = data.Length; // since only 3 bytes available

        // Act
        long skipped = ms.SkipBytes(bytesToSkip, false);

        // Assert
        skipped.Should().Be(expectedSkipped, "should return the number of bytes actually read (which is less than requested)");
        ms.Position.Should().Be(expectedSkipped, "stream position moved to the end");
    }

    /// <summary>
    /// Tests that SkipBytes overload which accepts long bytesCount throws ArgumentOutOfRangeException when bytesCount is negative.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    public void SkipBytes_NegativeByteCount_ThrowsArgumentOutOfRangeException(long negativeBytes)
    {
        // Arrange
        byte[] data = { 1, 2, 3 };
        using MemoryStream ms = new MemoryStream(data);

        // Act
        Action act = () => ms.SkipBytes(negativeBytes, true);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*", "negative bytes count should throw an ArgumentOutOfRangeException");
    }

    /// <summary>
    /// Tests that SkipBytes overload which accepts long bytesCount throws ArgumentOutOfRangeException when bytesCount exceeds int.MaxValue.
    /// </summary>
    [Fact]
    public void SkipBytes_ExceedingIntMaxValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        byte[] data = new byte[10];
        using MemoryStream ms = new MemoryStream(data);
        long tooManyBytes = (long)int.MaxValue + 1;

        // Act
        Action act = () => ms.SkipBytes(tooManyBytes, true);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*", "bytesCount greater than int.MaxValue should throw an ArgumentOutOfRangeException");
    }

    #endregion

    #region TryGetLength Tests

    /// <summary>
    /// Tests that TryGetLength returns true and outputs the correct length for a seekable stream.
    /// </summary>
    [Fact]
    public void TryGetLength_OnSeekableStream_ReturnsTrueAndCorrectLength()
    {
        // Arrange
        byte[] data = { 1, 2, 3, 4 };
        using MemoryStream ms = new MemoryStream(data);

        // Act
        bool result = ms.TryGetLength(out long length);

        // Assert
        result.Should().BeTrue("MemoryStream supports seeking");
        length.Should().Be(ms.Length, "length should match MemoryStream length");
    }

    /// <summary>
    /// Tests that TryGetLength returns false and outputs 0 for a non-seekable stream.
    /// </summary>
    [Fact]
    public void TryGetLength_OnNonSeekableStream_ReturnsFalseAndZeroLength()
    {
        // Arrange
        byte[] data = { 5, 6, 7, 8 };
        using Stream nonSeekableStream = new NonSeekableStream(new MemoryStream(data));

        // Act
        bool result = nonSeekableStream.TryGetLength(out long length);

        // Assert
        result.Should().BeFalse("Non-seekable stream should return false");
        length.Should().Be(0, "length should be 0 for a non-seekable stream");
    }

    #endregion

    #region ToReadableSeekableStream Tests

    /// <summary>
    /// Tests that ToReadableSeekableStream returns a seekable stream.
    /// NOTE: This test is partial because TransparentReadStream.EnsureSeekableStream implementation is not provided.
    /// </summary>
    [Fact]
    public void ToReadableSeekableStream_ReturnsSeekableStream()
    {
        // Arrange
        byte[] data = { 9, 8, 7, 6 };
        using MemoryStream ms = new MemoryStream(data);

        // Act
        Stream readableStream = ms.ToReadableSeekableStream();

        // Assert
        readableStream.Should().NotBeNull("should return a valid stream");
        readableStream.CanSeek.Should().BeTrue("the returned stream should be seekable");
    }

    #endregion

    #region Slice Tests

    /// <summary>
    /// Tests that Slice returns a stream that provides a bounded view over the specified length from the underlying stream.
    /// </summary>
    [Fact]
    public void Slice_WithValidLength_ReturnsSubStreamWithBoundedData()
    {
        // Arrange
        byte[] data = new byte[20];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(i + 1);
        }
        using MemoryStream ms = new MemoryStream(data);
        long sliceLength = 10;

        // Act
        Stream slice = ms.Slice(sliceLength);
        byte[] result = new byte[sliceLength];
        int read = slice.Read(result, 0, (int)sliceLength);

        // Assert
        read.Should().Be((int)sliceLength, "should read exactly the slice length");
        for (int i = 0; i < sliceLength; i++)
        {
            result[i].Should().Be(data[i], $"the {i}th byte should match the original data");
        }
    }

    #endregion

    #region Concat Tests

    /// <summary>
    /// Tests that Concat returns a stream that concatenates the data from the two underlying streams.
    /// </summary>
    [Fact]
    public void Concat_WithTwoStreams_ReturnsStreamWithConcatenatedData()
    {
        // Arrange
        byte[] data1 = { 1, 2, 3 };
        byte[] data2 = { 4, 5, 6 };
        using MemoryStream ms1 = new MemoryStream(data1);
        using MemoryStream ms2 = new MemoryStream(data2);

        // Act
        Stream concatenated = ms1.Concat(ms2);
        byte[] result = new byte[6];
        int totalRead = concatenated.Read(result, 0, result.Length);

        // Assert
        totalRead.Should().Be(6, "should read all concatenated bytes");
        result.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5, 6 }, "the bytes should be the concatenation of the two streams");
    }

    #endregion

    #region ReadAtLeast Additional Edge Case Tests

    /// <summary>
    /// Tests that ReadAtLeast returns the total bytes read when the stream ends and throwOnEndOfStream is false.
    /// </summary>
    [Fact]
    public void ReadAtLeast_WithInsufficientDataAndThrowFalse_ReturnsTotalRead()
    {
        // Arrange
        byte[] buffer = new byte[10];
        // Create a stream that only returns 2 bytes then end-of-stream.
        using MemoryStream ms = new MemoryStream(new byte[] { 100, 101 });
        int offset = 0;
        int minimumBytes = 5;

        // Act
        int bytesRead = ms.ReadAtLeast(buffer, offset, minimumBytes, throwOnEndOfStream: false);

        // Assert
        bytesRead.Should().Be(2, "only 2 bytes are available in the stream");
    }

    /// <summary>
    /// Tests that ReadAtLeast throws InvalidDataException when the stream ends prematurely and throwOnEndOfStream is true.
    /// </summary>
    [Fact]
    public void ReadAtLeast_WithInsufficientDataAndThrowTrue_ThrowsInvalidDataException()
    {
        // Arrange
        byte[] buffer = new byte[10];
        using MemoryStream ms = new MemoryStream(new byte[] { 200, 201 });
        int offset = 0;
        int minimumBytes = 3;

        // Act
        Action act = () => ms.ReadAtLeast(buffer, offset, minimumBytes, throwOnEndOfStream: true);

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("Unexpected end of stream.", "the stream ended before the minimum required bytes could be read");
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// A helper non-seekable stream wrapper for testing purposes.
    /// </summary>
    private class NonSeekableStream : Stream
    {
        private readonly Stream _innerStream;

        public NonSeekableStream(Stream innerStream)
        {
            _innerStream = innerStream;
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _innerStream.Position; set => throw new NotSupportedException(); }
        public override void Flush() => _innerStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => _innerStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);
    }

    #endregion
}
