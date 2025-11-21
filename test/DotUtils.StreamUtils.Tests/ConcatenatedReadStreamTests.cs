using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DotUtils.StreamUtils.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ConcatenatedReadStream"/> class.
    /// </summary>
    public class ConcatenatedReadStreamTests
    {
        private readonly Encoding _encoding = Encoding.UTF8;

        /// <summary>
        /// A helper stream that is non-readable.
        /// </summary>
        private class NonReadableStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanSeek => throw new NotImplementedException();
            public override bool CanWrite => throw new NotImplementedException();
            public override long Length => throw new NotImplementedException();
            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override void Flush() => throw new NotImplementedException();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
            public override void SetLength(long value) => throw new NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        }

        /// <summary>
        /// A helper MemoryStream that tracks whether it has been disposed.
        /// </summary>
        private class TestMemoryStream : MemoryStream
        {
            public bool IsDisposed { get; private set; } = false;
            public TestMemoryStream(byte[] buffer) : base(buffer) { }
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                IsDisposed = true;
            }
#if NET
            public override async ValueTask DisposeAsync()
            {
                await base.DisposeAsync();
                IsDisposed = true;
            }
#endif
        }

        /// <summary>
        /// Tests that the constructor throws an ArgumentException when provided a non-readable stream.
        /// </summary>
        [Fact]
        public void Constructor_WithNonReadableStream_ThrowsArgumentException()
        {
            // Arrange
            Stream nonReadable = new NonReadableStream();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ConcatenatedReadStream(nonReadable));
        }

        /// <summary>
        /// Tests that the constructor correctly flattens nested ConcatenatedReadStreams.
        /// </summary>
        [Fact]
        public void Constructor_WithNestedConcatenatedReadStream_MergesSubStreams()
        {
            // Arrange
            byte[] bytes1 = _encoding.GetBytes("Hello");
            byte[] bytes2 = _encoding.GetBytes("World");
            using var ms1 = new MemoryStream(bytes1);
            using var ms2 = new MemoryStream(bytes2);
            using var innerConcat = new ConcatenatedReadStream(ms1, ms2);

            // Act
            using var outerConcat = new ConcatenatedReadStream(innerConcat);

            byte[] buffer = new byte[20];
            int readCount = outerConcat.Read(buffer, 0, buffer.Length);
            string result = _encoding.GetString(buffer, 0, readCount);

            // Assert
            Assert.Equal("HelloWorld", result);
        }

        /// <summary>
        /// Tests that Flush always throws NotSupportedException.
        /// </summary>
        [Fact]
        public void Flush_Always_ThrowsNotSupportedException()
        {
            // Arrange
            using var ms = new MemoryStream(_encoding.GetBytes("data"));
            using var concatenatedReadStream = new ConcatenatedReadStream(ms);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => concatenatedReadStream.Flush());
        }

        /// <summary>
        /// Tests that Read reads data from multiple underlying streams and disposes each stream when exhausted.
        /// </summary>
        [Fact]
        public void Read_ReadsDataFromMultipleStreams()
        {
            // Arrange
            byte[] bytes1 = _encoding.GetBytes("Hello");
            byte[] bytes2 = _encoding.GetBytes("World");
            var testStream1 = new TestMemoryStream(bytes1);
            var testStream2 = new TestMemoryStream(bytes2);

            using var concatenatedReadStream = new ConcatenatedReadStream(testStream1, testStream2);
            byte[] buffer = new byte[20];

            // Act
            int totalRead = concatenatedReadStream.Read(buffer, 0, buffer.Length);
            string result = _encoding.GetString(buffer, 0, totalRead);

            // Assert
            Assert.Equal("HelloWorld", result);
            Assert.Equal(bytes1.Length + bytes2.Length, totalRead);
            Assert.True(testStream1.IsDisposed);
            Assert.True(testStream2.IsDisposed);
            Assert.Equal(totalRead, concatenatedReadStream.Position);
        }

        /// <summary>
        /// Tests that Read returns zero when no more data is available.
        /// </summary>
        [Fact]
        public void Read_WhenNoDataLeft_ReturnsZero()
        {
            // Arrange
            byte[] bytes = _encoding.GetBytes("Test");
            using var ms = new MemoryStream(bytes);
            using var concatenatedReadStream = new ConcatenatedReadStream(ms);
            byte[] buffer = new byte[10];
            // Read all data
            concatenatedReadStream.Read(buffer, 0, buffer.Length);

            // Act
            int bytesReadAfterExhaust = concatenatedReadStream.Read(buffer, 0, buffer.Length);

            // Assert
            Assert.Equal(0, bytesReadAfterExhaust);
        }

        /// <summary>
        /// Tests that ReadByte reads data one byte at a time from multiple streams.
        /// </summary>
        [Fact]
        public void ReadByte_ReadsDataFromMultipleStreams()
        {
            // Arrange
            byte[] bytes1 = _encoding.GetBytes("AB");
            byte[] bytes2 = _encoding.GetBytes("CD");
            var testStream1 = new TestMemoryStream(bytes1);
            var testStream2 = new TestMemoryStream(bytes2);

            using var concatenatedReadStream = new ConcatenatedReadStream(testStream1, testStream2);

            // Act
            var sb = new StringBuilder();
            int b;
            while ((b = concatenatedReadStream.ReadByte()) != -1)
            {
                sb.Append((char)b);
            }
            string result = sb.ToString();

            // Assert
            Assert.Equal("ABCD", result);
            Assert.True(testStream1.IsDisposed);
            Assert.True(testStream2.IsDisposed);
            Assert.Equal(4, concatenatedReadStream.Position);
        }

        /// <summary>
        /// Tests that ReadAsync reads data from multiple streams and updates the position accordingly.
        /// </summary>
        [Fact]
        public async Task ReadAsync_ReadsDataFromMultipleStreams()
        {
            // Arrange
            byte[] bytes1 = _encoding.GetBytes("Async");
            byte[] bytes2 = _encoding.GetBytes("Test");
            var testStream1 = new TestMemoryStream(bytes1);
            var testStream2 = new TestMemoryStream(bytes2);

            using var concatenatedReadStream = new ConcatenatedReadStream(testStream1, testStream2);
            byte[] buffer = new byte[20];

            // Act
            int totalRead = await concatenatedReadStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);
            string result = _encoding.GetString(buffer, 0, totalRead);

            // Assert
            Assert.Equal("AsyncTest", result);
            Assert.Equal(bytes1.Length + bytes2.Length, totalRead);
            Assert.True(testStream1.IsDisposed);
            Assert.True(testStream2.IsDisposed);
            Assert.Equal(totalRead, concatenatedReadStream.Position);
        }

