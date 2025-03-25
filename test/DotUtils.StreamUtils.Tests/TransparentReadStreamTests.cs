using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotUtils.StreamUtils;
using Moq;
using Xunit;

namespace DotUtils.StreamUtils.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="TransparentReadStream"/> class.
    /// </summary>
    public class TransparentReadStreamTests
    {
        private readonly byte[] _testData;

        public TransparentReadStreamTests()
        {
            // Initialize test data.
            _testData = new byte[10];
            for (int i = 0; i < _testData.Length; i++)
            {
                _testData[i] = (byte)i;
            }
        }

        #region Helper Classes

        /// <summary>
        /// A simple stream that is not seekable.
        /// </summary>
        private class NonSeekableStream : Stream
        {
            private readonly MemoryStream _innerStream;

            public NonSeekableStream(byte[] buffer)
            {
                _innerStream = new MemoryStream(buffer);
            }

            public override bool CanRead => _innerStream.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => _innerStream.CanWrite;
            public override long Length => _innerStream.Length;
            public override long Position
            {
                get => _innerStream.Position;
                set => _innerStream.Position = value;
            }
            public override void Flush() => _innerStream.Flush();
            public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => _innerStream.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);
            public override int ReadByte() => _innerStream.ReadByte();
#if NET
            public override int Read(Span<byte> buffer) => _innerStream.Read(buffer);
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
                _innerStream.ReadAsync(buffer, cancellationToken);
#endif
        }

        /// <summary>
        /// A stream that is not readable.
        /// </summary>
        private class NonReadableStream : Stream
        {
            private readonly MemoryStream _innerStream;

            public NonReadableStream(byte[] buffer)
            {
                _innerStream = new MemoryStream(buffer);
            }

            public override bool CanRead => false;
            public override bool CanSeek => _innerStream.CanSeek;
            public override bool CanWrite => _innerStream.CanWrite;
            public override long Length => _innerStream.Length;
            public override long Position
            {
                get => _innerStream.Position;
                set => _innerStream.Position = value;
            }
            public override void Flush() => _innerStream.Flush();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
            public override void SetLength(long value) => _innerStream.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);
            public override int ReadByte() => throw new NotSupportedException();
        }

        #endregion

        #region EnsureSeekableStream Tests

        /// <summary>
        /// Tests that EnsureSeekableStream returns the same stream if the provided stream is already seekable.
        /// </summary>
        [Fact]
        public void EnsureSeekableStream_WithSeekableStream_ReturnsSameInstance()
        {
            // Arrange
            using var memoryStream = new MemoryStream(_testData);

            // Act
            Stream result = TransparentReadStream.EnsureSeekableStream(memoryStream);

            // Assert
            Assert.Same(memoryStream, result);
        }

        /// <summary>
        /// Tests that EnsureSeekableStream wraps a non-seekable but readable stream in a TransparentReadStream.
        /// </summary>
        [Fact]
        public void EnsureSeekableStream_WithNonSeekableStream_ReturnsTransparentReadStreamWrapper()
        {
            // Arrange
            using var nonSeekable = new NonSeekableStream(_testData);

            // Act
            Stream result = TransparentReadStream.EnsureSeekableStream(nonSeekable);

            // Assert
            Assert.IsType<TransparentReadStream>(result);
        }

        /// <summary>
        /// Tests that EnsureSeekableStream throws an InvalidOperationException when the stream is not readable.
        /// </summary>
        [Fact]
        public void EnsureSeekableStream_WithNonReadableStream_ThrowsInvalidOperationException()
        {
            // Arrange
            using var nonReadable = new NonReadableStream(_testData);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => TransparentReadStream.EnsureSeekableStream(nonReadable));
        }

        #endregion

        #region EnsureTransparentReadStream Tests

        /// <summary>
        /// Tests that EnsureTransparentReadStream returns the same instance if the provided stream is already a TransparentReadStream.
        /// </summary>
