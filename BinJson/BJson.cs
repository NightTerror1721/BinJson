#nullable enable

using System;
using System.IO;
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

        public static byte[] SerializeToBytes<T>(T? value, BJsonSerializerOptions? options = null)
        {
            return BJsonBinaryWriter.Serialize(Serialize(value, options));
        }

        public static BJsonValue Deserialize(ReadOnlySpan<byte> data)
        {
            return BJsonBinaryReader.Deserialize(data);
        }

        public static T? Deserialize<T>(ReadOnlySpan<byte> data, BJsonSerializerOptions? options)
        {
            return Deserialize<T>(BJsonBinaryReader.Deserialize(data), options);
        }

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

        public static BJsonValue Parse(TextReader reader, bool leaveOpen = false)
        {
            return BJsonTextReader.Deserialize(reader, options: null, leaveOpen);
        }

        public static BJsonValue Parse(TextReader reader, BJsonTextReaderOptions? options, bool leaveOpen = false)
        {
            return BJsonTextReader.Deserialize(reader, options, leaveOpen);
        }

        public static BJsonValue ParseJson(Stream stream, bool leaveOpen = false)
        {
            return BJsonTextReader.Deserialize(stream, options: null, leaveOpen);
        }

        public static BJsonValue ParseJson(Stream stream, BJsonTextReaderOptions? options, bool leaveOpen = false)
        {
            return BJsonTextReader.Deserialize(stream, options, leaveOpen);
        }

        public static string Stringify(BJsonValue value)
        {
            return BJsonTextWriter.Serialize(value);
        }

        public static string Stringify<T>(T? value, BJsonSerializerOptions? serializerOptions = null, BJsonTextWriterOptions? textOptions = null)
        {
            return BJsonTextWriter.Serialize(Serialize(value, serializerOptions), textOptions);
        }

        public static T? Parse<T>(string json, BJsonSerializerOptions? serializerOptions = null, BJsonTextReaderOptions? textOptions = null)
        {
            if (json is null)
                throw new BJsonValidationException("Parameter 'json' cannot be null.");

            return Deserialize<T>(BJsonTextReader.Deserialize(json, textOptions), serializerOptions);
        }

        public static void Stringify(TextWriter writer, BJsonValue value, bool leaveOpen = false)
        {
            BJsonTextWriter.Serialize(writer, value, options: null, leaveOpen);
        }
    }
}