#if NET
        /// <summary>
        /// Tests that the Read(Span<byte>) overload correctly reads data from multiple streams.
        /// </summary>
        [Fact]
        public void ReadSpan_ReadsDataFromMultipleStreams()
        {
            // Arrange
            byte[] bytes1 = _encoding.GetBytes("Span");
            byte[] bytes2 = _encoding.GetBytes("Test");
            var testStream1 = new TestMemoryStream(bytes1);
            var testStream2 = new TestMemoryStream(bytes2);

            using var concatenatedReadStream = new ConcatenatedReadStream(testStream1, testStream2);
            byte[] bufferArray = new byte[20];
            var buffer = new Span<byte>(bufferArray);

            // Act
            int totalRead = concatenatedReadStream.Read(buffer);
            string result = _encoding.GetString(bufferArray, 0, totalRead);

            // Assert
            Assert.Equal("SpanTest", result);
            Assert.Equal(bytes1.Length + bytes2.Length, totalRead);
            Assert.True(testStream1.IsDisposed);
            Assert.True(testStream2.IsDisposed);
            Assert.Equal(totalRead, concatenatedReadStream.Position);
        }

        /// <summary>
        /// Tests that the ReadAsync(Memory<byte>) overload correctly reads data from multiple streams.
        /// </summary>
        [Fact]
        public async Task ReadAsyncMemory_ReadsDataFromMultipleStreams()
        {
            // Arrange
            byte[] bytes1 = _encoding.GetBytes("Memory");
            byte[] bytes2 = _encoding.GetBytes("Async");
            var testStream1 = new TestMemoryStream(bytes1);
            var testStream2 = new TestMemoryStream(bytes2);

            using var concatenatedReadStream = new ConcatenatedReadStream(testStream1, testStream2);
            byte[] bufferArray = new byte[20];
            Memory<byte> buffer = new Memory<byte>(bufferArray);

            // Act
            int totalRead = await concatenatedReadStream.ReadAsync(buffer);
            string result = _encoding.GetString(bufferArray, 0, totalRead);

            // Assert
            Assert.Equal("MemoryAsync", result);
            Assert.Equal(bytes1.Length + bytes2.Length, totalRead);
            Assert.True(testStream1.IsDisposed);
            Assert.True(testStream2.IsDisposed);
            Assert.Equal(totalRead, concatenatedReadStream.Position);
        }
