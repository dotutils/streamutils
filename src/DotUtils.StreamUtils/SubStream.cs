namespace DotUtils.StreamUtils;

/// <summary>
/// Bounded read-only, forward-only view over an underlying stream.
/// </summary>
public class SubStream : Stream
{
    // Do not Dispose/Close on Dispose/Close !!
    private readonly Stream _stream;
    private readonly long _length;
    private long _position;

    public SubStream(Stream stream, long length)
    {
        _stream = stream;
        _length = length;

        if (!stream.CanRead)
        {
            throw new InvalidOperationException("Stream must be readable.");
        }
    }

    public bool IsAtEnd => _position >= _length;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position { get => _position; set => throw new NotImplementedException(); }

    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count)
    {
        count = Math.Min((int)Math.Max(Length - _position, 0), count);
        int read = _stream.Read(buffer, offset, count);
        _position += read;
        return read;
    }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
    public override void SetLength(long value) => throw new NotImplementedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
}

