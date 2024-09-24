using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

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

        [Fact]
        public void Read_ReadsStreamSequentialy()
        {
            using MemoryStream ms1 = new(new byte[] { 1, 2, 3 });
            using MemoryStream ms2 = new(new byte[] { 4 });
            using MemoryStream ms3 = new(new byte[] { 5, 6 });
            using MemoryStream ms4 = new(new byte[] { 7, 8, 9 });

            Stream stream = new ConcatenatedReadStream(ms1, ms2, ms3, ms4);

            var readBuffer = new byte[2];

            stream.Read(readBuffer).Should().Be(2);
            readBuffer.Should().BeEquivalentTo(new byte[] { 1, 2 });

            stream.Read(readBuffer).Should().Be(2);
            readBuffer.Should().BeEquivalentTo(new byte[] { 3, 4 });

            stream.Read(readBuffer).Should().Be(2);
            readBuffer.Should().BeEquivalentTo(new byte[] { 5, 6 });

            stream.Read(readBuffer).Should().Be(2);
            readBuffer.Should().BeEquivalentTo(new byte[] { 7, 8 });

            // zero out for assertion clarity.
            Array.Clear(readBuffer);

            stream.Read(readBuffer).Should().Be(1);
            readBuffer.Should().BeEquivalentTo(new byte[] { 9, 0 });

            // zero out for assertion clarity.
            Array.Clear(readBuffer);

            // cannot read anymore
            stream.Read(readBuffer).Should().Be(0);
            readBuffer.Should().BeEquivalentTo(new byte[] { 0, 0 });
        }

        [Fact]
        public void Read_ReadsStreamSequentialy_UsingMultipleSubstreams()
        {
            using MemoryStream ms1 = new(new byte[] { 1, 2, 3 });
            using MemoryStream ms2 = new(new byte[] { 4 });
            using MemoryStream ms3 = new(new byte[] { 5, 6 });
            using MemoryStream ms4 = new(new byte[] { 7, 8, 9, 10, 11 });

            Stream stream = new ConcatenatedReadStream(ms1, ms2, ms3, ms4);

            var readBuffer = new byte[5];

            stream.Read(readBuffer).Should().Be(5);
            readBuffer.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5 });

            stream.Read(readBuffer).Should().Be(5);
            readBuffer.Should().BeEquivalentTo(new byte[] { 6, 7, 8, 9, 10 });

            // zero out for assertion clarity.
            Array.Clear(readBuffer);

            stream.Read(readBuffer).Should().Be(1);
            readBuffer.Should().BeEquivalentTo(new byte[] { 11, 0, 0, 0, 0 });

            // zero out for assertion clarity.
            Array.Clear(readBuffer);

            // cannot read anymore
            stream.Read(readBuffer).Should().Be(0);
            readBuffer.Should().BeEquivalentTo(new byte[] { 0, 0, 0, 0, 0 });
        }
    }
}
