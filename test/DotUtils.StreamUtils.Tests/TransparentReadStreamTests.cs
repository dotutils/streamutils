using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DotUtils.StreamUtils.Tests.StreamTestExtensions;

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
    }
}
