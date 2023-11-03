#if NET
using System.Buffers;
#endif
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

    public static long SkipBytes(this Stream stream)
        => SkipBytes(stream, stream.Length, true);

    public static long SkipBytes(this Stream stream, long bytesCount)
        => SkipBytes(stream, bytesCount, true);

    private static bool CheckIsSkipNeeded(long bytesCount)
    {
        if (bytesCount is < 0 or > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(bytesCount), $"Attempt to skip {bytesCount} bytes, only non-negative offset up to int.MaxValue is allowed.");
        }

        return bytesCount > 0;
    }

    public static int SkipBytes(this Stream stream, long bytesCount, bool throwOnEndOfStream)
    {
        if (!CheckIsSkipNeeded(bytesCount))
        {
            return 0;
        }

        const int bufferSize = 4096;
        byte[] buffer;

#if NET
        buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        using var _ = new CleanupScope(() => ArrayPool<byte>.Shared.Return(buffer));
#else
        buffer = new byte[bufferSize];
#endif
        return SkipBytes(stream, bytesCount, throwOnEndOfStream, buffer);
    }

    public static int SkipBytes(this Stream stream, long bytesCount, bool throwOnEndOfStream, byte[] buffer)
    {
        if (!CheckIsSkipNeeded(bytesCount))
        {
            return 0;
        }

        int totalRead = 0;
        while (totalRead < bytesCount)
        {
            int read = stream.Read(buffer, 0, (int)Math.Min(bytesCount - totalRead, buffer.Length));
            if (read == 0)
            {
                if (throwOnEndOfStream)
                {
                    throw new InvalidDataException("Unexpected end of stream.");
                }

                return totalRead;
            }

            totalRead += read;
        }

        return totalRead;
    }

    public static byte[] ReadToEnd(this Stream stream)
    {
        if (stream.TryGetLength(out long length))
        {
            BinaryReader reader = new(stream);
            return reader.ReadBytes((int)length);
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public static bool TryGetLength(this Stream stream, out long length)
    {
        try
        {
            length = stream.Length;
            return true;
        }
        catch (NotSupportedException)
        {
            length = 0;
            return false;
        }
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

    /// <summary>
    /// Creates a stream that concatenates the current stream with another stream.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static Stream Concat(this Stream stream, Stream other)
    {
        return new ConcatenatedReadStream(stream, other);
    }
}
