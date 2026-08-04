#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Krampus.BinJson.Binary
{
    public sealed class BJsonBinaryReaderAsync : BJsonBinaryReaderBase
    {
        public BJsonBinaryReaderAsync(Stream stream, bool leaveOpen = false, BJsonBinaryReaderOptions? options = null)
            : base(stream, leaveOpen, options)
        {
        }

        public async Task<BJsonValue> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (TryGetReadableMemory(Stream, out ReadOnlyMemory<byte> data, out MemoryStream? memoryStream))
            {
                using var core = new BJsonBinaryReaderCore(data, Options);
                BJsonValue value = core.Read();
                if (memoryStream is not null)
                    memoryStream.Position += core.BytesRead;

                return value;
            }

            if (Stream.CanSeek)
            {
                long remaining = Stream.Length - Stream.Position;
                if (remaining >= 0 && remaining <= int.MaxValue)
                {
                    byte[] dataBuffer = new byte[(int)remaining];
                    int totalRead = 0;
                    while (totalRead < dataBuffer.Length)
                    {
                        int read = await Stream.ReadAsync(dataBuffer.AsMemory(totalRead), cancellationToken).ConfigureAwait(false);
                        if (read == 0)
                            break;

                        totalRead += read;
                    }

                    using var core = new BJsonBinaryReaderCore(dataBuffer.AsMemory(0, totalRead), Options);
                    return core.Read();
                }
            }

            using var memory = new MemoryStream();
            await Stream.CopyToAsync(memory, 81920, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            using var fallbackCore = new BJsonBinaryReaderCore(memory.ToArray(), Options);
            return fallbackCore.Read();
        }

        public static async Task<BJsonValue> DeserializeAsync(Stream stream, bool leaveOpen = false, CancellationToken cancellationToken = default, BJsonBinaryReaderOptions? options = null)
        {
            using var reader = new BJsonBinaryReaderAsync(stream, leaveOpen, options);
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        public static Task<BJsonValue> DeserializeAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default, BJsonBinaryReaderOptions? options = null)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<BJsonValue>(cancellationToken);

            using var core = new BJsonBinaryReaderCore(data, options);
            return Task.FromResult(core.Read());
        }

        private static bool TryGetReadableMemory(Stream stream, out ReadOnlyMemory<byte> data, out MemoryStream? memoryStream)
        {
            if (stream is MemoryStream candidate && candidate.TryGetBuffer(out ArraySegment<byte> segment))
            {
                int offset = checked((int)candidate.Position);
                int count = checked((int)(candidate.Length - candidate.Position));
                data = segment.AsMemory(segment.Offset + offset, count);
                memoryStream = candidate;
                return true;
            }

            data = default;
            memoryStream = null;
            return false;
        }
    }
}