using System.IO.Compression;
using FluentAssertions;

namespace DotUtils.StreamUtils.Tests;

public class StreamExtensionsTests
{
    [Fact]
    public void ReadAtLeast_ThrowsOnEndOfStream()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var buffer = new byte[10];

        Assert.Throws<InvalidDataException>(() => stream.ReadAtLeast(buffer, 0, 10, throwOnEndOfStream: true));
    }

    [Fact]
    public void ReadToEnd_OnSeekableStream()
    {
        using MemoryStream ms1 = new(new byte[] { 1, 2, 3, 4, 5 });

        ms1.ReadByte();
        ms1.ReadByte();

        ms1.ReadToEnd().Should().BeEquivalentTo(new[] { 3, 4, 5 });

        // cannot read anymore
        ms1.ReadByte().Should().Be(-1);
    }

    [Fact]
    public void ReadToEnd_OnNonseekableStream()
    {
        using MemoryStream ms1 = new();
        GZipStream zipStream = new GZipStream(ms1, CompressionMode.Compress);
        zipStream.Write(new byte[] { 1, 2, 3, 4, 5 });
        zipStream.Flush();

        ms1.Position = 0;
        using GZipStream unzipStream = new GZipStream(ms1, CompressionMode.Decompress);

        unzipStream.ReadByte();
        unzipStream.ReadByte();

        unzipStream.ReadToEnd().Should().BeEquivalentTo(new[] { 3, 4, 5 });

        // cannot read anymore
        unzipStream.ReadByte().Should().Be(-1);
    }
}
