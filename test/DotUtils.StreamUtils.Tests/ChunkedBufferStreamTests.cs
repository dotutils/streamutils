using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotUtils.StreamUtils;
using Xunit;

namespace DotUtils.StreamUtils.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ChunkedBufferStream"/> class.
    /// </summary>
    public class ChunkedBufferStreamTests
    {
        private const string NotSupportedMessage = "GreedyBufferedStream is write-only, append-only";

        /// <summary>
        /// A test stream derived from MemoryStream to verify that Close is called.
        /// </summary>
        private class TestMemoryStream : MemoryStream
        {
            public bool IsClosed { get; private set; } = false;

            public override void Close()
            {
                IsClosed = true;
                base.Close();
            }
        }

        /// <summary>
        /// Tests that Flush writes any partial data in the buffer to the underlying stream.
        /// </summary>
        [Fact]
        public void Flush_WithPartialBuffer_WritesBufferedDataToUnderlyingStream()
        {
            // Arrange
            var underlyingStream = new MemoryStream();
            const int bufferSize = 4;
            using var chunkedStream = new ChunkedBufferStream(underlyingStream, bufferSize);
            byte[] data = { 1, 2 };
            chunkedStream.Write(data, 0, data.Length);

            // Act
            chunkedStream.Flush();

            // Assert
            byte[] result = underlyingStream.ToArray();
            Assert.Equal(data, result);
        }

        /// <summary>
        /// Tests that Write(byte[], int, int) flushes automatically when the buffer becomes full and retains leftover data.
        /// </summary>
        [Fact]
        public void Write_WithDataExceedingBuffer_FlushesAutomaticallyAndRetainsRemainingData()
        {
            // Arrange
            var underlyingStream = new MemoryStream();
            const int bufferSize = 4;
            using var chunkedStream = new ChunkedBufferStream(underlyingStream, bufferSize);
            byte[] input = { 10, 20, 30, 40, 50, 60 }; // 6 bytes total

            // Act
            chunkedStream.Write(input, 0, input.Length);
            // At this point, first 4 bytes should have been automatically flushed.
            // Manually flush to write the remaining 2 bytes.
            chunkedStream.Flush();

            // Assert
            byte[] result = underlyingStream.ToArray();
            byte[] expected = { 10, 20, 30, 40, 50, 60 };
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that WriteByte writes a byte and flushes automatically when the buffer becomes full.
        /// </summary>
        [Fact]
        public void WriteByte_WhenBufferBecomesFull_FlushesAutomatically()
        {
            // Arrange
            var underlyingStream = new MemoryStream();
            const int bufferSize = 3;
            using var chunkedStream = new ChunkedBufferStream(underlyingStream, bufferSize);
            byte[] input = { 1, 2, 3 };

            // Act
            // Write first two bytes; should not flush yet.
            chunkedStream.WriteByte(input[0]);
            chunkedStream.WriteByte(input[1]);
            Assert.Equal(0, underlyingStream.Length);

            // Write third byte; this should cause the buffer to flush.
            chunkedStream.WriteByte(input[2]);

            // Assert
            byte[] result = underlyingStream.ToArray();
            Assert.Equal(input, result);
        }

        /// <summary>
        /// Tests that WriteAsync writes data asynchronously and flushes automatically when the buffer becomes full.
        /// </summary>
        [Fact]
        public async Task WriteAsync_WithDataExceedingBuffer_FlushesAutomaticallyAndRetainsRemainingData()
        {
            // Arrange
            var underlyingStream = new MemoryStream();
            const int bufferSize = 4;
            using var chunkedStream = new ChunkedBufferStream(underlyingStream, bufferSize);
            byte[] input = { 5, 15, 25, 35, 45, 55 };

            // Act
            await chunkedStream.WriteAsync(input, 0, input.Length, CancellationToken.None);
            // Auto flush should have occurred once when buffer reached 4 and then remaining bytes were not flushed.
            chunkedStream.Flush();
            
            // Assert
            byte[] result = underlyingStream.ToArray();
            byte[] expected = { 5, 15, 25, 35, 45, 55 };
            Assert.Equal(expected, result);
        }

#if NET
        /// <summary>
        /// Tests that Write(ReadOnlySpan) writes data and flushes automatically when the buffer becomes full.
        /// </summary>
        [Fact]
        public void Write_ReadOnlySpan_WithDataExceedingBuffer_FlushesAutomaticallyAndRetainsRemainingData()
        {
            // Arrange
            var underlyingStream = new MemoryStream();
            const int bufferSize = 5;
            using var chunkedStream = new ChunkedBufferStream(underlyingStream, bufferSize);
            ReadOnlySpan<byte> input = new byte[] { 100, 110, 120, 130, 140, 150, 160 };

            // Act
            chunkedStream.Write(input);
            // After write, first 5 bytes should have flushed automatically.
            chunkedStream.Flush();

            // Assert
            byte[] result = underlyingStream.ToArray();
            byte[] expected = { 100, 110, 120, 130, 140, 150, 160 };
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that WriteAsync(ReadOnlyMemory) writes data asynchronously and flushes automatically when the buffer becomes full.
        /// </summary>
        [Fact]
        public async Task WriteAsync_ReadOnlyMemory_WithDataExceedingBuffer_FlushesAutomaticallyAndRetainsRemainingData()
        {
            // Arrange
            var underlyingStream = new MemoryStream();
            const int bufferSize = 3;
            using var chunkedStream = new ChunkedBufferStream(underlyingStream, bufferSize);
            ReadOnlyMemory<byte> input = new byte[] { 200, 210, 220, 230, 240 };

            // Act
            await chunkedStream.WriteAsync(input, CancellationToken.None);
            // Auto flush would have occurred when first 3 bytes were written.
            chunkedStream.Flush();

            // Assert
            byte[] result = underlyingStream.ToArray();
            byte[] expected = { 200, 210, 220, 230, 240 };
            Assert.Equal(expected, result);
        }
#endif

        /// <summary>
        /// Tests that Read method always throws NotSupportedException.
        /// </summary>
        [Fact]
        public void Read_AnyArguments_ThrowsNotSupportedException()
        {
            // Arrange
            var underlyingStream = new MemoryStream();
            using var chunkedStream = new ChunkedBufferStream(underlyingStream, 4);
            byte[] buffer = new byte[10];

            // Act & Assert
            var exception = Assert.Throws<NotSupportedException>(() => chunkedStream.Read(buffer, 0, buffer.Length));
            Assert.Equal(NotSupportedMessage, exception.Message);
        }

        /// <summary>
        /// Tests that Seek method always throws NotSupportedException.
        /// </summary>
        [Fact]
        public void Seek_AnyArguments_ThrowsNotSupportedException()
        {
            // Arrange
            var underlyingStream = new MemoryStream();
            using var chunkedStream = new ChunkedBufferStream(underlyingStream, 4);

            // Act & Assert
            var exception = Assert.Throws<NotSupportedException>(() => chunkedStream.Seek(0, SeekOrigin.Begin));
            Assert.Equal(NotSupportedMessage, exception.Message);
        }

        /// <summary>
        /// Tests that SetLength method always throws NotSupportedException.
        /// </summary>
        [Fact]
        public void SetLength_AnyArgument_ThrowsNotSupportedException()
        {
            // Arrange
            var underlyingStream = new MemoryStream();
            using var chunkedStream = new ChunkedBufferStream(underlyingStream, 4);

            // Act & Assert
            var exception = Assert.Throws<NotSupportedException>(() => chunkedStream.SetLength(100));
            Assert.Equal(NotSupportedMessage, exception.Message);
        }

        /// <summary>
        /// Tests that setting Position property throws NotSupportedException.
        /// </summary>
        [Fact]
        public void SetPosition_PropertySetter_ThrowsNotSupportedException()
        {
            // Arrange
            var underlyingStream = new MemoryStream();
            using var chunkedStream = new ChunkedBufferStream(underlyingStream, 4);

            // Act & Assert
            var exception = Assert.Throws<NotSupportedException>(() => chunkedStream.Position = 10);
            Assert.Equal(NotSupportedMessage, exception.Message);
        }

        /// <summary>
        /// Tests that the CanRead, CanSeek, and CanWrite properties return the expected values.
        /// </summary>
        [Fact]
        public void Properties_CanReadCanSeekAndCanWrite_ReturnExpectedValues()
        {
            // Arrange
            var underlyingStream = new MemoryStream();
            const int bufferSize = 4;
            using var chunkedStream = new ChunkedBufferStream(underlyingStream, bufferSize);

            // Act & Assert
            Assert.False(chunkedStream.CanRead);
            Assert.False(chunkedStream.CanSeek);
            Assert.True(chunkedStream.CanWrite);
        }

        /// <summary>
        /// Tests that Length property returns the sum of underlying stream length and buffered data count.
        /// </summary>
        [Fact]
        public void Length_Property_ReturnsUnderlyingLengthPlusBufferedDataCount()
        {
            // Arrange
            var underlyingStream = new MemoryStream();
            const int bufferSize = 4;
            using var chunkedStream = new ChunkedBufferStream(underlyingStream, bufferSize);
            // Write less than bufferSize so that data stays in the buffer.
            byte[] data = { 1, 2 };
            chunkedStream.Write(data, 0, data.Length);

            // Underlying stream length is still zero, buffered data count is 2.
            long expectedLength = underlyingStream.Length + 2;

            // Act
            long actualLength = chunkedStream.Length;

            // Assert
            Assert.Equal(expectedLength, actualLength);
        }

        /// <summary>
        /// Tests that Position getter returns the sum of underlying stream position and buffered data count.
        /// </summary>
        [Fact]
        public void GetPosition_Property_ReturnsUnderlyingPositionPlusBufferedDataCount()
        {
            // Arrange
            var underlyingStream = new MemoryStream();
            const int bufferSize = 4;
            using var chunkedStream = new ChunkedBufferStream(underlyingStream, bufferSize);
            // Write 3 bytes; these remain in the buffer until flush.
            byte[] data = { 10, 20, 30 };
            chunkedStream.Write(data, 0, data.Length);

            // Underlying stream position is 0, buffered data count is 3.
            long expectedPosition = underlyingStream.Position + 3;

            // Act
            long actualPosition = chunkedStream.Position;

            // Assert
            Assert.Equal(expectedPosition, actualPosition);
        }

        /// <summary>
        /// Tests that Close flushes any remaining buffer to underlying stream and closes the underlying stream.
        /// </summary>
        [Fact]
        public void Close_CallsFlushAndClosesUnderlyingStream()
        {
            // Arrange
            var underlyingStream = new TestMemoryStream();
            const int bufferSize = 4;
            var chunkedStream = new ChunkedBufferStream(underlyingStream, bufferSize);
            byte[] data = { 7, 8 };
            chunkedStream.Write(data, 0, data.Length);

            // Act
            chunkedStream.Close();

            // Assert
            byte[] result = underlyingStream.ToArray();
            Assert.Equal(data, result);
            Assert.True(underlyingStream.IsClosed);
        }
    }
}
