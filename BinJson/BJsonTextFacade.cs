#nullable enable

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Krampus.BinJson.Error;
using Krampus.BinJson.Text;

namespace Krampus.BinJson
{
    /// <summary>
    /// Specialized facade for JSON text parse/stringify operations and text visitor flows.
    /// </summary>
    public static class BJsonTextFacade
    {
        public static BJsonValue Parse(string json)
        {
            if (json is null)
                throw new BJsonValidationException("Parameter 'json' cannot be null.");

            return BJsonTextReader.Deserialize(json);
        }

        public static BJsonValue Parse(string json, BJsonTextReaderOptions? options)
        {
            if (json is null)
                throw new BJsonValidationException("Parameter 'json' cannot be null.");

            return BJsonTextReader.Deserialize(json, options);
        }

        public static bool TryParse(string json, out BJsonValue value)
        {
            return TryParse(json, options: null, out value);
        }

        public static bool TryParse(string json, BJsonTextReaderOptions? options, out BJsonValue value)
        {
            if (json is null)
            {
                value = BJsonValue.Null;
                return false;
            }

            try
            {
                value = BJsonTextReader.Deserialize(json, options);
                return true;
            }
            catch
            {
                value = BJsonValue.Null;
                return false;
            }
        }

        public static BJsonValue Parse(TextReader reader, bool leaveOpen = false)
        {
            return BJsonTextReader.Deserialize(reader, options: null, leaveOpen);
        }

        public static BJsonValue Parse(TextReader reader, BJsonTextReaderOptions? options, bool leaveOpen = false)
        {
            return BJsonTextReader.Deserialize(reader, options, leaveOpen);
        }

        public static Task<BJsonValue> ParseAsync(TextReader reader, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            return BJsonTextReaderAsync.DeserializeAsync(reader, options: null, leaveOpen, cancellationToken);
        }

        public static Task<BJsonValue> ParseAsync(TextReader reader, BJsonTextReaderOptions? options, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            return BJsonTextReaderAsync.DeserializeAsync(reader, options, leaveOpen, cancellationToken);
        }

        public static BJsonValue ParseJson(Stream stream, bool leaveOpen = false)
        {
            return BJsonTextReader.Deserialize(stream, options: null, leaveOpen);
        }

        public static BJsonValue ParseJson(Stream stream, BJsonTextReaderOptions? options, bool leaveOpen = false)
        {
            return BJsonTextReader.Deserialize(stream, options, leaveOpen);
        }

        public static Task<BJsonValue> ParseJsonAsync(Stream stream, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            return BJsonTextReaderAsync.DeserializeAsync(stream, options: null, leaveOpen, cancellationToken);
        }

        public static Task<BJsonValue> ParseJsonAsync(Stream stream, BJsonTextReaderOptions? options, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            return BJsonTextReaderAsync.DeserializeAsync(stream, options, leaveOpen, cancellationToken);
        }

        public static void VisitText(string json, BJsonTextVisitor visitor, BJsonTextReaderOptions? options = null)
        {
            BJsonTextReader.Visit(json, visitor, options);
        }

        public static bool TryReadTextRootObjectProperty(string json, string propertyName, out BJsonValue value, BJsonTextReaderOptions? options = null)
        {
            return BJsonTextReader.TryReadRootObjectProperty(json, propertyName, out value, options);
        }

        public static BJsonObject ReadTextRootObjectProperties(string json, System.Collections.Generic.IReadOnlyList<string> propertyNames, BJsonTextReaderOptions? options = null)
        {
            return BJsonTextReader.ReadRootObjectProperties(json, propertyNames, options);
        }

        public static string Stringify(BJsonValue value)
        {
            return BJsonTextWriter.Serialize(value);
        }

        public static Task<string> StringifyAsync(BJsonValue value, BJsonTextWriterOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<string>(cancellationToken);
            return Task.FromResult(BJsonTextWriter.Serialize(value, options));
        }

        public static void Stringify(TextWriter writer, BJsonValue value, bool leaveOpen = false)
        {
            BJsonTextWriter.Serialize(writer, value, options: null, leaveOpen);
        }

        public static Task StringifyAsync(TextWriter writer, BJsonValue value, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            return BJsonTextWriterAsync.SerializeAsync(writer, value, options: null, leaveOpen, cancellationToken);
        }

        public static Task StringifyAsync(TextWriter writer, BJsonValue value, BJsonTextWriterOptions? options, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            return BJsonTextWriterAsync.SerializeAsync(writer, value, options, leaveOpen, cancellationToken);
        }

        public static BJsonValue ParseFile(string filePath, BJsonTextReaderOptions? options = null, Encoding? encoding = null)
        {
            ValidateFilePath(filePath);
            using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: false);
            return Parse(reader, options, leaveOpen: false);
        }

        public static async Task<BJsonValue> ParseFileAsync(string filePath, BJsonTextReaderOptions? options = null, Encoding? encoding = null, CancellationToken cancellationToken = default)
        {
            ValidateFilePath(filePath);
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: false);
            return await ParseAsync(reader, options, leaveOpen: false, cancellationToken).ConfigureAwait(false);
        }

        public static void StringifyToFile(string filePath, BJsonValue value, BJsonTextWriterOptions? options = null, Encoding? encoding = null)
        {
            ValidateFilePath(filePath);
            using var stream = File.Create(filePath);
            using var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
            BJsonTextWriter.Serialize(writer, value, options, leaveOpen: false);
        }

        public static async Task StringifyToFileAsync(string filePath, BJsonValue value, BJsonTextWriterOptions? options = null, Encoding? encoding = null, CancellationToken cancellationToken = default)
        {
            ValidateFilePath(filePath);
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            using var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
            await BJsonTextWriterAsync.SerializeAsync(writer, value, options, leaveOpen: false, cancellationToken).ConfigureAwait(false);
        }

        private static void ValidateFilePath(string filePath)
        {
            if (filePath is null)
                throw new BJsonValidationException("Parameter 'filePath' cannot be null.");
            if (filePath.Length == 0)
                throw new BJsonValidationException("Parameter 'filePath' cannot be empty.");
        }
    }
}
