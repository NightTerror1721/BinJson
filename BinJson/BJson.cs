#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Krampus.BinJson.Binary;
using Krampus.BinJson.Error;
using Krampus.BinJson.Serialization;
using Krampus.BinJson.Text;

namespace Krampus.BinJson
{
    public static class BJson
    {
        public static void Serialize(BJsonValue value, Stream stream, bool leaveOpen = false)
        {
            BJsonBinaryWriter.Serialize(stream, value, leaveOpen);
        }

        public static void Serialize(BJsonValue value, Stream stream, BJsonBinaryWriterOptions options, bool leaveOpen = false)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            BJsonBinaryWriter.Serialize(stream, value, leaveOpen, options);
        }

        public static Task SerializeAsync(BJsonValue value, Stream stream, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            return BJsonBinaryWriterAsync.SerializeAsync(stream, value, leaveOpen, cancellationToken);
        }

        public static Task SerializeAsync(BJsonValue value, Stream stream, BJsonBinaryWriterOptions options, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryWriterAsync.SerializeAsync(stream, value, leaveOpen, cancellationToken, options);
        }

        public static BJsonValue Serialize<T>(T? value, BJsonSerializerOptions? options = null)
        {
            return BJsonSerializer.Serialize(value, options);
        }

        public static BJsonValue Serialize(object? value, Type declaredType, BJsonSerializerOptions? options = null)
        {
            return BJsonSerializer.Serialize(value, declaredType, options);
        }

        public static BJsonValue Deserialize(Stream stream, bool leaveOpen = false)
        {
            return BJsonBinaryReader.Deserialize(stream, leaveOpen);
        }

