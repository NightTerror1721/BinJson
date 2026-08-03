#nullable enable

using System;
using System.IO;
using System.Text;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Text
{
    public sealed class BJsonTextReader : BJsonTextReaderBase
    {
        public BJsonTextReader(TextReader reader, bool leaveOpen = false)
            : this(reader, BJsonTextReaderOptions.Default, leaveOpen)
        {
        }

        public BJsonTextReader(TextReader reader, BJsonTextReaderOptions? options, bool leaveOpen = false)
            : base(reader, options, leaveOpen)
        {
        }

        public BJsonValue Read()
        {
            try
            {
                string json = Reader.ReadToEnd();
                return JsonTextParser.Parse(json, Options);
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

        public static BJsonValue Deserialize(Stream stream, BJsonTextReaderOptions? options = null, bool leaveOpen = false)
        {
            if (stream is null)
                throw new BJsonValidationException("Parameter 'stream' cannot be null.");

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: leaveOpen);
            return Deserialize(reader, options, leaveOpen: false);
        }

    }
}
