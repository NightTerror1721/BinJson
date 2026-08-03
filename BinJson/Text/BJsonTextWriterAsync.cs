#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Text
{
    public sealed class BJsonTextWriterAsync : BJsonTextWriterBase
    {
        public BJsonTextWriterAsync(TextWriter writer, bool leaveOpen = false)
            : this(writer, BJsonTextWriterOptions.Default, leaveOpen)
        {
        }

        public BJsonTextWriterAsync(TextWriter writer, BJsonTextWriterOptions? options, bool leaveOpen = false)
            : base(writer, options, leaveOpen)
        {
        }

        public Task WriteAsync(BJsonValue value, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            string json = BJsonTextWriterCore.SerializeToString(value, Options);
            return Writer.WriteAsync(json);
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await Writer.FlushAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception ex) when (ex is not BJsonException)
            {
                throw new BJsonSerializationException(
                    "Failed to flush JSON text writer.",
                    operation: "FlushAsync",
                    errorCode: BJsonErrorCode.TextSerializationError,
                    innerException: ex);
            }
        }

        public static async Task SerializeAsync(TextWriter writer, BJsonValue value, BJsonTextWriterOptions? options = null, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                await Task.FromCanceled(cancellationToken).ConfigureAwait(false);

            using var jsonWriter = new BJsonTextWriterAsync(writer, options, leaveOpen);
            await jsonWriter.WriteAsync(value, cancellationToken).ConfigureAwait(false);
            await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