#endif

        /// <summary>
        /// Tests that Seek throws NotSupportedException.
        /// </summary>
        [Fact]
        public void Seek_Always_ThrowsNotSupportedException()
        {
            // Arrange
            using var ms = new MemoryStream(_encoding.GetBytes("data"));
            using var concatenatedReadStream = new ConcatenatedReadStream(ms);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => concatenatedReadStream.Seek(0, SeekOrigin.Begin));
        }

        /// <summary>
        /// Tests that SetLength throws NotSupportedException.
        /// </summary>
        [Fact]
        public void SetLength_Always_ThrowsNotSupportedException()
        {
            // Arrange
            using var ms = new MemoryStream(_encoding.GetBytes("data"));
            using var concatenatedReadStream = new ConcatenatedReadStream(ms);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => concatenatedReadStream.SetLength(100));
        }

        /// <summary>
        /// Tests that Write throws NotSupportedException.
        /// </summary>
        [Fact]
        public void Write_Always_ThrowsNotSupportedException()
        {
            // Arrange
            byte[] buffer = _encoding.GetBytes("data");
            using var ms = new MemoryStream(_encoding.GetBytes("data"));
            using var concatenatedReadStream = new ConcatenatedReadStream(ms);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => concatenatedReadStream.Write(buffer, 0, buffer.Length));
        }

        /// <summary>
        /// Tests that setting the Position property throws NotSupportedException.
        /// </summary>
        [Fact]
        public void SetPosition_ThrowsNotSupportedException()
        {
            // Arrange
            using var ms = new MemoryStream(_encoding.GetBytes("data"));
            using var concatenatedReadStream = new ConcatenatedReadStream(ms);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => concatenatedReadStream.Position = 10);
        }

        /// <summary>
        /// Tests that the property values for CanRead, CanSeek, CanWrite and Length are correct.
        /// </summary>
        [Fact]
        public void Properties_ReturnExpectedValues()
        {
            // Arrange
            byte[] bytes1 = _encoding.GetBytes("12345");
            byte[] bytes2 = _encoding.GetBytes("67890");
            using var ms1 = new MemoryStream(bytes1);
            using var ms2 = new MemoryStream(bytes2);

            using var concatenatedReadStream = new ConcatenatedReadStream(ms1, ms2);

            // Act
            bool canRead = concatenatedReadStream.CanRead;
            bool canSeek = concatenatedReadStream.CanSeek;
            bool canWrite = concatenatedReadStream.CanWrite;
            long expectedLength = ms1.Length + ms2.Length;
            long actualLength = concatenatedReadStream.Length;

            // Assert
            Assert.True(canRead);
            Assert.False(canSeek);
            Assert.False(canWrite);
            Assert.Equal(expectedLength, actualLength);
        }
    }
}
