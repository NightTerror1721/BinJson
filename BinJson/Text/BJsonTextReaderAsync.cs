#nullable enable

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Text
{
    public sealed class BJsonTextReaderAsync : BJsonTextReaderBase
    {
        public BJsonTextReaderAsync(TextReader reader, bool leaveOpen = false)
            : this(reader, BJsonTextReaderOptions.Default, leaveOpen)
        {
        }

        public BJsonTextReaderAsync(TextReader reader, BJsonTextReaderOptions? options, bool leaveOpen = false)
            : base(reader, options, leaveOpen)
        {
        }

        public async Task<BJsonValue> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return await Task.FromCanceled<BJsonValue>(cancellationToken).ConfigureAwait(false);

            try
            {
                string json = await Reader.ReadToEndAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return JsonTextParser.Parse(json, Options);
            }
            catch (Exception ex) when (ex is not BJsonException)
            {
                throw new BJsonParseException(
                    "Failed to read and parse JSON text.",
                    errorCode: BJsonErrorCode.TextReadParseError,
                    innerException: ex,
                    details: new System.Collections.Generic.Dictionary<string, object?>
                    {
                        ["operation"] = "ReadAsync"
                    });
            }
        }

        public static async Task<BJsonValue> DeserializeAsync(TextReader reader, BJsonTextReaderOptions? options = null, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            using var jsonReader = new BJsonTextReaderAsync(reader, options, leaveOpen);
            return await jsonReader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async Task<BJsonValue> DeserializeAsync(Stream stream, BJsonTextReaderOptions? options = null, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new BJsonValidationException("Parameter 'stream' cannot be null.");

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: leaveOpen);
            return await DeserializeAsync(reader, options, leaveOpen: false, cancellationToken).ConfigureAwait(false);
        }
    }
}