        public static BJsonValue Deserialize(Stream stream, BJsonBinaryReaderOptions options, bool leaveOpen = false)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.Deserialize(stream, leaveOpen, options);
        }

        public static Task<BJsonValue> DeserializeAsync(Stream stream, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            return BJsonBinaryReaderAsync.DeserializeAsync(stream, leaveOpen, cancellationToken);
        }

        public static Task<BJsonValue> DeserializeAsync(Stream stream, BJsonBinaryReaderOptions options, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReaderAsync.DeserializeAsync(stream, leaveOpen, cancellationToken, options);
        }

        public static T? Deserialize<T>(BJsonValue value, BJsonSerializerOptions? options = null)
        {
            return BJsonSerializer.Deserialize<T>(value, options);
        }

        public static object? Deserialize(BJsonValue value, Type targetType, BJsonSerializerOptions? options = null)
        {
            return BJsonSerializer.Deserialize(value, targetType, options);
        }

        public static byte[] SerializeToBytes(BJsonValue value)
        {
            return BJsonBinaryWriter.Serialize(value);
        }

        public static byte[] SerializeToBytes(BJsonValue value, BJsonBinaryWriterOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryWriter.Serialize(value, options);
        }

        public static Task<byte[]> SerializeToBytesAsync(BJsonValue value, CancellationToken cancellationToken = default)
        {
            return BJsonBinaryWriterAsync.SerializeAsync(value, cancellationToken);
        }

        public static Task<byte[]> SerializeToBytesAsync(BJsonValue value, BJsonBinaryWriterOptions options, CancellationToken cancellationToken = default)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryWriterAsync.SerializeAsync(value, cancellationToken, options);
        }

        public static byte[] SerializeToBytes<T>(T? value, BJsonSerializerOptions? options = null)
        {
            return BJsonBinaryWriter.Serialize(Serialize(value, options));
        }

        public static Task<byte[]> SerializeToBytesAsync<T>(T? value, BJsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return SerializeToBytesAsync(Serialize(value, options), cancellationToken);
        }

        public static BJsonValue Deserialize(ReadOnlySpan<byte> data)
        {
            return BJsonBinaryReader.Deserialize(data);
        }

        public static BJsonValue Deserialize(byte[] data)
        {
            return BJsonBinaryReader.Deserialize(data);
        }

        public static BJsonValue Deserialize(ReadOnlyMemory<byte> data)
        {
            return BJsonBinaryReader.Deserialize(data);
        }

        public static BJsonValue Deserialize(ReadOnlySpan<byte> data, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.Deserialize(data, options);
        }

        public static BJsonValue Deserialize(byte[] data, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.Deserialize(data, options);
        }

        public static BJsonValue Deserialize(ReadOnlyMemory<byte> data, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.Deserialize(data, options);
        }

        public static Task<BJsonValue> DeserializeAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            return BJsonBinaryReaderAsync.DeserializeAsync(data, cancellationToken);
        }

        public static Task<BJsonValue> DeserializeAsync(ReadOnlyMemory<byte> data, BJsonBinaryReaderOptions options, CancellationToken cancellationToken = default)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReaderAsync.DeserializeAsync(data, cancellationToken, options);
        }

        public static void VisitBinary(Stream stream, BJsonBinaryVisitor visitor, bool leaveOpen = false)
        {
            BJsonBinaryReader.Visit(stream, visitor, leaveOpen);
        }

        public static void VisitBinary(Stream stream, BJsonBinaryVisitor visitor, BJsonBinaryReaderOptions options, bool leaveOpen = false)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            BJsonBinaryReader.Visit(stream, visitor, leaveOpen, options);
        }

        public static void VisitBinary(ReadOnlyMemory<byte> data, BJsonBinaryVisitor visitor)
        {
            BJsonBinaryReader.Visit(data, visitor);
        }

        public static void VisitBinary(byte[] data, BJsonBinaryVisitor visitor)
        {
            BJsonBinaryReader.Visit(data, visitor);
        }

        public static void VisitBinary(ReadOnlyMemory<byte> data, BJsonBinaryVisitor visitor, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            BJsonBinaryReader.Visit(data, visitor, options);
        }

        public static void VisitBinary(byte[] data, BJsonBinaryVisitor visitor, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            BJsonBinaryReader.Visit(data, visitor, options);
        }

        public static void VisitBinary(ReadOnlySpan<byte> data, BJsonBinaryVisitor visitor)
        {
            BJsonBinaryReader.Visit(data, visitor);
        }

        public static void VisitBinary(ReadOnlySpan<byte> data, BJsonBinaryVisitor visitor, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            BJsonBinaryReader.Visit(data, visitor, options);
        }

        public static bool TryDeserialize(ReadOnlySpan<byte> data, out BJsonValue value)
        {
            try
            {
                value = BJsonBinaryReader.Deserialize(data);
                return true;
            }
            catch
            {
                value = BJsonValue.Null;
                return false;
            }
        }

        public static bool TryDeserialize(byte[] data, out BJsonValue value)
        {
            try
            {
                value = BJsonBinaryReader.Deserialize(data);
                return true;
            }
            catch
            {
                value = BJsonValue.Null;
                return false;
            }
        }

        public static bool TryReadBinaryRootObjectProperty(ReadOnlyMemory<byte> data, string propertyName, out BJsonValue value)
        {
            return BJsonBinaryReader.TryReadRootObjectProperty(data, propertyName, out value);
        }

        public static bool TryReadBinaryRootObjectProperty(byte[] data, string propertyName, out BJsonValue value)
        {
            return BJsonBinaryReader.TryReadRootObjectProperty(data, propertyName, out value);
        }

        public static bool TryReadBinaryRootObjectProperty(ReadOnlySpan<byte> data, string propertyName, out BJsonValue value)
        {
            return BJsonBinaryReader.TryReadRootObjectProperty(data, propertyName, out value);
        }

        public static BJsonObject ReadBinaryRootObjectProperties(ReadOnlyMemory<byte> data, IReadOnlyList<string> propertyNames)
        {
            return BJsonBinaryReader.ReadRootObjectProperties(data, propertyNames);
        }

        public static BJsonObject ReadBinaryRootObjectProperties(byte[] data, IReadOnlyList<string> propertyNames)
        {
            return BJsonBinaryReader.ReadRootObjectProperties(data, propertyNames);
        }

        public static BJsonObject ReadBinaryRootObjectProperties(ReadOnlySpan<byte> data, IReadOnlyList<string> propertyNames)
        {
            return BJsonBinaryReader.ReadRootObjectProperties(data, propertyNames);
        }

        public static bool TryReadBinaryRootObjectProperty(ReadOnlyMemory<byte> data, string propertyName, out BJsonValue value, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.TryReadRootObjectProperty(data, propertyName, out value, options);
        }

        public static bool TryReadBinaryRootObjectProperty(byte[] data, string propertyName, out BJsonValue value, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.TryReadRootObjectProperty(data, propertyName, out value, options);
        }

        public static bool TryReadBinaryRootObjectProperty(ReadOnlySpan<byte> data, string propertyName, out BJsonValue value, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.TryReadRootObjectProperty(data, propertyName, out value, options);
        }

        public static BJsonObject ReadBinaryRootObjectProperties(ReadOnlyMemory<byte> data, IReadOnlyList<string> propertyNames, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.ReadRootObjectProperties(data, propertyNames, options);
        }

        public static BJsonObject ReadBinaryRootObjectProperties(byte[] data, IReadOnlyList<string> propertyNames, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.ReadRootObjectProperties(data, propertyNames, options);
        }

        public static BJsonObject ReadBinaryRootObjectProperties(ReadOnlySpan<byte> data, IReadOnlyList<string> propertyNames, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.ReadRootObjectProperties(data, propertyNames, options);
        }

        public static async Task<(bool Success, BJsonValue Value)> TryDeserializeAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await BJsonBinaryReaderAsync.DeserializeAsync(data, cancellationToken).ConfigureAwait(false);
                return (true, value);
            }
            catch
            {
                return (false, BJsonValue.Null);
            }
        }

        public static T? Deserialize<T>(ReadOnlySpan<byte> data, BJsonSerializerOptions? options)
        {
            return Deserialize<T>(BJsonBinaryReader.Deserialize(data), options);
        }

        public static T? Deserialize<T>(byte[] data, BJsonSerializerOptions? options)
        {
            return Deserialize<T>(BJsonBinaryReader.Deserialize(data), options);
        }

        public static async Task<T?> DeserializeAsync<T>(ReadOnlyMemory<byte> data, BJsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            var value = await BJsonBinaryReaderAsync.DeserializeAsync(data, cancellationToken).ConfigureAwait(false);
            return Deserialize<T>(value, options);
        }

        public static void SerializeToFile(string filePath, BJsonValue value)
        {
            ValidateFilePath(filePath);
            using var stream = File.Create(filePath);
            Serialize(value, stream, leaveOpen: false);
        }

        public static void SerializeToFile(string filePath, object? value, Type declaredType, BJsonSerializerOptions? options = null)
        {
            SerializeToFile(filePath, Serialize(value, declaredType, options));
        }

        public static async Task SerializeToFileAsync(string filePath, BJsonValue value, CancellationToken cancellationToken = default)
        {
            ValidateFilePath(filePath);
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await SerializeAsync(value, stream, leaveOpen: false, cancellationToken).ConfigureAwait(false);
        }

        public static Task SerializeToFileAsync(string filePath, object? value, Type declaredType, BJsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return SerializeToFileAsync(filePath, Serialize(value, declaredType, options), cancellationToken);
        }

        public static BJsonValue DeserializeFromFile(string filePath)
        {
            ValidateFilePath(filePath);
            using var stream = File.OpenRead(filePath);
            return Deserialize(stream);
        }

        public static T? DeserializeFromFile<T>(string filePath, BJsonSerializerOptions? options = null)
        {
            return Deserialize<T>(DeserializeFromFile(filePath), options);
        }

        public static object? DeserializeFromFile(string filePath, Type targetType, BJsonSerializerOptions? options = null)
        {
            return Deserialize(DeserializeFromFile(filePath), targetType, options);
        }

        public static async Task<BJsonValue> DeserializeFromFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            ValidateFilePath(filePath);
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            return await DeserializeAsync(stream, leaveOpen: false, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<T?> DeserializeFromFileAsync<T>(string filePath, BJsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            var value = await DeserializeFromFileAsync(filePath, cancellationToken).ConfigureAwait(false);
            return Deserialize<T>(value, options);
        }

        public static async Task<object?> DeserializeFromFileAsync(string filePath, Type targetType, BJsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            var value = await DeserializeFromFileAsync(filePath, cancellationToken).ConfigureAwait(false);
            return Deserialize(value, targetType, options);
        }

        public static BJsonValue Parse(string json)
        {
            if (json is null)
                throw new BJsonValidationException("Parameter 'json' cannot be null.");

            return BJsonTextReader.Deserialize(json);
        }

        public static bool TryParse(string json, out BJsonValue value)
        {
            return TryParse(json, options: null, out value);
        }

        public static BJsonValue Parse(string json, BJsonTextReaderOptions? options)
        {
            if (json is null)
                throw new BJsonValidationException("Parameter 'json' cannot be null.");

            return BJsonTextReader.Deserialize(json, options);
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

        public static Task<BJsonValue> ParseAsync(TextReader reader, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            return BJsonTextReaderAsync.DeserializeAsync(reader, options: null, leaveOpen, cancellationToken);
        }

        public static BJsonValue Parse(TextReader reader, BJsonTextReaderOptions? options, bool leaveOpen = false)
        {
            return BJsonTextReader.Deserialize(reader, options, leaveOpen);
        }

        public static Task<BJsonValue> ParseAsync(TextReader reader, BJsonTextReaderOptions? options, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            return BJsonTextReaderAsync.DeserializeAsync(reader, options, leaveOpen, cancellationToken);
        }

        public static BJsonValue ParseJson(Stream stream, bool leaveOpen = false)
        {
            return BJsonTextReader.Deserialize(stream, options: null, leaveOpen);
        }

        public static Task<BJsonValue> ParseJsonAsync(Stream stream, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            return BJsonTextReaderAsync.DeserializeAsync(stream, options: null, leaveOpen, cancellationToken);
        }

        public static BJsonValue ParseJson(Stream stream, BJsonTextReaderOptions? options, bool leaveOpen = false)
        {
            return BJsonTextReader.Deserialize(stream, options, leaveOpen);
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

        public static BJsonObject ReadTextRootObjectProperties(string json, IReadOnlyList<string> propertyNames, BJsonTextReaderOptions? options = null)
        {
            return BJsonTextReader.ReadRootObjectProperties(json, propertyNames, options);
        }

        public static BJsonValue ParseFile(string filePath, BJsonTextReaderOptions? options = null, Encoding? encoding = null)
        {
            ValidateFilePath(filePath);
            using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: false);
            return Parse(reader, options, leaveOpen: false);
        }

        public static T? ParseFile<T>(string filePath, BJsonSerializerOptions? serializerOptions = null, BJsonTextReaderOptions? textOptions = null, Encoding? encoding = null)
        {
            return Deserialize<T>(ParseFile(filePath, textOptions, encoding), serializerOptions);
        }

        public static async Task<BJsonValue> ParseFileAsync(string filePath, BJsonTextReaderOptions? options = null, Encoding? encoding = null, CancellationToken cancellationToken = default)
        {
            ValidateFilePath(filePath);
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: false);
            return await ParseAsync(reader, options, leaveOpen: false, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<T?> ParseFileAsync<T>(string filePath, BJsonSerializerOptions? serializerOptions = null, BJsonTextReaderOptions? textOptions = null, Encoding? encoding = null, CancellationToken cancellationToken = default)
        {
            var value = await ParseFileAsync(filePath, textOptions, encoding, cancellationToken).ConfigureAwait(false);
            return Deserialize<T>(value, serializerOptions);
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

        public static string Stringify<T>(T? value, BJsonSerializerOptions? serializerOptions = null, BJsonTextWriterOptions? textOptions = null)
        {
            return BJsonTextWriter.Serialize(Serialize(value, serializerOptions), textOptions);
        }

        public static Task<string> StringifyAsync<T>(T? value, BJsonSerializerOptions? serializerOptions = null, BJsonTextWriterOptions? textOptions = null, CancellationToken cancellationToken = default)
        {
            return StringifyAsync(Serialize(value, serializerOptions), textOptions, cancellationToken);
        }

        public static T? Parse<T>(string json, BJsonSerializerOptions? serializerOptions = null, BJsonTextReaderOptions? textOptions = null)
        {
            if (json is null)
                throw new BJsonValidationException("Parameter 'json' cannot be null.");

            return Deserialize<T>(BJsonTextReader.Deserialize(json, textOptions), serializerOptions);
        }

        public static Task<T?> ParseAsync<T>(string json, BJsonSerializerOptions? serializerOptions = null, BJsonTextReaderOptions? textOptions = null, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<T?>(cancellationToken);
            return Task.FromResult(Parse<T>(json, serializerOptions, textOptions));
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

        public static BJsonValue Transform(BJsonValue value, Func<BJsonValue, BJsonValue> transformer, int maxDepth = 256)
        {
            if (transformer is null)
                throw new BJsonValidationException("Parameter 'transformer' cannot be null.");
            if (maxDepth < 0)
                throw new BJsonValidationException("Parameter 'maxDepth' cannot be negative.");

            return TransformCore(value, transformer, maxDepth);
        }

        private static BJsonValue TransformCore(BJsonValue value, Func<BJsonValue, BJsonValue> transformer, int depth)
        {
            if (depth <= 0)
                throw new BJsonValidationException("Maximum transform depth exceeded.");

            var transformed = transformer(value);

            if (transformed.TryGetArray(out var array))
            {
                var copy = new BJsonArray(array.Count);
                for (int i = 0; i < array.Count; i++)
                {
                    copy.Add(TransformCore(array[i], transformer, depth - 1));
                }
                return BJsonValue.Create(copy);
            }

            if (transformed.TryGetObject(out var obj))
            {
                var copy = new BJsonObject(obj.Count);
                foreach (var kvp in obj)
                {
                    copy.Add(kvp.Key, TransformCore(kvp.Value, transformer, depth - 1));
                }
                return BJsonValue.Create(copy);
            }

            return transformed;
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
