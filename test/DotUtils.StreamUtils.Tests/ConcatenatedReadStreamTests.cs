using DotUtils.StreamUtils;
using static DotUtils.StreamUtils.Tests.StreamTestExtensions;
using FluentAssertions;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace DotUtils.StreamUtils.Tests
{
    public class ConcatenatedReadStreamTests
    {
        [Fact]
        public void ReadByte_ReadsStreamSequentialy()
        {
            using MemoryStream ms1 = new(new byte[]{1, 2, 3});
            using MemoryStream ms2 = new(new byte[] { 4 });
            using MemoryStream ms3 = new(new byte[] { 5, 6 });

            Stream stream = new ConcatenatedReadStream(ms1, ms2, ms3);

            stream.ReadByte().Should().Be(1);
            stream.ReadByte().Should().Be(2);
            stream.ReadByte().Should().Be(3);
            stream.ReadByte().Should().Be(4);
            stream.ReadByte().Should().Be(5);
            stream.ReadByte().Should().Be(6);

            // cannot read anymore
            stream.ReadByte().Should().Be(-1);
        }

        [Theory]
        [MemberData(nameof(StreamTestExtensions.EnumerateReadFunctionTypes), MemberType = typeof(StreamTestExtensions))]
        public void Read_ReadsStreamSequentialy(StreamFunctionType streamFunctionType)
        {
            using MemoryStream ms1 = new(new byte[] { 1, 2, 3 });
            using MemoryStream ms2 = new(new byte[] { 4 });
            using MemoryStream ms3 = new(new byte[] { 5, 6 });
            using MemoryStream ms4 = new(new byte[] { 7, 8, 9 });

            Stream stream = new ConcatenatedReadStream(ms1, ms2, ms3, ms4);

            ReadBytes readBytes = stream.GetReadFunc(streamFunctionType);

            var readBuffer = new byte[2];

            readBytes(readBuffer).Should().Be(2);
            readBuffer.Should().BeEquivalentTo(new byte[] { 1, 2 });

            readBytes(readBuffer).Should().Be(2);
            readBuffer.Should().BeEquivalentTo(new byte[] { 3, 4 });

            readBytes(readBuffer).Should().Be(2);
            readBuffer.Should().BeEquivalentTo(new byte[] { 5, 6 });

            readBytes(readBuffer).Should().Be(2);
            readBuffer.Should().BeEquivalentTo(new byte[] { 7, 8 });

            // zero out for assertion clarity.
            Array.Clear(readBuffer);

            readBytes(readBuffer).Should().Be(1);
            readBuffer.Should().BeEquivalentTo(new byte[] { 9, 0 });

            // zero out for assertion clarity.
            Array.Clear(readBuffer);

            // cannot read anymore
            readBytes(readBuffer).Should().Be(0);
            readBuffer.Should().BeEquivalentTo(new byte[] { 0, 0 });
        }

        [Theory]
        [MemberData(nameof(StreamTestExtensions.EnumerateReadFunctionTypes), MemberType = typeof(StreamTestExtensions))]
        public void Read_ReadsStreamSequentialy_UsingMultipleSubstreams(StreamFunctionType streamFunctionType)
        {
            using MemoryStream ms1 = new(new byte[] { 1, 2, 3 });
            using MemoryStream ms2 = new(new byte[] { 4 });
            using MemoryStream ms3 = new(new byte[] { 5, 6 });
            using MemoryStream ms4 = new(new byte[] { 7, 8, 9, 10, 11 });

            Stream stream = new ConcatenatedReadStream(ms1, ms2, ms3, ms4);

            ReadBytes readBytes = stream.GetReadFunc(streamFunctionType);

            var readBuffer = new byte[5];

            readBytes(readBuffer).Should().Be(5);
            readBuffer.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5 });

            readBytes(readBuffer).Should().Be(5);
            readBuffer.Should().BeEquivalentTo(new byte[] { 6, 7, 8, 9, 10 });

            // zero out for assertion clarity.
            Array.Clear(readBuffer);

            readBytes(readBuffer).Should().Be(1);
            readBuffer.Should().BeEquivalentTo(new byte[] { 11, 0, 0, 0, 0 });

            // zero out for assertion clarity.
            Array.Clear(readBuffer);

            // cannot read anymore
            readBytes(readBuffer).Should().Be(0);
            readBuffer.Should().BeEquivalentTo(new byte[] { 0, 0, 0, 0, 0 });
        }

        /// <summary>
        /// Tests that the constructor throws an ArgumentException when a non-readable stream is provided.
        /// </summary>
        [Fact]
        public void Constructor_NonReadableStream_ThrowsArgumentException()
        {
            // Arrange
            var nonReadableMock = new Mock<Stream>();
            nonReadableMock.Setup(s => s.CanRead).Returns(false);

            // Act
            Action act = () => new ConcatenatedReadStream(nonReadableMock.Object);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("All streams must be readable*")
                .And.ParamName.Should().Be("streams");
        }

        /// <summary>
        /// Tests that Flush always throws NotSupportedException.
        /// </summary>
        [Fact]
        public void Flush_Always_ThrowsNotSupportedException()
        {
            // Arrange
            using MemoryStream ms = new MemoryStream(new byte[] { 1, 2, 3 });
            using var stream = new ConcatenatedReadStream(ms);

            // Act
            Action act = () => stream.Flush();

            // Assert
            act.Should().Throw<NotSupportedException>()
                .WithMessage("ConcatenatedReadStream is forward-only read-only");
        }

        /// <summary>
        /// Tests that Seek always throws NotSupportedException.
        /// </summary>
        [Fact]
        public void Seek_Always_ThrowsNotSupportedException()
        {
            // Arrange
            using MemoryStream ms = new MemoryStream(new byte[] { 1, 2, 3 });
            using var stream = new ConcatenatedReadStream(ms);

            // Act
            Action act = () => stream.Seek(0, SeekOrigin.Begin);

            // Assert
            act.Should().Throw<NotSupportedException>()
                .WithMessage("ConcatenatedReadStream is forward-only read-only");
        }

        /// <summary>
        /// Tests that SetLength always throws NotSupportedException.
        /// </summary>
        [Fact]
        public void SetLength_Always_ThrowsNotSupportedException()
        {
            // Arrange
            using MemoryStream ms = new MemoryStream(new byte[] { 1, 2, 3 });
            using var stream = new ConcatenatedReadStream(ms);

            // Act
            Action act = () => stream.SetLength(100);

            // Assert
            act.Should().Throw<NotSupportedException>()
                .WithMessage("ConcatenatedReadStream is forward-only read-only");
        }

        /// <summary>
        /// Tests that Write always throws NotSupportedException.
        /// </summary>
        [Fact]
        public void Write_Always_ThrowsNotSupportedException()
        {
            // Arrange
            byte[] buffer = new byte[10];
            using MemoryStream ms = new MemoryStream(new byte[] { 1, 2, 3 });
            using var stream = new ConcatenatedReadStream(ms);

            // Act
            Action act = () => stream.Write(buffer, 0, buffer.Length);

            // Assert
            act.Should().Throw<NotSupportedException>()
                .WithMessage("ConcatenatedReadStream is forward-only read-only");
        }

        /// <summary>
        /// Tests that setting the Position property throws NotSupportedException.
        /// </summary>
        [Fact]
        public void PositionSetter_ThrowsNotSupportedException()
        {
            // Arrange
            using MemoryStream ms = new MemoryStream(new byte[] { 1, 2, 3 });
            using var stream = new ConcatenatedReadStream(ms);

            // Act
            Action act = () => stream.Position = 10;

            // Assert
            act.Should().Throw<NotSupportedException>()
                .WithMessage("ConcatenatedReadStream is forward-only read-only");
        }

        /// <summary>
        /// Tests that Read method disposes an exhausted stream and continues reading from the next stream.
        /// </summary>
        [Fact]
        public void Read_WithExhaustedFirstStream_ContinuesReadingFromNextStream()
        {
            // Arrange
            byte[] secondStreamData = { 9, 8, 7 };
            var buffer = new byte[5];

            var firstStreamMock = new Mock<Stream>();
            firstStreamMock.Setup(s => s.CanRead).Returns(true);
            // Simulate an exhausted stream by returning 0 bytes read.
            firstStreamMock.Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Returns(0);
            firstStreamMock.Setup(s => s.Dispose());

            using MemoryStream secondStream = new MemoryStream(secondStreamData);
            var concatenatedStream = new ConcatenatedReadStream(firstStreamMock.Object, secondStream);

            // Act
            int bytesRead = concatenatedStream.Read(buffer, 0, buffer.Length);

            // Assert
            bytesRead.Should().Be(secondStreamData.Length);
            buffer.Take(secondStreamData.Length).Should().Equal(secondStreamData);
            firstStreamMock.Verify(s => s.Dispose(), Times.Once);
        }

        /// <summary>
        /// Tests that ReadByte method disposes an exhausted stream and continues reading from the next stream.
        /// </summary>
        [Fact]
        public void ReadByte_WithExhaustedFirstStream_ContinuesReadingFromNextStream()
        {
            // Arrange
            byte expectedValue = 42;
            var firstStreamMock = new Mock<Stream>();
            firstStreamMock.Setup(s => s.CanRead).Returns(true);
            // Simulate exhausted stream by returning -1.
            firstStreamMock.Setup(s => s.ReadByte()).Returns(-1);
            firstStreamMock.Setup(s => s.Dispose());

            using MemoryStream secondStream = new MemoryStream(new byte[] { expectedValue });
            var concatenatedStream = new ConcatenatedReadStream(firstStreamMock.Object, secondStream);

            // Act
            int result = concatenatedStream.ReadByte();

            // Assert
            result.Should().Be(expectedValue);
            firstStreamMock.Verify(s => s.Dispose(), Times.Once);
        }

        /// <summary>
        /// Tests that ReadAsync method disposes an exhausted stream and continues reading from the next stream.
        /// </summary>
        [Fact]
        public async Task ReadAsync_WithExhaustedFirstStream_ContinuesReadingFromNextStream()
        {
            // Arrange
            byte[] secondStreamData = { 5, 6, 7, 8 };
            var buffer = new byte[10];
            var cancellationToken = CancellationToken.None;

            var firstStreamMock = new Mock<Stream>();
            firstStreamMock.Setup(s => s.CanRead).Returns(true);
            // Simulate an exhausted stream asynchronously.
            firstStreamMock.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), cancellationToken))
                .ReturnsAsync(0);
            firstStreamMock.Setup(s => s.Dispose());

            using MemoryStream secondStream = new MemoryStream(secondStreamData);
            var concatenatedStream = new ConcatenatedReadStream(firstStreamMock.Object, secondStream);

            // Act
            int bytesRead = await concatenatedStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

            // Assert
            bytesRead.Should().Be(secondStreamData.Length);
            buffer.Take(secondStreamData.Length).Should().Equal(secondStreamData);
            firstStreamMock.Verify(s => s.Dispose(), Times.Once);
        }

