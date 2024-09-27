using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using static DotUtils.StreamUtils.Tests.StreamTestExtensions;

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
    }
}
