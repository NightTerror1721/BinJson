#nullable enable

using System;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// High-level API for extensible object serialization and deserialization.
    /// Coordinates converters, interfaces, attributes, and reflection-based metadata.
    /// </summary>
    public static class BJsonSerializer
    {
        [ThreadStatic]
        private static BJsonObjectSerializer? _threadDefaultSerializer;

        public static BJsonValue Serialize<T>(T? value, BJsonSerializerOptions? options = null)
        {
            return GetSerializer(options).SerializeValue(value, typeof(T));
        }

        public static BJsonValue Serialize(object? value, Type declaredType, BJsonSerializerOptions? options = null)
        {
            if (declaredType is null)
                throw new BJsonValidationException("Parameter 'declaredType' cannot be null.");

            return GetSerializer(options).SerializeValue(value, declaredType);
        }

        public static T? Deserialize<T>(BJsonValue value, BJsonSerializerOptions? options = null)
        {
            return (T?)GetSerializer(options).DeserializeValue(value, typeof(T));
        }

        public static object? Deserialize(BJsonValue value, Type targetType, BJsonSerializerOptions? options = null)
        {
            if (targetType is null)
                throw new BJsonValidationException("Parameter 'targetType' cannot be null.");

            return GetSerializer(options).DeserializeValue(value, targetType);
        }

        private static BJsonObjectSerializer GetSerializer(BJsonSerializerOptions? options)
        {
            if (options is not null)
                return new BJsonObjectSerializer(options);

            return _threadDefaultSerializer ??= new BJsonObjectSerializer(options: null);
        }
    }
}
