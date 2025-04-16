using DotUtils.StreamUtils;
using static DotUtils.StreamUtils.Tests.StreamTestExtensions;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace DotUtils.StreamUtils.Tests
{
    public class SubStreamTests
    {
        [Fact]
        public void ReadByte_ReadsOnlyAllowedBounderies()
        {
            using MemoryStream ms1 = new(new byte[] { 1, 2, 3, 4 });

            ms1.ReadByte().Should().Be(1);

            Stream stream = new SubStream(ms1, 2);

            stream.ReadByte().Should().Be(2);
            stream.ReadByte().Should().Be(3);

            // cannot read anymore
            stream.ReadByte().Should().Be(-1);

            ms1.ReadByte().Should().Be(4);

            // cannot read anymore
            ms1.ReadByte().Should().Be(-1);
        }

        [Theory]
        [MemberData(nameof(StreamTestExtensions.EnumerateReadFunctionTypes), MemberType = typeof(StreamTestExtensions))]
        public void Read_ReadsOnlyAllowedBounderies(StreamFunctionType streamFunctionType)
        {
            using MemoryStream ms1 = new(new byte[] { 1, 2, 3, 4, 5, 6 });

            ms1.ReadByte().Should().Be(1);

            Stream stream = new SubStream(ms1, 3);

            ReadBytes readBytes = stream.GetReadFunc(streamFunctionType);

            var readBuffer = new byte[2];
            readBytes(readBuffer).Should().Be(2);
            readBuffer.Should().BeEquivalentTo(new byte[] { 2, 3 });

            Array.Clear(readBuffer);

            readBytes(readBuffer).Should().Be(1);
            readBuffer.Should().BeEquivalentTo(new byte[] { 4, 0 });

            // cannot read anymore
            stream.ReadByte().Should().Be(-1);

            ms1.ReadByte().Should().Be(5);
            ms1.ReadByte().Should().Be(6);

            // cannot read anymore
            ms1.ReadByte().Should().Be(-1);
        }

        /// <summary>
        /// Tests that the constructor throws an InvalidOperationException when the provided stream is not readable.
        /// Expected outcome: An InvalidOperationException is thrown with the appropriate message.
        /// </summary>
        [Fact]
        public void Constructor_StreamNotReadable_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockStream = new Mock<Stream>();
            mockStream.SetupGet(s => s.CanRead).Returns(false);

            // Act
            Action act = () => new SubStream(mockStream.Object, 10);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Stream must be readable.");
        }

        /// <summary>
        /// Tests that Read(byte[], int, int) returns only the allowed number of bytes when count exceeds remaining bytes.
        /// Expected outcome: The method returns only the remaining bytes and advances the internal position accordingly.
        /// </summary>
        [Fact]
        public void Read_WithCountGreaterThanRemaining_ReturnsRemainingBytes()
        {
            // Arrange
            byte[] sourceData = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            using MemoryStream memoryStream = new MemoryStream(sourceData);
            // Limit the substream to only first 5 bytes.
            SubStream subStream = new SubStream(memoryStream, 5);
            byte[] buffer = new byte[10];

            // Act
            int bytesRead = subStream.Read(buffer, 0, 10);

            // Assert
            bytesRead.Should().Be(5);
            buffer.AsSpan(0, bytesRead).ToArray().Should().Equal(new byte[] { 1, 2, 3, 4, 5 });

            // Further reading should return 0 bytes.
            int additionalRead = subStream.Read(buffer, 0, 10);
            additionalRead.Should().Be(0);
        }

        /// <summary>
        /// Tests that Read(byte[], int, int) returns zero when no bytes remain.
        /// Expected outcome: Zero is returned when attempting to read after the substream boundary.
        /// </summary>
        [Fact]
        public void Read_WhenNoBytesRemaining_ReturnsZero()
        {
            // Arrange
            byte[] sourceData = { 10, 20, 30 };
            using MemoryStream memoryStream = new MemoryStream(sourceData);
            SubStream subStream = new SubStream(memoryStream, 3);
            byte[] buffer = new byte[5];

            // Act
            // Read all available data.
            int firstRead = subStream.Read(buffer, 0, 3);
            // Attempt to read beyond the substream length.
            int secondRead = subStream.Read(buffer, 0, 3);

            // Assert
            firstRead.Should().Be(3);
            secondRead.Should().Be(0);
        }

        /// <summary>
        /// Tests that ReadByte() returns the correct byte when data is available and advances the stream position.
        /// Expected outcome: The returned byte equals the underlying stream's data and the subsequent call reads the next byte.
        /// </summary>
        [Fact]
        public void ReadByte_DataAvailable_ReturnsByteAndAdvances()
        {
            // Arrange
            byte[] sourceData = { 100, 101, 102, 103 };
            using MemoryStream memoryStream = new MemoryStream(sourceData);
            // Limit the substream to 3 bytes.
            SubStream subStream = new SubStream(memoryStream, 3);

            // Act
            int firstByte = subStream.ReadByte();
            int secondByte = subStream.ReadByte();
            int thirdByte = subStream.ReadByte();
            int fourthByte = subStream.ReadByte();

            // Assert
            firstByte.Should().Be(100);
            secondByte.Should().Be(101);
            thirdByte.Should().Be(102);
            // Since substream length is 3, the fourth read should return -1 even though stream has more data.
            fourthByte.Should().Be(-1);
        }

        /// <summary>
        /// Tests that ReadByte() returns -1 when no data remains in the substream.
        /// Expected outcome: The method returns -1 indicating the end of the substream.
        /// </summary>
        [Fact]
        public void ReadByte_NoDataRemaining_ReturnsMinusOne()
        {
            // Arrange
            byte[] sourceData = { 200 };
            using MemoryStream memoryStream = new MemoryStream(sourceData);
            // Limit the substream to 0 bytes.
            SubStream subStream = new SubStream(memoryStream, 0);

            // Act
            int result = subStream.ReadByte();

            // Assert
            result.Should().Be(-1);
        }

        /// <summary>
        /// Tests that ReadAsync(byte[], int, int, CancellationToken) returns only the allowed number of bytes when count exceeds the remaining bytes.
        /// Expected outcome: The method asynchronously returns only the remaining bytes and advances the internal position accordingly.
        /// </summary>
        [Fact]
        public async Task ReadAsync_WithCountGreaterThanRemaining_ReturnsRemainingBytesAsync()
        {
            // Arrange
            byte[] sourceData = { 11, 12, 13, 14, 15, 16 };
            using MemoryStream memoryStream = new MemoryStream(sourceData);
            // Limit the substream to the first 4 bytes.
            SubStream subStream = new SubStream(memoryStream, 4);
            byte[] buffer = new byte[10];

            // Act
            int bytesRead = await subStream.ReadAsync(buffer, 0, 10, CancellationToken.None);

            // Assert
            bytesRead.Should().Be(4);
            buffer.AsSpan(0, bytesRead).ToArray().Should().Equal(new byte[] { 11, 12, 13, 14 });

            // Further async reading should return 0.
            int additionalRead = await subStream.ReadAsync(buffer, 0, 5, CancellationToken.None);
            additionalRead.Should().Be(0);
        }

#if NET
        /// <summary>
        /// Tests that Read(Span<byte>) returns only the allowed number of bytes when the provided span length exceeds remaining bytes.
        /// Expected outcome: The method returns the number of bytes actually read, which is the minimum of the requested span length and remaining bytes.
        /// </summary>
        [Fact]
        public void ReadSpan_WithCountGreaterThanRemaining_ReturnsRemainingBytes()
        {
            // Arrange
            byte[] sourceData = { 21, 22, 23, 24, 25 };
            using MemoryStream memoryStream = new MemoryStream(sourceData);
            // Limit the substream to 3 bytes.
            SubStream subStream = new SubStream(memoryStream, 3);
            Span<byte> buffer = new byte[5];

            // Act
            int bytesRead = subStream.Read(buffer);

            // Assert
            bytesRead.Should().Be(3);
            buffer.Slice(0, bytesRead).ToArray().Should().Equal(new byte[] { 21, 22, 23 });

            // Reading again returns 0.
            int additionalRead = subStream.Read(buffer);
            additionalRead.Should().Be(0);
        }

        /// <summary>
        /// Tests that ReadAsync(Memory<byte>, CancellationToken) asynchronously returns only the allowed number of bytes when the memory length exceeds the remaining bytes.
        /// Expected outcome: The method returns a ValueTask that evaluates to the number of bytes read, which is the minimum of the memory length and remaining bytes.
        /// </summary>
        [Fact]
        public async Task ReadAsyncMemory_WithCountGreaterThanRemaining_ReturnsRemainingBytesAsync()
        {
            // Arrange
            byte[] sourceData = { 31, 32, 33, 34, 35, 36 };
            using MemoryStream memoryStream = new MemoryStream(sourceData);
            // Limit the substream to the first 4 bytes.
            SubStream subStream = new SubStream(memoryStream, 4);
            Memory<byte> buffer = new byte[10];

            // Act
            int bytesRead = await subStream.ReadAsync(buffer, CancellationToken.None);

            // Assert
            bytesRead.Should().Be(4);
            buffer.Span.Slice(0, bytesRead).ToArray().Should().Equal(new byte[] { 31, 32, 33, 34 });

            // Further async reading returns 0.
            bytesRead = await subStream.ReadAsync(buffer, CancellationToken.None);
            bytesRead.Should().Be(0);
        }
#endif
        }
}