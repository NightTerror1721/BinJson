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
    }
}
