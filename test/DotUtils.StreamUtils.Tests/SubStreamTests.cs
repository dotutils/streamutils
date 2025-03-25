using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DotUtils.StreamUtils;

namespace DotUtils.StreamUtils.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="SubStream"/> class.
    /// </summary>
    public class SubStreamTests
    {
        /// <summary>
        /// A helper stream that simulates a non-readable stream.
        /// </summary>
        private class NonReadableStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotImplementedException();
            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
            public override void SetLength(long value) => throw new NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        }

        /// <summary>
        /// Tests that the constructor throws an InvalidOperationException when provided a non-readable stream.
        /// </summary>
        [Fact]
        public void Constructor_WithNonReadableStream_ThrowsInvalidOperationException()
        {
            // Arrange
            var nonReadable = new NonReadableStream();
            const long length = 10;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new SubStream(nonReadable, length));
        }

        /// <summary>
        /// Tests that the properties CanRead, CanSeek, CanWrite, and Length return expected values.
        /// </summary>
        [Fact]
        public void Properties_ReturnExpectedValues()
        {
            // Arrange
            byte[] data = new byte[10];
            using var memoryStream = new MemoryStream(data);
            const long subLength = 5;
            var subStream = new SubStream(memoryStream, subLength);

            // Act & Assert
            Assert.True(subStream.CanRead);
            Assert.False(subStream.CanSeek);
            Assert.False(subStream.CanWrite);
            Assert.Equal(subLength, subStream.Length);
        }

        /// <summary>
        /// Tests that setting the Position property throws a NotImplementedException.
        /// </summary>
        [Fact]
        public void PositionSetter_ThrowsNotImplementedException()
        {
            // Arrange
            byte[] data = new byte[10];
            using var memoryStream = new MemoryStream(data);
            var subStream = new SubStream(memoryStream, 5);

            // Act & Assert
            Assert.Throws<NotImplementedException>(() => subStream.Position = 1);
        }

        /// <summary>
        /// Tests that Flush calls the underlying stream's Flush method.
        /// </summary>
//         [Fact] [Error] (87-34)CS0246 The type or namespace name 'Mock<>' could not be found (are you missing a using directive or an assembly reference?) [Error] (97-47)CS0103 The name 'Times' does not exist in the current context
//         public void Flush_CallsUnderlyingFlush()
//         {
//             // Arrange
//             var mockStream = new Mock<Stream>();
//             mockStream.Setup(s => s.CanRead).Returns(true);
//             mockStream.Setup(s => s.Flush());
//             const long length = 10;
//             var subStream = new SubStream(mockStream.Object, length);
// 
//             // Act
//             subStream.Flush();
// 
//             // Assert
//             mockStream.Verify(s => s.Flush(), Times.Once);
//         }

        /// <summary>
        /// Tests that FlushAsync calls the underlying stream's FlushAsync method.
        /// </summary>
//         [Fact] [Error] (107-34)CS0246 The type or namespace name 'Mock<>' could not be found (are you missing a using directive or an assembly reference?) [Error] (117-81)CS0103 The name 'Times' does not exist in the current context
//         public async Task FlushAsync_CallsUnderlyingFlushAsync()
//         {
//             // Arrange
//             var mockStream = new Mock<Stream>();
//             mockStream.Setup(s => s.CanRead).Returns(true);
//             mockStream.Setup(s => s.FlushAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
//             const long length = 10;
//             var subStream = new SubStream(mockStream.Object, length);
// 
//             // Act
//             await subStream.FlushAsync(CancellationToken.None);
// 
//             // Assert
//             mockStream.Verify(s => s.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
//         }

        /// <summary>
        /// Tests that Read reads only up to the specified substream length and updates the internal position.
        /// </summary>
        [Fact]
        public void Read_ReadWithinSubstreamLength_ReturnsExpectedBytesAndUpdatesPosition()
        {
            // Arrange
            byte[] sourceData = { 1, 2, 3, 4, 5, 6, 7, 8 };
            using var memoryStream = new MemoryStream(sourceData);
            const int subLength = 5;
            var subStream = new SubStream(memoryStream, subLength);
            byte[] buffer = new byte[10];

            // Act
            int bytesRead = subStream.Read(buffer, 0, 3);

            // Assert
            Assert.Equal(3, bytesRead);
            Assert.Equal(1, buffer[0]);
            Assert.Equal(2, buffer[1]);
            Assert.Equal(3, buffer[2]);
            // Read again and check that remaining is correctly limited to subLength
            bytesRead = subStream.Read(buffer, 0, 10);
            // Only 2 bytes left in the substream view.
            Assert.Equal(2, bytesRead);
            Assert.Equal(4, buffer[0]);
            Assert.Equal(5, buffer[1]);
        }

        /// <summary>
        /// Tests that Read returns 0 when no more data is available in the substream.
        /// </summary>
        [Fact]
        public void Read_WhenAtEnd_ReturnsZero()
        {
            // Arrange
            byte[] sourceData = { 10, 20, 30 };
            using var memoryStream = new MemoryStream(sourceData);
            const int subLength = 3;
            var subStream = new SubStream(memoryStream, subLength);
            byte[] buffer = new byte[10];
            // Read all available bytes.
            int totalRead = subStream.Read(buffer, 0, 3);
            Assert.Equal(3, totalRead);

            // Act
            int extraRead = subStream.Read(buffer, 0, 10);

            // Assert
            Assert.Equal(0, extraRead);
            Assert.True(subStream.IsAtEnd);
        }

        /// <summary>
        /// Tests that ReadByte returns the expected byte and updates the position.
        /// </summary>
        [Fact]
        public void ReadByte_ReturnsExpectedByteAndUpdatesPosition()
        {
            // Arrange
            byte[] sourceData = { 100, 101 };
            using var memoryStream = new MemoryStream(sourceData);
            const int subLength = 2;
            var subStream = new SubStream(memoryStream, subLength);

            // Act & Assert
            int first = subStream.ReadByte();
            Assert.Equal(100, first);
            int second = subStream.ReadByte();
            Assert.Equal(101, second);
            int third = subStream.ReadByte();
            Assert.Equal(-1, third);
        }

        /// <summary>
        /// Tests that ReadAsync reads correctly up to the substream length.
        /// </summary>
        [Fact]
        public async Task ReadAsync_ReadWithinSubstreamLength_ReturnsExpectedBytesAndUpdatesPosition()
        {
            // Arrange
            byte[] sourceData = { 5, 6, 7, 8, 9 };
            using var memoryStream = new MemoryStream(sourceData);
            const int subLength = 5;
            var subStream = new SubStream(memoryStream, subLength);
            byte[] buffer = new byte[10];

            // Act
            int bytesRead = await subStream.ReadAsync(buffer, 0, 3, CancellationToken.None);

            // Assert
            Assert.Equal(3, bytesRead);
            Assert.Equal(5, buffer[0]);
            Assert.Equal(6, buffer[1]);
            Assert.Equal(7, buffer[2]);

            // Act - read remaining bytes (2 bytes left)
            bytesRead = await subStream.ReadAsync(buffer, 0, 10, CancellationToken.None);

            // Assert
            Assert.Equal(2, bytesRead);
            Assert.Equal(8, buffer[0]);
            Assert.Equal(9, buffer[1]);
        }

