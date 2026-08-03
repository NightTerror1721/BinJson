#nullable enable

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Text
{
    public sealed class BJsonTextReader : IDisposable
    {
        private readonly TextReader _reader;
        private readonly bool _leaveOpen;
        private readonly BJsonTextReaderOptions _options;

        public BJsonTextReader(TextReader reader, bool leaveOpen = false)
            : this(reader, BJsonTextReaderOptions.Default, leaveOpen)
        {
        }

        public BJsonTextReader(TextReader reader, BJsonTextReaderOptions? options, bool leaveOpen = false)
        {
            _reader = reader ?? throw new BJsonValidationException("Parameter 'reader' cannot be null.");
            _options = options ?? BJsonTextReaderOptions.Default;
            _leaveOpen = leaveOpen;
        }

        public BJsonValue Read()
        {
            try
            {
                string json = _reader.ReadToEnd();
                return JsonTextParser.Parse(json, _options);
            }
            catch (Exception ex) when (!(ex is BJsonException))
            {
                throw new BJsonParseException(
                    "Failed to read and parse JSON text.",
                    errorCode: BJsonErrorCode.TextReadParseError,
                    innerException: ex,
                    details: new System.Collections.Generic.Dictionary<string, object?>
                    {
                        ["operation"] = "Read"
                    });
            }
        }

        public async Task<BJsonValue> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return await Task.FromCanceled<BJsonValue>(cancellationToken).ConfigureAwait(false);

            try
            {
                string json = await _reader.ReadToEndAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return JsonTextParser.Parse(json, _options);
            }
            catch (Exception ex) when (!(ex is BJsonException))
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

        public void Dispose()
        {
            if (!_leaveOpen)
                _reader.Dispose();
        }

        public static BJsonValue Deserialize(string json, BJsonTextReaderOptions? options = null)
        {
            if (json is null)
                throw new BJsonValidationException("Parameter 'json' cannot be null.");

            return JsonTextParser.Parse(json, options);
        }

        public static BJsonValue Deserialize(TextReader reader, BJsonTextReaderOptions? options = null, bool leaveOpen = false)
        {
            using var jsonReader = new BJsonTextReader(reader, options, leaveOpen);
            return jsonReader.Read();
        }

        public static async Task<BJsonValue> DeserializeAsync(TextReader reader, BJsonTextReaderOptions? options = null, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            using var jsonReader = new BJsonTextReader(reader, options, leaveOpen);
            return await jsonReader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        public static BJsonValue Deserialize(Stream stream, BJsonTextReaderOptions? options = null, bool leaveOpen = false)
        {
            if (stream is null)
                throw new BJsonValidationException("Parameter 'stream' cannot be null.");

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: leaveOpen);
            return Deserialize(reader, options, leaveOpen: false);
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
