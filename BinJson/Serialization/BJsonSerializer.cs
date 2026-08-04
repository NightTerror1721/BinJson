#nullable enable

using System;
using System.Runtime.CompilerServices;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// High-level API for extensible object serialization and deserialization.
    /// Coordinates converters, interfaces, attributes, and reflection-based metadata.
    /// </summary>
    public static class BJsonSerializer
    {
        private static readonly BJsonRuntime _defaultRuntime = new BJsonRuntime();
        private static readonly ConditionalWeakTable<BJsonSerializerOptions, BJsonRuntime> _runtimeCache = new ConditionalWeakTable<BJsonSerializerOptions, BJsonRuntime>();

        public static BJsonValue Serialize<T>(T? value, BJsonSerializerOptions? options = null)
        {
            return GetRuntime(options).Serialize(value, typeof(T));
        }

        public static BJsonValue Serialize(object? value, Type declaredType, BJsonSerializerOptions? options = null)
        {
            if (declaredType is null)
                throw new BJsonValidationException("Parameter 'declaredType' cannot be null.");

            return GetRuntime(options).Serialize(value, declaredType);
        }

        public static T? Deserialize<T>(BJsonValue value, BJsonSerializerOptions? options = null)
        {
            return GetRuntime(options).Deserialize<T>(value);
        }

        public static object? Deserialize(BJsonValue value, Type targetType, BJsonSerializerOptions? options = null)
        {
            if (targetType is null)
                throw new BJsonValidationException("Parameter 'targetType' cannot be null.");

            return GetRuntime(options).Deserialize(value, targetType);
        }

        private static BJsonRuntime GetRuntime(BJsonSerializerOptions? options)
        {
            if (options is null)
                return _defaultRuntime;

            return _runtimeCache.GetValue(options, static key => new BJsonRuntime(key));
        }
    }
}
