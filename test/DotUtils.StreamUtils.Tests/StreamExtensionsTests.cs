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
}
