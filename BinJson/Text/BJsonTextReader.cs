#nullable enable

using System;
using System.IO;
using System.Text;

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
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _options = options ?? BJsonTextReaderOptions.Default;
            _leaveOpen = leaveOpen;
        }

        public BJsonValue Read()
        {
            string json = _reader.ReadToEnd();
            return JsonTextParser.Parse(json, _options);
        }

        public void Dispose()
        {
            if (!_leaveOpen)
                _reader.Dispose();
        }

        public static BJsonValue Deserialize(string json, BJsonTextReaderOptions? options = null)
        {
            if (json is null)
                throw new ArgumentNullException(nameof(json));

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
                throw new ArgumentNullException(nameof(stream));

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: leaveOpen);
            return Deserialize(reader, options, leaveOpen: false);
        }
    }
}