//         [Fact] [Error] (153-45)CS0122 'TransparentReadStream.TransparentReadStream(Stream)' is inaccessible due to its protection level
//         public void EnsureTransparentReadStream_WhenAlreadyTransparentReadStream_ReturnsSameInstance()
//         {
//             // Arrange
//             using var memoryStream = new MemoryStream(_testData);
//             TransparentReadStream trs = new TransparentReadStream(memoryStream);
// 
//             // Act
//             TransparentReadStream result = TransparentReadStream.EnsureTransparentReadStream(trs);
// 
//             // Assert
//             Assert.Same(trs, result);
//         }

        /// <summary>
        /// Tests that EnsureTransparentReadStream wraps a readable stream that is not a TransparentReadStream.
        /// </summary>
        [Fact]
        public void EnsureTransparentReadStream_WithReadableStream_ReturnsTransparentReadStreamWrapper()
        {
            // Arrange
            using var memoryStream = new MemoryStream(_testData);

            // Act
            TransparentReadStream result = TransparentReadStream.EnsureTransparentReadStream(memoryStream);

            // Assert
            Assert.IsType<TransparentReadStream>(result);
        }

        /// <summary>
        /// Tests that EnsureTransparentReadStream throws an InvalidOperationException when the stream is not readable.
        /// </summary>
        [Fact]
        public void EnsureTransparentReadStream_WithNonReadableStream_ThrowsInvalidOperationException()
        {
            // Arrange
            using var nonReadable = new NonReadableStream(_testData);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => TransparentReadStream.EnsureTransparentReadStream(nonReadable));
        }

        #endregion

        #region Read Methods Tests

        /// <summary>
        /// Tests that Read(byte[], int, int) only reads up to the allowed bytes limit.
        /// </summary>
        [Fact]
        public void Read_ReadsWithinAllowedBytes()
        {
            // Arrange
            using var memoryStream = new MemoryStream(_testData);
            var trs = TransparentReadStream.EnsureTransparentReadStream(memoryStream);
            // Limit allowed bytes to 5.
            trs.BytesCountAllowedToRead = 5;
            byte[] buffer = new byte[10];
            
            // Act
            int bytesRead = trs.Read(buffer, 0, buffer.Length);

            // Assert
            Assert.Equal(5, bytesRead);
            for (int i = 0; i < bytesRead; i++)
            {
                Assert.Equal(_testData[i], buffer[i]);
            }
            // Remaining allowed bytes should be 0.
            Assert.Equal(0, trs.BytesCountAllowedToReadRemaining);
        }

        /// <summary>
        /// Tests that ReadByte() reads a single byte if within the allowed bytes limit, and returns -1 when limit is reached.
        /// </summary>
        [Fact]
        public void ReadByte_ReadsWithinAllowedBytes()
        {
            // Arrange
            using var memoryStream = new MemoryStream(_testData);
            var trs = TransparentReadStream.EnsureTransparentReadStream(memoryStream);
            // Set allowed bytes to 3.
            trs.BytesCountAllowedToRead = 3;

            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                int value = trs.ReadByte();
                Assert.Equal(_testData[i], (byte)value);
            }
            // Further read should return -1.
            Assert.Equal(-1, trs.ReadByte());
        }

        /// <summary>
        /// Tests that ReadAsync(byte[], int, int) only reads up to the allowed bytes limit.
        /// </summary>
        [Fact]
        public async Task ReadAsync_ReadsWithinAllowedBytes()
        {
            // Arrange
            using var memoryStream = new MemoryStream(_testData);
            var trs = TransparentReadStream.EnsureTransparentReadStream(memoryStream);
            trs.BytesCountAllowedToRead = 4;
            byte[] buffer = new byte[10];

            // Act
            int bytesRead = await trs.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

            // Assert
            Assert.Equal(4, bytesRead);
            for (int i = 0; i < bytesRead; i++)
            {
                Assert.Equal(_testData[i], buffer[i]);
            }
            Assert.Equal(0, trs.BytesCountAllowedToReadRemaining);
        }

#if NET
        /// <summary>
        /// Tests that Read(Span<byte>) only reads up to the allowed bytes limit.
        /// </summary>
        [Fact]
        public void ReadSpan_ReadsWithinAllowedBytes()
        {
            // Arrange
            using var memoryStream = new MemoryStream(_testData);
            var trs = TransparentReadStream.EnsureTransparentReadStream(memoryStream);
            trs.BytesCountAllowedToRead = 6;
            Span<byte> buffer = new byte[10];

            // Act
            int bytesRead = trs.Read(buffer);

            // Assert
            Assert.Equal(6, bytesRead);
            for (int i = 0; i < bytesRead; i++)
            {
                Assert.Equal(_testData[i], buffer[i]);
            }
            Assert.Equal(0, trs.BytesCountAllowedToReadRemaining);
        }

        /// <summary>
        /// Tests that ReadAsync(Memory<byte>, CancellationToken) only reads up to the allowed bytes limit.
        /// </summary>
        [Fact]
        public async Task ReadAsyncMemory_ReadsWithinAllowedBytes()
        {
            // Arrange
            using var memoryStream = new MemoryStream(_testData);
            var trs = TransparentReadStream.EnsureTransparentReadStream(memoryStream);
            trs.BytesCountAllowedToRead = 7;
            Memory<byte> buffer = new byte[10];

            // Act
            int bytesRead = await trs.ReadAsync(buffer, CancellationToken.None);

            // Assert
            Assert.Equal(7, bytesRead);
            for (int i = 0; i < bytesRead; i++)
            {
                Assert.Equal(_testData[i], buffer.Span[i]);
            }
            Assert.Equal(0, trs.BytesCountAllowedToReadRemaining);
        }
#endif

        #endregion

        #region Flush Methods Tests

        /// <summary>
        /// Tests that Flush() calls the underlying stream's Flush() method.
        /// </summary>
