using DotUtils.StreamUtils;
using static DotUtils.StreamUtils.Tests.StreamTestExtensions;
using FluentAssertions;
using Moq;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace DotUtils.StreamUtils.Tests
{
    public class TransparentReadStreamTests
    {
        [Fact]
        public void ReadByte_TracksPosition()
        {
            using MemoryStream ms1 = new(new byte[] { 1, 2, 3 });

            ms1.ReadByte().Should().Be(1);

            Stream stream = TransparentReadStream.EnsureTransparentReadStream(ms1);

            stream.Position.Should().Be(0);

            stream.ReadByte().Should().Be(2);
            stream.Position.Should().Be(1);

            stream.ReadByte().Should().Be(3);
            stream.Position.Should().Be(2);

            stream.ReadByte().Should().Be(-1);
            stream.Position.Should().Be(2);
        }

        [Theory]
        [MemberData(nameof(StreamTestExtensions.EnumerateReadFunctionTypes), MemberType = typeof(StreamTestExtensions))]
        public void Read_TracksPosition(StreamFunctionType streamFunctionType)
        {
            using MemoryStream ms1 = new(new byte[] { 1, 2, 3, 4 });

            ms1.ReadByte().Should().Be(1);

            Stream stream = TransparentReadStream.EnsureTransparentReadStream(ms1);

            ReadBytes readBytes = stream.GetReadFunc(streamFunctionType);

            stream.Position.Should().Be(0);

            var readBuffer = new byte[2];
            readBytes(readBuffer).Should().Be(2);
            readBuffer.Should().BeEquivalentTo(new byte[] { 2, 3 });
            stream.Position.Should().Be(2);

            Array.Clear(readBuffer);
            readBytes(readBuffer).Should().Be(1);
            readBuffer.Should().BeEquivalentTo(new byte[] { 4, 0 });
            stream.Position.Should().Be(3);

            Array.Clear(readBuffer);
            readBytes(readBuffer).Should().Be(0);
            readBuffer.Should().BeEquivalentTo(new byte[] { 0, 0 });
            stream.Position.Should().Be(3);
        }

        [Theory]
        [MemberData(nameof(StreamTestExtensions.EnumerateReadFunctionTypes), MemberType = typeof(StreamTestExtensions))]
        public void Seek_TracksPosition(StreamFunctionType streamFunctionType)
        {
            using MemoryStream ms1 = new(new byte[] { 1, 2, 3, 4, 5 });

            Stream stream = TransparentReadStream.EnsureTransparentReadStream(ms1);

            ReadBytes readBytes = stream.GetReadFunc(streamFunctionType);

            stream.Position.Should().Be(0);

            var readBuffer = new byte[2];
            readBytes(readBuffer).Should().Be(2);
            readBuffer.Should().BeEquivalentTo(new byte[] { 1, 2 });
            stream.Position.Should().Be(2);

            stream.Seek(2, SeekOrigin.Current).Should().Be(4);
            stream.Position.Should().Be(4);

            Array.Clear(readBuffer);
            readBytes(readBuffer).Should().Be(1);
            readBuffer.Should().BeEquivalentTo(new byte[] { 5, 0 });
            stream.Position.Should().Be(5);

            Array.Clear(readBuffer);
            readBytes(readBuffer).Should().Be(0);
            readBuffer.Should().BeEquivalentTo(new byte[] { 0, 0 });
            stream.Position.Should().Be(5);

            var act = () => stream.Seek(2, SeekOrigin.Current);
            act.Should().Throw<InvalidDataException>();
            stream.Position.Should().Be(5);
        }

        [Theory]
        [MemberData(nameof(StreamTestExtensions.EnumerateReadFunctionTypes), MemberType = typeof(StreamTestExtensions))]
        public void Read_ConstraintsBytesCountAllowedToRead(StreamFunctionType streamFunctionType)
        {
            using MemoryStream ms1 = new(new byte[] { 1, 2, 3, 4, 5 });

            ms1.ReadByte().Should().Be(1);

            TransparentReadStream stream = TransparentReadStream.EnsureTransparentReadStream(ms1);

            ReadBytes readBytes = stream.GetReadFunc(streamFunctionType);

            stream.Position.Should().Be(0);
            stream.BytesCountAllowedToRead = 3;
            stream.BytesCountAllowedToReadRemaining.Should().Be(3);


            var readBuffer = new byte[2];
            readBytes(readBuffer).Should().Be(2);
            readBuffer.Should().BeEquivalentTo(new byte[] { 2, 3 });
            stream.Position.Should().Be(2);
            stream.BytesCountAllowedToReadRemaining.Should().Be(1);

            Array.Clear(readBuffer);
            readBytes(readBuffer).Should().Be(1);
            readBuffer.Should().BeEquivalentTo(new byte[] { 4, 0 });
            stream.Position.Should().Be(3);
            stream.BytesCountAllowedToReadRemaining.Should().Be(0);

            stream.BytesCountAllowedToRead = 2;

            Array.Clear(readBuffer);
            readBytes(readBuffer).Should().Be(1);
            readBuffer.Should().BeEquivalentTo(new byte[] { 5, 0 });
            stream.Position.Should().Be(4);
            stream.BytesCountAllowedToReadRemaining.Should().Be(1);

            Array.Clear(readBuffer);
            readBytes(readBuffer).Should().Be(0);
            readBuffer.Should().BeEquivalentTo(new byte[] { 0, 0 });
            stream.Position.Should().Be(4);
            stream.BytesCountAllowedToReadRemaining.Should().Be(1);
        }
    #region EnsureSeekableStream tests

    /// <summary>
    /// Tests that when a seekable stream is passed to EnsureSeekableStream, the same stream is returned.
    /// </summary>
    [Fact]
    public void EnsureSeekableStream_WhenStreamIsSeekable_ReturnsSameInstance()
    {
        // Arrange
        using MemoryStream seekableStream = new MemoryStream(new byte[] { 1, 2, 3 });
        
        // Act
        Stream result = TransparentReadStream.EnsureSeekableStream(seekableStream);

        // Assert
        result.Should().BeSameAs(seekableStream);
    }

    /// <summary>
    /// Tests that when a non-readable stream is passed to EnsureSeekableStream, it throws an InvalidOperationException.
    /// </summary>
    [Fact]
    public void EnsureSeekableStream_WhenStreamIsNotReadable_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockStream = new Mock<Stream>();
        mockStream.SetupGet(s => s.CanSeek).Returns(false);
        mockStream.SetupGet(s => s.CanRead).Returns(false);

        // Act
        Action act = () => TransparentReadStream.EnsureSeekableStream(mockStream.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Stream must be readable.");
    }

    /// <summary>
    /// Tests that when a non-seekable but readable stream is passed to EnsureSeekableStream, a new TransparentReadStream is created.
    /// </summary>
    [Fact]
    public void EnsureSeekableStream_WhenStreamIsNonSeekableButReadable_ReturnsTransparentReadStream()
    {
        // Arrange
        var mockStream = new Mock<Stream>();
        mockStream.SetupGet(s => s.CanSeek).Returns(false);
        mockStream.SetupGet(s => s.CanRead).Returns(true);

        // Act
        Stream result = TransparentReadStream.EnsureSeekableStream(mockStream.Object);

        // Assert
        result.Should().NotBeSameAs(mockStream.Object);
        result.Should().BeOfType<TransparentReadStream>();
    }

    #endregion

    #region EnsureTransparentReadStream tests

    /// <summary>
    /// Tests that if the stream passed to EnsureTransparentReadStream is already a TransparentReadStream, the same instance is returned.
    /// </summary>
    [Fact]
    public void EnsureTransparentReadStream_WhenStreamIsAlreadyTransparent_ReturnsSameInstance()
    {
        // Arrange
        using MemoryStream ms = new MemoryStream(new byte[] { 1, 2, 3 });
        TransparentReadStream transparentStream = TransparentReadStream.EnsureTransparentReadStream(ms);

        // Act
        TransparentReadStream result = TransparentReadStream.EnsureTransparentReadStream(transparentStream);

        // Assert
        result.Should().BeSameAs(transparentStream);
    }

    /// <summary>
    /// Tests that if a non-readable stream is passed to EnsureTransparentReadStream, it throws an InvalidOperationException.
    /// </summary>
    [Fact]
    public void EnsureTransparentReadStream_WhenStreamIsNotReadable_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockStream = new Mock<Stream>();
        mockStream.SetupGet(s => s.CanRead).Returns(false);

        // Act
        Action act = () => TransparentReadStream.EnsureTransparentReadStream(mockStream.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Stream must be readable.");
    }

    /// <summary>
    /// Tests that when a non-transparent but readable stream is passed to EnsureTransparentReadStream, a new TransparentReadStream is created.
    /// </summary>
    [Fact]
    public void EnsureTransparentReadStream_WhenStreamIsNotTransparentButReadable_ReturnsNewInstance()
    {
        // Arrange
        using MemoryStream ms = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        TransparentReadStream result = TransparentReadStream.EnsureTransparentReadStream(ms);

        // Assert
        result.Should().BeOfType<TransparentReadStream>();
        result.Should().NotBeSameAs(ms);
    }

    #endregion

    #region Flush and FlushAsync tests

    /// <summary>
    /// Tests that calling Flush on the TransparentReadStream calls Flush on the underlying stream.
    /// </summary>
    [Fact]
    public void Flush_CallsUnderlyingStreamFlush()
    {
        // Arrange
        var mockStream = new Mock<Stream>();
        // Setup Flush to do nothing.
        mockStream.Setup(s => s.Flush());
        // Ensure the mock stream is readable.
        mockStream.SetupGet(s => s.CanRead).Returns(true);
        // Create TransparentReadStream instance.
        TransparentReadStream transparentStream = TransparentReadStream.EnsureTransparentReadStream(mockStream.Object);

        // Act
        transparentStream.Flush();

        // Assert
        mockStream.Verify(s => s.Flush(), Times.Once);
    }

    /// <summary>
    /// Tests that calling FlushAsync on the TransparentReadStream calls FlushAsync on the underlying stream.
    /// </summary>
    [Fact]
    public async Task FlushAsync_CallsUnderlyingStreamFlushAsync()
    {
        // Arrange
        var mockStream = new Mock<Stream>();
        var cancellationToken = new CancellationToken();
        mockStream.Setup(s => s.FlushAsync(cancellationToken)).Returns(Task.CompletedTask);
        mockStream.SetupGet(s => s.CanRead).Returns(true);
        TransparentReadStream transparentStream = TransparentReadStream.EnsureTransparentReadStream(mockStream.Object);

        // Act
        await transparentStream.FlushAsync(cancellationToken).ConfigureAwait(false);

        // Assert
        mockStream.Verify(s => s.FlushAsync(cancellationToken), Times.Once);
    }

    #endregion

    #region Read and ReadAsync tests

    /// <summary>
    /// Tests that Read method truncates the read count when the allowed bytes limit is reached.
    /// </summary>
    [Fact]
    public void Read_WhenCountExceedsAllowedBytes_TruncatesReadCount()
    {
        // Arrange
        byte[] sourceBytes = { 10, 20, 30, 40, 50 };
        using MemoryStream ms = new MemoryStream(sourceBytes);
        TransparentReadStream transparentStream = TransparentReadStream.EnsureTransparentReadStream(ms);
        // Advance position by reading one byte.
        int firstByte = transparentStream.ReadByte();
        firstByte.Should().Be(10);
        // Limit allowed bytes to 2 more bytes.
        transparentStream.BytesCountAllowedToRead = 2;
        byte[] buffer = new byte[4];

        // Act
        int bytesRead = transparentStream.Read(buffer, 0, 4);

        // Assert
        bytesRead.Should().Be(2);
        buffer[0].Should().Be(20);
        buffer[1].Should().Be(30);
        transparentStream.Position.Should().Be(3);
    }

    /// <summary>
    /// Tests that ReadByte returns -1 when no more allowed bytes are available.
    /// </summary>
    [Fact]
    public void ReadByte_WhenAllowedBytesExceeded_ReturnsMinusOne()
    {
        // Arrange
        byte[] sourceBytes = { 100 };
        using MemoryStream ms = new MemoryStream(sourceBytes);
        TransparentReadStream transparentStream = TransparentReadStream.EnsureTransparentReadStream(ms);
        // Set allowed bytes to 0 so that even one byte cannot be read.
        transparentStream.BytesCountAllowedToRead = 0;

        // Act
        int result = transparentStream.ReadByte();

        // Assert
        result.Should().Be(-1);
        transparentStream.Position.Should().Be(0);
    }

    /// <summary>
    /// Tests that ReadAsync method truncates the read count when the allowed bytes limit is reached.
    /// </summary>
    [Fact]
    public async Task ReadAsync_WhenCountExceedsAllowedBytes_TruncatesReadCount()
    {
        // Arrange
        byte[] sourceBytes = { 5, 15, 25, 35 };
        using MemoryStream ms = new MemoryStream(sourceBytes);
        TransparentReadStream transparentStream = TransparentReadStream.EnsureTransparentReadStream(ms);
        // Read one byte to advance position.
        int firstByte = await transparentStream.ReadAsync(new byte[1], 0, 1, CancellationToken.None).ConfigureAwait(false);
        firstByte.Should().Be(1);
        // Limit allowed bytes to 2 more bytes.
        transparentStream.BytesCountAllowedToRead = 2;
        byte[] buffer = new byte[3];

        // Act
        int bytesRead = await transparentStream.ReadAsync(buffer, 0, 3, CancellationToken.None).ConfigureAwait(false);

        // Assert
        bytesRead.Should().Be(2);
        buffer[0].Should().Be(15);
        buffer[1].Should().Be(25);
        transparentStream.Position.Should().Be(3);
    }

    #endregion

    #region #if NET Read(Span<byte>) and ReadAsync(Memory<byte>) tests

#if NET
    /// <summary>
    /// Tests that Read(Span<byte>) truncates the read count when the allowed bytes limit is reached.
    /// </summary>
    [Fact]
    public void ReadOverSpan_WhenBufferExceedsAllowedBytes_TruncatesReadCount()
    {
        // Arrange
        byte[] sourceBytes = { 1, 2, 3, 4, 5 };
        using MemoryStream ms = new MemoryStream(sourceBytes);
        TransparentReadStream transparentStream = TransparentReadStream.EnsureTransparentReadStream(ms);
        // Advance position by reading one byte.
        int firstByte = transparentStream.ReadByte();
        firstByte.Should().Be(1);
        // Limit allowed bytes to 2 more bytes.
        transparentStream.BytesCountAllowedToRead = 2;
        Span<byte> buffer = new byte[4];

        // Act
        int bytesRead = transparentStream.Read(buffer);

        // Assert
        bytesRead.Should().Be(2);
        buffer[0].Should().Be(2);
        buffer[1].Should().Be(3);
        transparentStream.Position.Should().Be(3);
    }

    /// <summary>
    /// Tests that ReadAsync(Memory<byte>) truncates the read count when the allowed bytes limit is reached.
    /// </summary>
    [Fact]
    public async Task ReadAsyncOverMemory_WhenBufferExceedsAllowedBytes_TruncatesReadCount()
    {
        // Arrange
        byte[] sourceBytes = { 9, 8, 7, 6 };
        using MemoryStream ms = new MemoryStream(sourceBytes);
        TransparentReadStream transparentStream = TransparentReadStream.EnsureTransparentReadStream(ms);
        // Read one byte to advance position.
        int firstByte = transparentStream.ReadByte();
        firstByte.Should().Be(9);
        // Limit allowed bytes to 2 more bytes.
        transparentStream.BytesCountAllowedToRead = 2;
        Memory<byte> buffer = new byte[3];

        // Act
        int bytesRead = await transparentStream.ReadAsync(buffer, CancellationToken.None).ConfigureAwait(false);

        // Assert
        bytesRead.Should().Be(2);
        buffer.Span[0].Should().Be(8);
        buffer.Span[1].Should().Be(7);
        transparentStream.Position.Should().Be(3);
    }
#endif

    #endregion

    #region Seek tests

    /// <summary>
    /// Tests that Seek throws NotSupportedException when called with an origin other than SeekOrigin.Current.
    /// </summary>
    [Fact]
    public void Seek_WhenOriginIsNotCurrent_ThrowsNotSupportedException()
    {
        // Arrange
        using MemoryStream ms = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        TransparentReadStream transparentStream = TransparentReadStream.EnsureTransparentReadStream(ms);

        // Act
        Action act = () => transparentStream.Seek(2, SeekOrigin.Begin);

        // Assert
        act.Should().Throw<NotSupportedException>()
           .WithMessage("Only seeking from SeekOrigin.Current is supported.");
    }

    #endregion

    #region SetLength and Write tests

    /// <summary>
    /// Tests that calling SetLength on TransparentReadStream throws NotSupportedException.
    /// </summary>
    [Fact]
    public void SetLength_Always_ThrowsNotSupportedException()
    {
        // Arrange
        using MemoryStream ms = new MemoryStream(new byte[] { 1, 2, 3 });
        TransparentReadStream transparentStream = TransparentReadStream.EnsureTransparentReadStream(ms);

        // Act
        Action act = () => transparentStream.SetLength(100);

        // Assert
        act.Should().Throw<NotSupportedException>()
           .WithMessage("Expanding stream is not supported.");
    }

    /// <summary>
    /// Tests that calling Write on TransparentReadStream throws NotSupportedException.
    /// </summary>
    [Fact]
    public void Write_Always_ThrowsNotSupportedException()
    {
        // Arrange
        using MemoryStream ms = new MemoryStream(new byte[] { 10, 20, 30 });
        TransparentReadStream transparentStream = TransparentReadStream.EnsureTransparentReadStream(ms);
        byte[] buffer = new byte[] { 1, 2, 3 };

        // Act
        Action act = () => transparentStream.Write(buffer, 0, buffer.Length);

        // Assert
        act.Should().Throw<NotSupportedException>()
           .WithMessage("Writing is not supported.");
    }

    #endregion

    #region Close test

    /// <summary>
    /// Tests that calling Close on TransparentReadStream closes the underlying stream.
    /// </summary>
    [Fact]
    public void Close_CallsUnderlyingStreamClose()
    {
        // Arrange
        var mockStream = new Mock<Stream>();
        // Setup Close to do nothing.
        mockStream.Setup(s => s.Close());
        mockStream.SetupGet(s => s.CanRead).Returns(true);
        TransparentReadStream transparentStream = TransparentReadStream.EnsureTransparentReadStream(mockStream.Object);

        // Act
        transparentStream.Close();

        // Assert
        mockStream.Verify(s => s.Close(), Times.Once);
    }

    #endregion
    }
}