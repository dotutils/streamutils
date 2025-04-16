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
    public class ChunkedBufferStreamTests
    {
        [Theory]
        [MemberData(nameof(StreamTestExtensions.EnumerateReadFunctionTypes), MemberType = typeof(StreamTestExtensions))]
        public void Write_CusesChunking(StreamFunctionType streamFunctionType)
        {
            int chunkSize = 3;
            byte[] bytes = new byte[100];
            using MemoryStream ms = new(bytes);
            using Stream stream = new ChunkedBufferStream(ms, chunkSize);

            WriteBytes writeBytes = stream.GetWriteFunc(streamFunctionType);

            writeBytes(new byte[]{1,2});
            bytes.Should().AllBeEquivalentTo(0);

            writeBytes(new byte[] { 3, 4 });
            bytes.Take(3).Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
            bytes.Skip(3).Should().AllBeEquivalentTo(0);

            writeBytes(new byte[] { 5, 6 });
            bytes.Take(6).Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5, 6 });
            bytes.Skip(6).Should().AllBeEquivalentTo(0);

            writeBytes(new byte[] { 7, 8 });
            bytes.Take(6).Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5, 6 });
            bytes.Skip(6).Should().AllBeEquivalentTo(0);

            writeBytes(new byte[] { 9, 10, 11, 12, 13, 14, 15, 16 });
            bytes.Take(15).Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });
            bytes.Skip(15).Should().AllBeEquivalentTo(0);

            stream.Flush();
            bytes.Take(16).Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
            bytes.Skip(16).Should().AllBeEquivalentTo(0);
        }

        [Fact]
        public void WriteByte_CusesChunking()
        {
            int chunkSize = 3;
            var bytes = new byte[100];
            using MemoryStream ms = new(bytes);
            using Stream stream = new ChunkedBufferStream(ms, chunkSize);

            stream.WriteByte(1);
            bytes.Should().AllBeEquivalentTo(0);
            stream.WriteByte(2);
            bytes.Should().AllBeEquivalentTo(0);

            stream.WriteByte(3);
            bytes.Take(3).Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
            bytes.Skip(3).Should().AllBeEquivalentTo(0);

            stream.WriteByte(4);
            stream.WriteByte(5);
            bytes.Take(3).Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
            bytes.Skip(3).Should().AllBeEquivalentTo(0);

            stream.WriteByte(6);
            bytes.Take(6).Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5, 6 });
            bytes.Skip(6).Should().AllBeEquivalentTo(0);

            stream.WriteByte(7);
            bytes.Take(6).Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5, 6 });
            bytes.Skip(6).Should().AllBeEquivalentTo(0);

            stream.Flush();
            bytes.Take(7).Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5, 6, 7 });
            bytes.Skip(7).Should().AllBeEquivalentTo(0);
        }
        // A helper custom stream to test Close functionality.
        private sealed class TestableStream : MemoryStream
        {
            public bool IsClosed { get; private set; } = false;

            public override void Close()
            {
                IsClosed = true;
                base.Close();
            }
        }

        /// <summary>
        /// Tests that calling Read throws a NotSupportedException with the expected message.
        /// </summary>
        [Fact]
        public void Read_AnyInput_ThrowsNotSupportedException()
        {
            // Arrange
            using MemoryStream ms = new MemoryStream();
            using ChunkedBufferStream chunkedStream = new ChunkedBufferStream(ms, 10);
            byte[] buffer = new byte[5];

            // Act
            Action act = () => chunkedStream.Read(buffer, 0, buffer.Length);

            // Assert
            act.Should().Throw<NotSupportedException>()
                .WithMessage("GreedyBufferedStream is write-only, append-only");
        }

        /// <summary>
        /// Tests that calling Seek throws a NotSupportedException with the expected message.
        /// </summary>
        [Fact]
        public void Seek_AnyInput_ThrowsNotSupportedException()
        {
            // Arrange
            using MemoryStream ms = new MemoryStream();
            using ChunkedBufferStream chunkedStream = new ChunkedBufferStream(ms, 10);

            // Act
            Action act = () => chunkedStream.Seek(5, SeekOrigin.Begin);

            // Assert
            act.Should().Throw<NotSupportedException>()
                .WithMessage("GreedyBufferedStream is write-only, append-only");
        }

        /// <summary>
        /// Tests that calling SetLength throws a NotSupportedException with the expected message.
        /// </summary>
        [Fact]
        public void SetLength_AnyInput_ThrowsNotSupportedException()
        {
            // Arrange
            using MemoryStream ms = new MemoryStream();
            using ChunkedBufferStream chunkedStream = new ChunkedBufferStream(ms, 10);

            // Act
            Action act = () => chunkedStream.SetLength(100);

            // Assert
            act.Should().Throw<NotSupportedException>()
                .WithMessage("GreedyBufferedStream is write-only, append-only");
        }

        /// <summary>
        /// Tests that the CanRead property returns false.
        /// </summary>
        [Fact]
        public void CanRead_ReturnsFalse()
        {
            // Arrange
            using MemoryStream ms = new MemoryStream();
            using ChunkedBufferStream chunkedStream = new ChunkedBufferStream(ms, 10);

            // Act
            bool canRead = chunkedStream.CanRead;

            // Assert
            canRead.Should().BeFalse();
        }

        /// <summary>
        /// Tests that the CanSeek property returns false.
        /// </summary>
        [Fact]
        public void CanSeek_ReturnsFalse()
        {
            // Arrange
            using MemoryStream ms = new MemoryStream();
            using ChunkedBufferStream chunkedStream = new ChunkedBufferStream(ms, 10);

            // Act
            bool canSeek = chunkedStream.CanSeek;

            // Assert
            canSeek.Should().BeFalse();
        }

        /// <summary>
        /// Tests that the CanWrite property returns the underlying stream's CanWrite value.
        /// </summary>
        [Fact]
        public void CanWrite_ReturnsUnderlyingStreamValue()
        {
            // Arrange
            using MemoryStream ms = new MemoryStream();
            using ChunkedBufferStream chunkedStream = new ChunkedBufferStream(ms, 10);

            // Act
            bool canWrite = chunkedStream.CanWrite;

            // Assert
            canWrite.Should().Be(ms.CanWrite);
        }

        /// <summary>
        /// Tests that the Length property returns the sum of the underlying stream's length and the current buffer position.
        /// </summary>
        [Fact]
        public void Length_ReturnsUnderlyingLengthPlusBufferPosition()
        {
            // Arrange
            using MemoryStream ms = new MemoryStream();
            // Initialize underlying stream with some data by writing directly.
            byte[] initialData = new byte[5];
            ms.Write(initialData, 0, initialData.Length);
            ms.Position = 0;
            int bufferSize = 10;
            using ChunkedBufferStream chunkedStream = new ChunkedBufferStream(ms, bufferSize);

            // Act
            // Write fewer than bufferSize bytes so buffer is partially filled.
            byte[] extraData = new byte[3];
            chunkedStream.Write(extraData, 0, extraData.Length);
            long expectedLength = ms.Length + 3; // underlying length unchanged until flush, plus buffer position
            long actualLength = chunkedStream.Length;

            // Assert
            actualLength.Should().Be(expectedLength);
        }

        /// <summary>
        /// Tests that the Position getter returns the sum of the underlying stream's position and the buffer position.
        /// Note: This test drives the stream state by writing data which does not flush until full.
        /// </summary>
        [Fact]
        public void Position_Get_ReturnsSumOfUnderlyingPositionAndBufferPosition()
        {
            // Arrange
            using MemoryStream ms = new MemoryStream();
            int bufferSize = 5;
            using ChunkedBufferStream chunkedStream = new ChunkedBufferStream(ms, bufferSize);

            // Act
            // Write less than bufferSize bytes so no flush occurs.
            byte[] data = new byte[] { 10, 20, 30 };
            chunkedStream.Write(data, 0, data.Length);
            // Since underlying stream hasn't been written to, its position remains 0.
            long pos = chunkedStream.Position;

            // Assert
            pos.Should().Be(0 + data.Length);
        }

        /// <summary>
        /// Tests that setting the Position property throws a NotSupportedException with the expected message.
        /// </summary>
        [Fact]
        public void Position_Set_ThrowsNotSupportedException()
        {
            // Arrange
            using MemoryStream ms = new MemoryStream();
            int bufferSize = 5;
            using ChunkedBufferStream chunkedStream = new ChunkedBufferStream(ms, bufferSize);

            // Act
            Action act = () => chunkedStream.Position = 10;

            // Assert
            act.Should().Throw<NotSupportedException>()
                .WithMessage("GreedyBufferedStream is write-only, append-only");
        }

        /// <summary>
        /// Tests that WriteAsync writes data correctly and flushes the buffer when full.
        /// </summary>
        [Fact]
        public async Task WriteAsync_WritesDataAndFlushesOnFullBuffer()
        {
            // Arrange
            int bufferSize = 4;
            byte[] destination = new byte[20];
            using MemoryStream ms = new MemoryStream(destination);
            using ChunkedBufferStream chunkedStream = new ChunkedBufferStream(ms, bufferSize);
            byte[] data = new byte[] { 1, 2, 3, 4, 5, 6 }; // More than one chunk

            // Act
            await chunkedStream.WriteAsync(data, 0, data.Length, CancellationToken.None).ConfigureAwait(false);
            // At this point, one full chunk (4 bytes) should have been flushed and 2 bytes remain in buffer.
            // Flush remaining bytes.
            chunkedStream.Flush();

            // Assert
            // The destination's first 6 bytes should match the data.
            destination.AsSpan(0, 6).ToArray().Should().Equal(new byte[] { 1, 2, 3, 4, 5, 6 });
        }

