using System.Diagnostics;

namespace DotUtils.StreamUtils;

public static class StreamExtensions
{
    public static int ReadAtLeast(this Stream stream, byte[] buffer, int offset, int minimumBytes, bool throwOnEndOfStream)
    {
        Debug.Assert(offset + minimumBytes <= buffer.Length);

        int totalRead = 0;
        while (totalRead < minimumBytes)
        {
            int read = stream.Read(buffer, offset, minimumBytes - totalRead);
            if (read == 0)
            {
                if (throwOnEndOfStream)
                {
                    throw new InvalidDataException("Unexpected end of stream.");
                }

                return totalRead;
            }

            totalRead += read;
            offset += read;
        }

        return totalRead;
    }

    public static Stream ToReadableSeekableStream(this Stream stream)
    {
        return TransparentReadStream.CreateSeekableStream(stream);
    }

    /// <summary>
    /// Creates bounded read-only, forward-only view over an underlying stream.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static Stream Slice(this Stream stream, long length)
    {
        return new SubStream(stream, length);
    }
}
