#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// High-level API for extensible object serialization and deserialization.
    /// Coordinates converters, interfaces, attributes, and reflection-based metadata.
    /// </summary>
    public static class BJsonSerializer
    {
        public static BJsonValue Serialize<T>(T? value, BJsonSerializerOptions? options = null)
        {
            return new BJsonObjectSerializer(options).SerializeValue(value, typeof(T));
        }

        public static BJsonValue Serialize(object? value, Type declaredType, BJsonSerializerOptions? options = null)
        {
            if (declaredType is null)
                throw new ArgumentNullException(nameof(declaredType));

            return new BJsonObjectSerializer(options).SerializeValue(value, declaredType);
        }

        public static T? Deserialize<T>(BJsonValue value, BJsonSerializerOptions? options = null)
        {
            return (T?)new BJsonObjectSerializer(options).DeserializeValue(value, typeof(T));
        }

        public static object? Deserialize(BJsonValue value, Type targetType, BJsonSerializerOptions? options = null)
        {
            if (targetType is null)
                throw new ArgumentNullException(nameof(targetType));

            return new BJsonObjectSerializer(options).DeserializeValue(value, targetType);
        }
    }
}
