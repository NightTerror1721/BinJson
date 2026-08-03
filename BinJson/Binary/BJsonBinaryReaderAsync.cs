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
            using var memory = new MemoryStream();
            await Stream.CopyToAsync(memory, 81920, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            memory.Position = 0;
            using var core = new BJsonBinaryReaderCore(memory, leaveOpen: true, Options);
            return core.Read();
        }

        public static async Task<BJsonValue> DeserializeAsync(Stream stream, bool leaveOpen = false, CancellationToken cancellationToken = default, BJsonBinaryReaderOptions? options = null)
        {
            using var reader = new BJsonBinaryReaderAsync(stream, leaveOpen, options);
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async Task<BJsonValue> DeserializeAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default, BJsonBinaryReaderOptions? options = null)
        {
            if (cancellationToken.IsCancellationRequested)
                return await Task.FromCanceled<BJsonValue>(cancellationToken).ConfigureAwait(false);

            using var stream = new MemoryStream(data.ToArray(), writable: false);
            using var reader = new BJsonBinaryReaderAsync(stream, leaveOpen: true, options);
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}