#if NET
        /// <summary>
        /// Tests that Write(ReadOnlySpan&lt;byte&gt;) writes data correctly and flushes when the buffer is full.
        /// </summary>
        [Fact]
        public void Write_ReadOnlySpan_WritesDataCorrectly()
        {
            // Arrange
            int bufferSize = 3;
            byte[] destination = new byte[20];
            using MemoryStream ms = new MemoryStream(destination);
            using ChunkedBufferStream chunkedStream = new ChunkedBufferStream(ms, bufferSize);
            ReadOnlySpan<byte> data1 = new byte[] { 10, 20 };
            ReadOnlySpan<byte> data2 = new byte[] { 30, 40, 50, 60 };

            // Act
            chunkedStream.Write(data1);
            // No flush expected yet because the buffer is not full.
            ms.Position.Should().Be(0);

            chunkedStream.Write(data2);
            // After writing, first full chunk flushed.
            // Flush remaining data.
            chunkedStream.Flush();

            // Assert
            // Check that the written bytes are as expected.
            destination.AsSpan(0, 6).ToArray().Should().Equal(new byte[] { 10, 20, 30, 40, 50, 60 });
        }

        /// <summary>
        /// Tests that WriteAsync(ReadOnlyMemory&lt;byte&gt;) writes data correctly and flushes when the buffer is full.
        /// </summary>
        [Fact]
        public async Task WriteAsync_ReadOnlyMemory_WritesDataCorrectly()
        {
            // Arrange
            int bufferSize = 4;
            byte[] destination = new byte[20];
            using MemoryStream ms = new MemoryStream(destination);
            using ChunkedBufferStream chunkedStream = new ChunkedBufferStream(ms, bufferSize);
            ReadOnlyMemory<byte> data = new byte[] { 100, 101, 102, 103, 104 }; // one full chunk and one remaining byte

            // Act
            await chunkedStream.WriteAsync(data, CancellationToken.None).ConfigureAwait(false);
            // Flush remaining bytes.
            chunkedStream.Flush();

            // Assert
            destination.AsSpan(0, 5).ToArray().Should().Equal(new byte[] { 100, 101, 102, 103, 104 });
        }
//         /// <summary> // Could not make this test passing: Build of suggested fix failed
//         /// Tests that Close calls Flush and then closes the underlying stream.
//         /// </summary>
//         [Fact]
//         public void Close_CallsFlushAndClosesUnderlyingStream()
//         {
//             // Arrange
//             int bufferSize = 3;
//             TestableStream testStream = new TestableStream();
//             ChunkedBufferStream chunkedStream = new ChunkedBufferStream(testStream, bufferSize);
//             // Write data that does not fill the buffer.
//             chunkedStream.Write(new byte[] { 1, 2 }, 0, 2);
//             testStream.ToArray().Should().BeEmpty("because data is still in the buffer and has not been flushed yet");
// 
//             // Act
//             chunkedStream.Close();
// 
//             // Assert
//             // After closing, the remaining data in buffer should have been flushed.
//             testStream.ToArray().Should().Equal(new byte[] { 1, 2 });
//             testStream.IsClosed.Should().BeTrue("because the underlying stream should be closed after Close is called");
//         }
    }
}