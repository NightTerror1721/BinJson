#nullable enable

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Krampus.BinJson.Error;
using Krampus.BinJson.Serialization;

namespace Krampus.BinJson
{
    /// <summary>
    /// Specialized facade for CLR object serialization and deserialization.
    /// </summary>
    public static class BJsonTypedFacade
    {
        public static BJsonValue Serialize<T>(T? value, BJsonSerializerOptions? options = null)
        {
            return BJsonSerializer.Serialize(value, options);
        }

        public static BJsonValue Serialize(object? value, Type declaredType, BJsonSerializerOptions? options = null)
        {
            return BJsonSerializer.Serialize(value, declaredType, options);
        }

        public static T? Deserialize<T>(BJsonValue value, BJsonSerializerOptions? options = null)
        {
            return BJsonSerializer.Deserialize<T>(value, options);
        }

        public static object? Deserialize(BJsonValue value, Type targetType, BJsonSerializerOptions? options = null)
        {
            return BJsonSerializer.Deserialize(value, targetType, options);
        }

        public static byte[] SerializeToBytes<T>(T? value, BJsonSerializerOptions? options = null)
        {
            return BJsonBinaryFacade.SerializeToBytes(Serialize(value, options));
        }

        public static Task<byte[]> SerializeToBytesAsync<T>(T? value, BJsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return BJsonBinaryFacade.SerializeToBytesAsync(Serialize(value, options), cancellationToken);
        }

        public static T? Deserialize<T>(ReadOnlySpan<byte> data, BJsonSerializerOptions? options)
        {
            return Deserialize<T>(BJsonBinaryFacade.Deserialize(data), options);
        }

        public static T? Deserialize<T>(byte[] data, BJsonSerializerOptions? options)
        {
            return Deserialize<T>(BJsonBinaryFacade.Deserialize(data), options);
        }

        public static async Task<T?> DeserializeAsync<T>(ReadOnlyMemory<byte> data, BJsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            var value = await BJsonBinaryFacade.DeserializeAsync(data, cancellationToken).ConfigureAwait(false);
            return Deserialize<T>(value, options);
        }

        public static T? Parse<T>(string json, BJsonSerializerOptions? serializerOptions = null, Text.BJsonTextReaderOptions? textOptions = null)
        {
            if (json is null)
                throw new BJsonValidationException("Parameter 'json' cannot be null.");

            return Deserialize<T>(BJsonTextFacade.Parse(json, textOptions), serializerOptions);
        }

        public static Task<T?> ParseAsync<T>(string json, BJsonSerializerOptions? serializerOptions = null, Text.BJsonTextReaderOptions? textOptions = null, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<T?>(cancellationToken);
            return Task.FromResult(Parse<T>(json, serializerOptions, textOptions));
        }

        public static string Stringify<T>(T? value, BJsonSerializerOptions? serializerOptions = null, Text.BJsonTextWriterOptions? textOptions = null)
        {
            return Text.BJsonTextWriter.Serialize(Serialize(value, serializerOptions), textOptions);
        }

        public static Task<string> StringifyAsync<T>(T? value, BJsonSerializerOptions? serializerOptions = null, Text.BJsonTextWriterOptions? textOptions = null, CancellationToken cancellationToken = default)
        {
            return BJsonTextFacade.StringifyAsync(Serialize(value, serializerOptions), textOptions, cancellationToken);
        }

        public static void SerializeToFile(string filePath, object? value, Type declaredType, BJsonSerializerOptions? options = null)
        {
            BJsonBinaryFacade.SerializeToFile(filePath, Serialize(value, declaredType, options));
        }

        public static Task SerializeToFileAsync(string filePath, object? value, Type declaredType, BJsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return BJsonBinaryFacade.SerializeToFileAsync(filePath, Serialize(value, declaredType, options), cancellationToken);
        }

        public static T? DeserializeFromFile<T>(string filePath, BJsonSerializerOptions? options = null)
        {
            return Deserialize<T>(BJsonBinaryFacade.DeserializeFromFile(filePath), options);
        }

        public static object? DeserializeFromFile(string filePath, Type targetType, BJsonSerializerOptions? options = null)
        {
            return Deserialize(BJsonBinaryFacade.DeserializeFromFile(filePath), targetType, options);
        }

        public static async Task<T?> DeserializeFromFileAsync<T>(string filePath, BJsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            var value = await BJsonBinaryFacade.DeserializeFromFileAsync(filePath, cancellationToken).ConfigureAwait(false);
            return Deserialize<T>(value, options);
        }

        public static async Task<object?> DeserializeFromFileAsync(string filePath, Type targetType, BJsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            var value = await BJsonBinaryFacade.DeserializeFromFileAsync(filePath, cancellationToken).ConfigureAwait(false);
            return Deserialize(value, targetType, options);
        }

        public static T? ParseFile<T>(string filePath, BJsonSerializerOptions? serializerOptions = null, Text.BJsonTextReaderOptions? textOptions = null, Encoding? encoding = null)
        {
            return Deserialize<T>(BJsonTextFacade.ParseFile(filePath, textOptions, encoding), serializerOptions);
        }

        public static async Task<T?> ParseFileAsync<T>(string filePath, BJsonSerializerOptions? serializerOptions = null, Text.BJsonTextReaderOptions? textOptions = null, Encoding? encoding = null, CancellationToken cancellationToken = default)
        {
            var value = await BJsonTextFacade.ParseFileAsync(filePath, textOptions, encoding, cancellationToken).ConfigureAwait(false);
            return Deserialize<T>(value, serializerOptions);
        }
    }
}
