using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotUtils.StreamUtils.Tests
{
    public delegate int ReadBytes(byte[] bytes);

    public delegate void WriteBytes(byte[] bytes);


    public static class StreamTestExtensions
    {
        public enum StreamFunctionType
        {
            ReadArray,
            ReadSpan,
            ReadAsyncArray,
            ReadAsyncMemory,
        }

        public static ReadBytes GetReadFunc(this Stream stream, StreamFunctionType streamFunctionType)
            => streamFunctionType switch
            {
                StreamFunctionType.ReadArray => (bytes) => stream.Read(bytes, 0, bytes.Length),
                StreamFunctionType.ReadSpan => (bytes) => stream.Read(bytes),
                // We do sync over async here - since we are interested in just exercising the method code in tests.
                StreamFunctionType.ReadAsyncArray => (bytes) =>
                    stream.ReadAsync(bytes, 0, bytes.Length, CancellationToken.None).Result,
                StreamFunctionType.ReadAsyncMemory => (bytes) => stream.ReadAsync(bytes, CancellationToken.None).Result,
                _ => throw new ArgumentOutOfRangeException(nameof(streamFunctionType), streamFunctionType, null)
            };

        public static WriteBytes GetWriteFunc(this Stream stream, StreamFunctionType streamFunctionType)
            => streamFunctionType switch
            {
                StreamFunctionType.ReadArray => (bytes) => stream.Write(bytes, 0, bytes.Length),
                StreamFunctionType.ReadSpan => (bytes) => stream.Write(bytes),
                // We do sync over async here - since we are interested in just exercising the method code in tests.
                StreamFunctionType.ReadAsyncArray => (bytes) =>
                    stream.WriteAsync(bytes, 0, bytes.Length, CancellationToken.None).Wait(),
                StreamFunctionType.ReadAsyncMemory => (bytes) => stream.WriteAsync(bytes, CancellationToken.None).AsTask().Wait(),
                _ => throw new ArgumentOutOfRangeException(nameof(streamFunctionType), streamFunctionType, null)
            };

        public static IEnumerable<object[]> EnumerateReadFunctionTypes()
            => Enum.GetValues<StreamFunctionType>().Select(v => new object[] { v });
    }
}
