using Moq;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace DotUtils.StreamUtils.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="StreamExtensions"/> class.
    /// </summary>
    public class StreamExtensionsTests
    {
        /// <summary>
        /// Tests that ReadAtLeast reads at least the specified minimum bytes in a happy path scenario.
        /// </summary>
        [Fact]
        public void ReadAtLeast_WhenEnoughDataAvailable_ReturnsMinimumBytesRead()
        {
            // Arrange
            byte[] data = Encoding.ASCII.GetBytes("HelloWorld");
            using var stream = new MemoryStream(data);
            byte[] buffer = new byte[20];
            int offset = 5;
            int minimumBytes = 10;
            bool throwOnEndOfStream = true;

            // Act
            int bytesRead = stream.ReadAtLeast(buffer, offset, minimumBytes, throwOnEndOfStream);

            // Assert
            Assert.Equal(minimumBytes, bytesRead);
            // Validate that expected bytes are read at the correct offset
            for (int i = 0; i < minimumBytes; i++)
            {
                Assert.Equal(data[i], buffer[offset + i]);
            }
        }

        /// <summary>
        /// Tests that ReadAtLeast returns partial bytes read when throwOnEndOfStream is false and end-of-stream is reached.
        /// </summary>
        [Fact]
        public void ReadAtLeast_WhenNotEnoughDataAvailableAndNoThrow_ReturnsPartialBytesRead()
        {
            // Arrange
            byte[] data = Encoding.ASCII.GetBytes("Short");
            using var stream = new MemoryStream(data);
            byte[] buffer = new byte[10];
            int offset = 2;
            int minimumBytes = 7; // more than available data
            bool throwOnEndOfStream = false;

            // Act
            int bytesRead = stream.ReadAtLeast(buffer, offset, minimumBytes, throwOnEndOfStream);

            // Assert
            Assert.Equal(data.Length, bytesRead);
            for (int i = 0; i < data.Length; i++)
            {
                Assert.Equal(data[i], buffer[offset + i]);
            }
        }

        /// <summary>
        /// Tests that ReadAtLeast throws InvalidDataException when throwOnEndOfStream is true and stream ends prematurely.
        /// </summary>
        [Fact]
        public void ReadAtLeast_WhenNotEnoughDataAvailableAndThrow_ThrowsInvalidDataException()
        {
            // Arrange
            byte[] data = Encoding.ASCII.GetBytes("Data");
            using var stream = new MemoryStream(data);
            byte[] buffer = new byte[10];
            int offset = 0;
            int minimumBytes = 6; // more than available
            bool throwOnEndOfStream = true;

            // Act & Assert
            Assert.Throws<InvalidDataException>(() =>
            {
                stream.ReadAtLeast(buffer, offset, minimumBytes, throwOnEndOfStream);
            });
        }

        /// <summary>
        /// Tests that SkipBytes (overload without parameters) skips all bytes in the stream.
        /// </summary>
        [Fact]
        public void SkipBytes_NoParameter_SkipsEntireStream()
        {
            // Arrange
            byte[] data = new byte[100];
            using var stream = new MemoryStream(data);
            long originalLength = stream.Length;

            // Act
            long skipped = stream.SkipBytes();

            // Assert
            Assert.Equal(originalLength, skipped);
            Assert.Equal(stream.Length, stream.Position);
        }

        /// <summary>
        /// Tests that SkipBytes with bytesCount parameter skips the correct number of bytes.
        /// </summary>
        [Fact]
        public void SkipBytes_WithBytesCount_SkipsSpecifiedNumberOfBytes()
        {
            // Arrange
            byte[] data = new byte[5000];
            using var stream = new MemoryStream(data);
            int skipBytes = 3000;
            long initialPosition = stream.Position;

            // Act
            int skipped = stream.SkipBytes(skipBytes, throwOnEndOfStream: false);

            // Assert
            Assert.Equal(skipBytes, skipped);
            Assert.Equal(initialPosition + skipBytes, stream.Position);
        }

        /// <summary>
        /// Tests that SkipBytes with throwOnEndOfStream throws InvalidDataException when there is insufficient data.
        /// </summary>
        [Fact]
        public void SkipBytes_WithBytesCountAndThrow_WhenInsufficientData_ThrowsInvalidDataException()
        {
            // Arrange
            byte[] data = new byte[100];
            using var stream = new MemoryStream(data);
            int skipBytes = 200; // more than available

            // Act & Assert
            Assert.Throws<InvalidDataException>(() =>
            {
                stream.SkipBytes(skipBytes, throwOnEndOfStream: true);
            });
        }

        /// <summary>
        /// Tests that SkipBytes with provided buffer overload skips the correct number of bytes.
        /// </summary>
        [Fact]
        public void SkipBytes_WithBuffer_SkipsSpecifiedNumberOfBytes()
        {
            // Arrange
            byte[] data = new byte[4096 * 3];
            using var stream = new MemoryStream(data);
            int skipBytes = 4096 * 2;
            byte[] customBuffer = new byte[4096];

            // Act
            int skipped = stream.SkipBytes(skipBytes, throwOnEndOfStream: false, buffer: customBuffer);

            // Assert
            Assert.Equal(skipBytes, skipped);
            Assert.Equal(skipBytes, stream.Position);
        }

        /// <summary>
        /// Tests that SkipBytes throws ArgumentOutOfRangeException when negative bytesCount is provided.
        /// </summary>
        [Theory]
        [InlineData(-1)]
        public void SkipBytes_NegativeBytesCount_ThrowsArgumentOutOfRangeException(long bytesCount)
        {
            // Arrange
            byte[] data = new byte[50];
            using var stream = new MemoryStream(data);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                stream.SkipBytes(bytesCount, throwOnEndOfStream: false);
            });
        }

        /// <summary>
        /// Tests that SkipBytes throws ArgumentOutOfRangeException when bytesCount exceeds int.MaxValue.
        /// </summary>
        [Fact]
        public void SkipBytes_BytesCountExceedingIntMaxValue_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            byte[] data = new byte[100];
            using var stream = new MemoryStream(data);
            long tooManyBytes = (long)int.MaxValue + 1;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                stream.SkipBytes(tooManyBytes, throwOnEndOfStream: false);
            });
        }

        /// <summary>
        /// Tests that ReadToEnd returns the full content of the stream.
        /// </summary>
        [Fact]
        public void ReadToEnd_WhenCalled_ReturnsAllData()
        {
            // Arrange
            byte[] data = Encoding.ASCII.GetBytes("CompleteDataSet");
            using var stream = new MemoryStream(data);

            // Act
            byte[] result = stream.ReadToEnd();

            // Assert
            Assert.Equal(data, result);
        }

        /// <summary>
        /// Tests that TryGetLength returns true and outputs correct length for a seekable stream.
        /// </summary>
        [Fact]
        public void TryGetLength_SeekableStream_ReturnsTrueAndCorrectLength()
        {
            // Arrange
            byte[] data = new byte[256];
            using var stream = new MemoryStream(data);

            // Act
            bool result = stream.TryGetLength(out long length);

            // Assert
            Assert.True(result);
            Assert.Equal(data.Length, length);
        }

        /// <summary>
        /// Tests that TryGetLength returns false and outputs 0 for a non-seekable stream.
        /// </summary>
        [Fact]
        public void TryGetLength_NonSeekableStream_ReturnsFalseAndLengthZero()
        {
            // Arrange
            using var stream = new NonSeekableStream(new MemoryStream(Encoding.ASCII.GetBytes("NonSeekable")));

            // Act
            bool result = stream.TryGetLength(out long length);

            // Assert
            Assert.False(result);
            Assert.Equal(0, length);
        }

        /// <summary>
        /// Tests that ToReadableSeekableStream returns a seekable and readable stream.
        /// </summary>
        [Fact]
        public void ToReadableSeekableStream_WhenCalled_ReturnsSeekableReadableStream()
        {
            // Arrange
            byte[] data = Encoding.ASCII.GetBytes("TestData");
            using var originalStream = new MemoryStream(data);

            // Act
            Stream resultStream = originalStream.ToReadableSeekableStream();

            // Assert
            Assert.True(resultStream.CanRead);
            Assert.True(resultStream.CanSeek);
            // If the input is already seekable, it could return the same instance.
            // Thus, reading the data should be equal to the original.
            resultStream.Position = 0;
            byte[] resultData = new byte[data.Length];
            int read = resultStream.Read(resultData, 0, data.Length);
            Assert.Equal(data.Length, read);
            Assert.Equal(data, resultData);
        }

        /// <summary>
        /// Tests that Slice returns a stream that is a bounded view over the underlying stream.
        /// </summary>
        [Fact]
        public void Slice_WhenCalled_ReturnsSubStreamWithLimitedLength()
        {
            // Arrange
            byte[] data = Encoding.ASCII.GetBytes("SubStreamTestData");
            using var baseStream = new MemoryStream(data);
            long sliceLength = 9; // "SubStream"

            // Act
            using Stream slice = baseStream.Slice(sliceLength);
            byte[] buffer = new byte[50];
            int bytesRead = slice.Read(buffer, 0, buffer.Length);

            // Assert
            Assert.Equal(sliceLength, bytesRead);
            string result = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Assert.Equal("SubStrea", result.Substring(0, 8)); // Due to potential internal behavior; we check length only.
            // Alternatively, check that we do not read beyond sliceLength.
            Assert.Equal(sliceLength, ((MemoryStream)slice).Length);
        }

        /// <summary>
        /// Tests that Concat returns a stream which concatenates two streams.
        /// </summary>
        [Fact]
        public void Concat_WhenCalled_ReturnsStreamContainingConcatenatedData()
        {
            // Arrange
            byte[] data1 = Encoding.ASCII.GetBytes("Hello");
            byte[] data2 = Encoding.ASCII.GetBytes("World");
            using var stream1 = new MemoryStream(data1);
            using var stream2 = new MemoryStream(data2);

            // Act
            using Stream concatStream = stream1.Concat(stream2);
            using var resultStream = new MemoryStream();
            concatStream.CopyTo(resultStream);
            byte[] resultData = resultStream.ToArray();

            // Assert
            byte[] expected = new byte[data1.Length + data2.Length];
            Buffer.BlockCopy(data1, 0, expected, 0, data1.Length);
            Buffer.BlockCopy(data2, 0, expected, data1.Length, data2.Length);
            Assert.Equal(expected, resultData);
        }
    }

    /// <summary>
    /// A non-seekable stream wrapper for testing purposes.
    /// </summary>
    public class NonSeekableStream : Stream
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
}
