#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Krampus.BinJson.Binary
{
    public sealed class BJsonBinaryWriterAsync : BJsonBinaryWriterBase
    {
        public BJsonBinaryWriterAsync(Stream stream, bool leaveOpen = false, BJsonBinaryWriterOptions? options = null)
            : base(stream, leaveOpen, options)
        {
        }

        public async Task WriteAsync(BJsonValue value, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            using (var core = new BJsonBinaryWriterCore(buffer, leaveOpen: true, Options))
            {
                core.Write(value);
                core.Flush();
            }

            if (buffer.TryGetBuffer(out var segment))
            {
                await Stream.WriteAsync(segment.Array!, segment.Offset, (int)buffer.Length, cancellationToken).ConfigureAwait(false);
                return;
            }

            byte[] data = buffer.ToArray();
            await Stream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Stream.FlushAsync(cancellationToken);
        }

        public static async Task SerializeAsync(Stream stream, BJsonValue value, bool leaveOpen = false, CancellationToken cancellationToken = default, BJsonBinaryWriterOptions? options = null)
        {
            using var writer = new BJsonBinaryWriterAsync(stream, leaveOpen, options);
            await writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async Task<byte[]> SerializeAsync(BJsonValue value, CancellationToken cancellationToken = default, BJsonBinaryWriterOptions? options = null)
        {
            using var stream = new MemoryStream();
            using var writer = new BJsonBinaryWriterAsync(stream, leaveOpen: true, options);
            await writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            return stream.ToArray();
        }
    }
}