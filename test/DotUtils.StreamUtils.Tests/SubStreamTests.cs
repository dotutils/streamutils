using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        [Fact]
        public void Read_ReadsOnlyAllowedBounderies()
        {
            using MemoryStream ms1 = new(new byte[] { 1, 2, 3, 4, 5, 6 });

            ms1.ReadByte().Should().Be(1);

            Stream stream = new SubStream(ms1, 3);

            var readBuffer = new byte[2];
            stream.Read(readBuffer).Should().Be(2);
            readBuffer.Should().BeEquivalentTo(new byte[] { 2, 3 });

            Array.Clear(readBuffer);

            stream.Read(readBuffer).Should().Be(1);
            readBuffer.Should().BeEquivalentTo(new byte[] { 4, 0 });

            // cannot read anymore
            stream.ReadByte().Should().Be(-1);

            ms1.ReadByte().Should().Be(5);
            ms1.ReadByte().Should().Be(6);

            // cannot read anymore
            ms1.ReadByte().Should().Be(-1);
        }
    }
}
