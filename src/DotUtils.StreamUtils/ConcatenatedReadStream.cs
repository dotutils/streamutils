using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotUtils.StreamUtils;

public class ConcatenatedReadStream : Stream
{
    private readonly Queue<Stream> _streams;
    private long _position;

    public ConcatenatedReadStream(IEnumerable<Stream> streams)
        => _streams = EnsureStreamsAreReadable(streams);

    public ConcatenatedReadStream(params Stream[] streams)
        => _streams = EnsureStreamsAreReadable(streams);

    private static Queue<Stream> EnsureStreamsAreReadable(IEnumerable<Stream> streams)
    {
        var result = (streams is ICollection<Stream> collection) ? new Queue<Stream>(collection.Count) : new Queue<Stream>();

        foreach (Stream stream in streams)
        {
            if (!stream.CanRead)
            {
                throw new ArgumentException("All streams must be readable", nameof(streams));
            }

            if (stream is ConcatenatedReadStream concatenatedStream)
            {
                foreach (Stream subStream in concatenatedStream._streams)
                {
                    result.Enqueue(subStream);
                }
            }
            else
            {
                result.Enqueue(stream);
            }
        }

        return result;
    }

    public override void Flush()
    {
        throw new NotSupportedException("ConcatenatedReadStream is forward-only read-only");
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalBytesRead = 0;

        while (count > 0 && _streams.Count > 0)
        {
            int bytesRead = _streams.Peek().Read(buffer, offset, count);
            if (bytesRead == 0)
            {
                _streams.Dequeue().Dispose();
                continue;
            }

            totalBytesRead += bytesRead;
            offset += bytesRead;
            count -= bytesRead;
        }

        _position += totalBytesRead;
        return totalBytesRead;
    }

    public override int ReadByte()
    {
        while (_streams.Count > 0)
        {
            int value = _streams.Peek().ReadByte();
            if (value < 0)
            {
                _streams.Dequeue().Dispose();
                continue;
            }

            _position++;
            return value;
        }

        return -1;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int totalBytesRead = 0;

        while (count > 0 && _streams.Count > 0)
        {
            int bytesRead = await _streams.Peek().ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
#if NET
                await _streams.Dequeue().DisposeAsync().ConfigureAwait(false);
#else
                _streams.Dequeue().Dispose();
#endif
                continue;
            }

            totalBytesRead += bytesRead;
            offset += bytesRead;
            count -= bytesRead;
        }

        _position += totalBytesRead;
        return totalBytesRead;
    }

#if NET
    public override int Read(Span<byte> buffer)
    {
        int totalBytesRead = 0;

        while (!buffer.IsEmpty && _streams.Count > 0)
        {
            int bytesRead = _streams.Peek().Read(buffer);
            if (bytesRead == 0)
            {
                _streams.Dequeue().Dispose();
                continue;
            }

            totalBytesRead += bytesRead;
            buffer = buffer.Slice(bytesRead);
        }

        _position += totalBytesRead;
        return totalBytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int totalBytesRead = 0;

        while (!buffer.IsEmpty && _streams.Count > 0)
        {
            int bytesRead = await _streams.Peek().ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                _streams.Dequeue().Dispose();
                continue;
            }

            totalBytesRead += bytesRead;
            buffer = buffer.Slice(bytesRead);
        }

        _position += totalBytesRead;
        return totalBytesRead;
    }
#endif

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException("ConcatenatedReadStream is forward-only read-only");
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("ConcatenatedReadStream is forward-only read-only");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("ConcatenatedReadStream is forward-only read-only");
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _streams.Sum(s => s.Length);

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException("ConcatenatedReadStream is forward-only read-only");
    }
}