//         [Fact] [Error] (331-27)CS0122 'TransparentReadStream.TransparentReadStream(Stream)' is inaccessible due to its protection level
//         public void Flush_CallsUnderlyingStreamFlush()
//         {
//             // Arrange
//             var mockStream = new Mock<Stream>();
//             mockStream.Setup(s => s.CanRead).Returns(true);
//             mockStream.Setup(s => s.Flush());
//             var trs = new TransparentReadStream(mockStream.Object);
// 
//             // Act
//             trs.Flush();
// 
//             // Assert
//             mockStream.Verify(s => s.Flush(), Times.Once);
//         }

        /// <summary>
        /// Tests that FlushAsync(CancellationToken) calls the underlying stream's FlushAsync(CancellationToken) method.
        /// </summary>
//         [Fact] [Error] (350-27)CS0122 'TransparentReadStream.TransparentReadStream(Stream)' is inaccessible due to its protection level
//         public async Task FlushAsync_CallsUnderlyingStreamFlushAsync()
//         {
//             // Arrange
//             var mockStream = new Mock<Stream>();
//             mockStream.Setup(s => s.CanRead).Returns(true);
//             mockStream.Setup(s => s.FlushAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
//             var trs = new TransparentReadStream(mockStream.Object);
// 
//             // Act
//             await trs.FlushAsync(CancellationToken.None);
// 
//             // Assert
//             mockStream.Verify(s => s.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
//         }

        #endregion

        #region Position and Seek Tests

        /// <summary>
        /// Tests that setting the Position property advances the stream by reading the required number of bytes.
        /// </summary>
        [Fact]
        public void PositionSetter_SkipsBytesProperly()
        {
            // Arrange
            using var memoryStream = new MemoryStream(_testData);
            var trs = TransparentReadStream.EnsureTransparentReadStream(memoryStream);
            // Read 3 bytes first.
            byte[] initialBuffer = new byte[3];
            int read = trs.Read(initialBuffer, 0, 3);
            Assert.Equal(3, read);
            // Act: set position to 7 (which should skip additional 4 bytes)
            trs.Position = 7;
            // Assert: Reading next byte should be the 8th byte (index 7) in _testData.
            int nextByte = trs.ReadByte();
            Assert.Equal(_testData[7], (byte)nextByte);
        }

        /// <summary>
        /// Tests that Seek with SeekOrigin.Current adjusts the position accordingly.
        /// </summary>
        [Fact]
        public void Seek_WithSeekOriginCurrent_AdjustsPosition()
        {
            // Arrange
            using var memoryStream = new MemoryStream(_testData);
            var trs = TransparentReadStream.EnsureTransparentReadStream(memoryStream);
            // Read 2 bytes to advance position.
            byte[] buffer = new byte[2];
            int read = trs.Read(buffer, 0, 2);
            Assert.Equal(2, read);

            // Act: seek 3 bytes forward.
            long newPosition = trs.Seek(3, SeekOrigin.Current);

            // Assert
            Assert.Equal(5, newPosition);
            // Further reading should continue from position 5.
            int nextByte = trs.ReadByte();
            Assert.Equal(_testData[5], (byte)nextByte);
        }

        /// <summary>
        /// Tests that Seek with a non-SeekOrigin.Current origin throws a NotSupportedException.
        /// </summary>
        [Fact]
        public void Seek_WithNonCurrentOrigin_ThrowsNotSupportedException()
        {
            // Arrange
            using var memoryStream = new MemoryStream(_testData);
            var trs = TransparentReadStream.EnsureTransparentReadStream(memoryStream);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => trs.Seek(3, SeekOrigin.Begin));
            Assert.Throws<NotSupportedException>(() => trs.Seek(3, SeekOrigin.End));
        }

        #endregion

        #region Unsupported Methods Tests

        /// <summary>
        /// Tests that SetLength(long) throws a NotSupportedException.
        /// </summary>
        [Fact]
        public void SetLength_ThrowsNotSupportedException()
        {
            // Arrange
            using var memoryStream = new MemoryStream(_testData);
            var trs = TransparentReadStream.EnsureTransparentReadStream(memoryStream);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => trs.SetLength(100));
        }

        /// <summary>
        /// Tests that Write(byte[], int, int) throws a NotSupportedException.
        /// </summary>
        [Fact]
        public void Write_ThrowsNotSupportedException()
        {
            // Arrange
            using var memoryStream = new MemoryStream(_testData);
            var trs = TransparentReadStream.EnsureTransparentReadStream(memoryStream);
            byte[] buffer = new byte[5];

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => trs.Write(buffer, 0, buffer.Length));
        }

        #endregion

        #region Close Tests

        /// <summary>
        /// Tests that Close() calls the underlying stream's Close() method.
        /// </summary>
//         [Fact] [Error] (468-27)CS0122 'TransparentReadStream.TransparentReadStream(Stream)' is inaccessible due to its protection level
//         public void Close_CallsUnderlyingStreamClose()
//         {
//             // Arrange
//             var mockStream = new Mock<Stream>();
//             mockStream.Setup(s => s.Close());
//             var trs = new TransparentReadStream(mockStream.Object);
// 
//             // Act
//             trs.Close();
// 
//             // Assert
//             mockStream.Verify(s => s.Close(), Times.Once);
//         }

        #endregion
    }
}
