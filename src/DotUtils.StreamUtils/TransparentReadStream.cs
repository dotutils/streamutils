#if NET
using System.Buffers;
#endif

namespace DotUtils.StreamUtils;

/// <summary>
/// A wrapper stream that allows position tracking and forward seeking.
/// </summary>
public class TransparentReadStream : Stream
{
    private readonly Stream _stream;
    private long _position;
    private long _maxAllowedPosition = long.MaxValue;

    public static Stream CreateSeekableStream(Stream stream)
    {
        if (stream.CanSeek)
        {
            return stream;
        }

        if(!stream.CanRead)
        {
            throw new InvalidOperationException("Stream must be readable.");
        }

        return new TransparentReadStream(stream);
    }

    private TransparentReadStream(Stream stream)
    {
        _stream = stream;
    }

    public int? BytesCountAllowedToRead
    {
        set => _maxAllowedPosition = value.HasValue ? _position + value.Value : long.MaxValue;
    }

    // if we haven't constrained the allowed read size - do not report it being unfinished either.
    public int BytesCountAllowedToReadRemaining =>
        _maxAllowedPosition == long.MaxValue ? 0 : (int)(_maxAllowedPosition - _position);

    public override bool CanRead => _stream.CanRead;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _stream.Length;
    public override long Position
    {
        get => _position;
        set => this.SkipBytes(value - _position, true);
    }

    public override void Flush()
    {
        _stream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_position + count > _maxAllowedPosition)
        {
            count = (int)(_maxAllowedPosition - _position);
        }

        int cnt = _stream.Read(buffer, offset, count);
        _position += cnt;
        return cnt;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if(origin != SeekOrigin.Current)
        {
            throw new InvalidOperationException("Only seeking from SeekOrigin.Current is supported.");
        }

        this.SkipBytes(offset, true);

        return _position;
    }

    public override void SetLength(long value)
    {
        throw new InvalidOperationException("Expanding stream is not supported.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new InvalidOperationException("Writing is not supported.");
    }

    public override void Close() => _stream.Close();
}