#if NET
        /// <summary>
        /// Tests that Read(Span<byte>) reads correctly up to the substream length.
        /// </summary>
        [Fact]
        public void Read_Span_ReadsExpectedBytesAndUpdatesPosition()
        {
            // Arrange
            byte[] sourceData = { 11, 12, 13, 14 };
            using var memoryStream = new MemoryStream(sourceData);
            const int subLength = 3;
            var subStream = new SubStream(memoryStream, subLength);
            Span<byte> buffer = new byte[5];

            // Act
            int bytesRead = subStream.Read(buffer);

            // Assert
            Assert.Equal(3, bytesRead);
            Assert.Equal(11, buffer[0]);
            Assert.Equal(12, buffer[1]);
            Assert.Equal(13, buffer[2]);
        }

        /// <summary>
        /// Tests that ReadAsync(Memory<byte>, CancellationToken) reads correctly up to the substream length.
        /// </summary>
        [Fact]
        public async Task ReadAsync_Memory_ReadsExpectedBytesAndUpdatesPosition()
        {
            // Arrange
            byte[] sourceData = { 21, 22, 23, 24, 25 };
            using var memoryStream = new MemoryStream(sourceData);
            const int subLength = 4;
            var subStream = new SubStream(memoryStream, subLength);
            Memory<byte> buffer = new byte[10];

            // Act
            int bytesRead = await subStream.ReadAsync(buffer, CancellationToken.None);

            // Assert
            Assert.Equal(4, bytesRead);
            Assert.Equal(21, buffer.Span[0]);
            Assert.Equal(22, buffer.Span[1]);
            Assert.Equal(23, buffer.Span[2]);
            Assert.Equal(24, buffer.Span[3]);
        }
#endif

        /// <summary>
        /// Tests that Seek throws a NotImplementedException.
        /// </summary>
        [Fact]
        public void Seek_ThrowsNotImplementedException()
        {
            // Arrange
            byte[] data = { 1, 2, 3 };
            using var memoryStream = new MemoryStream(data);
            var subStream = new SubStream(memoryStream, 3);

            // Act & Assert
            Assert.Throws<NotImplementedException>(() => subStream.Seek(1, SeekOrigin.Begin));
        }

        /// <summary>
        /// Tests that SetLength throws a NotImplementedException.
        /// </summary>
        [Fact]
        public void SetLength_ThrowsNotImplementedException()
        {
            // Arrange
            byte[] data = { 1, 2, 3 };
            using var memoryStream = new MemoryStream(data);
            var subStream = new SubStream(memoryStream, 3);

            // Act & Assert
            Assert.Throws<NotImplementedException>(() => subStream.SetLength(10));
        }

        /// <summary>
        /// Tests that Write throws a NotImplementedException.
        /// </summary>
        [Fact]
        public void Write_ThrowsNotImplementedException()
        {
            // Arrange
            byte[] data = { 1, 2, 3 };
            using var memoryStream = new MemoryStream(data);
            var subStream = new SubStream(memoryStream, 3);
            byte[] buffer = { 9, 9, 9 };

            // Act & Assert
            Assert.Throws<NotImplementedException>(() => subStream.Write(buffer, 0, buffer.Length));
        }

        /// <summary>
        /// Tests that IsAtEnd returns true only when the internal position is equal to or greater than substream length.
        /// </summary>
        [Fact]
        public void IsAtEnd_ReturnsCorrectValueAfterReads()
        {
            // Arrange
            byte[] sourceData = { 31, 32, 33, 34 };
            using var memoryStream = new MemoryStream(sourceData);
            const int subLength = 3;
            var subStream = new SubStream(memoryStream, subLength);
            byte[] buffer = new byte[10];

            // Act & Assert
            // Initially, not at end.
            Assert.False(subStream.IsAtEnd);

            int bytesRead = subStream.Read(buffer, 0, 2);
            Assert.Equal(2, bytesRead);
            Assert.False(subStream.IsAtEnd);

            // Read remaining one byte.
            bytesRead = subStream.Read(buffer, 0, 10);
            Assert.Equal(1, bytesRead);
            Assert.True(subStream.IsAtEnd);
        }
    }
}