#if NET
        /// <summary>
        /// Tests that Read(Span<byte>) disposes an exhausted stream and continues reading from the next stream.
        /// </summary>
        [Fact]
        public void ReadSpan_WithExhaustedFirstStream_ContinuesReadingFromNextStream()
        {
            // Arrange
            byte[] secondStreamData = { 11, 12 };
            Span<byte> buffer = new byte[5];

            var firstStreamMock = new Mock<Stream>();
            firstStreamMock.Setup(s => s.CanRead).Returns(true);
            firstStreamMock.Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Returns(0);
            firstStreamMock.Setup(s => s.Dispose());

            using MemoryStream secondStream = new MemoryStream(secondStreamData);
            var concatenatedStream = new ConcatenatedReadStream(firstStreamMock.Object, secondStream);

            // Act
            int bytesRead = concatenatedStream.Read(buffer);

            // Assert
            bytesRead.Should().Be(secondStreamData.Length);
            buffer.Slice(0, secondStreamData.Length).ToArray().Should().Equal(secondStreamData);
            firstStreamMock.Verify(s => s.Dispose(), Times.Once);
        }

        /// <summary>
        /// Tests that ReadAsync(Memory<byte>) disposes an exhausted stream and continues reading from the next stream.
        /// </summary>
        [Fact]
        public async Task ReadAsyncMemory_WithExhaustedFirstStream_ContinuesReadingFromNextStream()
        {
            // Arrange
            byte[] secondStreamData = { 21, 22, 23 };
            Memory<byte> memoryBuffer = new byte[10];
            var cancellationToken = CancellationToken.None;

            var firstStreamMock = new Mock<Stream>();
            firstStreamMock.Setup(s => s.CanRead).Returns(true);
            firstStreamMock.Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), cancellationToken))
                .ReturnsAsync(0);
            firstStreamMock.Setup(s => s.Dispose());

            using MemoryStream secondStream = new MemoryStream(secondStreamData);
            var concatenatedStream = new ConcatenatedReadStream(firstStreamMock.Object, secondStream);

            // Act
            int bytesRead = await concatenatedStream.ReadAsync(memoryBuffer, cancellationToken).ConfigureAwait(false);

            // Assert
            bytesRead.Should().Be(secondStreamData.Length);
            memoryBuffer.Slice(0, secondStreamData.Length).ToArray().Should().Equal(secondStreamData);
            firstStreamMock.Verify(s => s.Dispose(), Times.Once);
        }
#endif
    }
}
