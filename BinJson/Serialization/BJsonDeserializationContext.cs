#nullable enable

using System;
using Krampus.BinJson.Serialization.References;

namespace Krampus.BinJson.Serialization
{
    public sealed class BJsonDeserializationContext
    {
        private readonly BJsonObjectSerializer _serializer;

        internal BJsonDeserializationContext(BJsonObjectSerializer serializer, BJsonSerializerOptions options, Type targetType)
        {
            _serializer = serializer;
            Options = options ?? throw new ArgumentNullException(nameof(options));
            TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
            ReferenceResolver = options.ReferenceHandler?.CreateResolver();
        }

        public BJsonSerializerOptions Options { get; }

        public Type TargetType { get; }

        public ReferenceResolver? ReferenceResolver { get; }

        public T? Deserialize<T>(BJsonValue value)
        {
            return (T?)_serializer.DeserializeValue(value, typeof(T));
        }

        public object? Deserialize(BJsonValue value, Type type)
        {
            return _serializer.DeserializeValue(value, type ?? throw new ArgumentNullException(nameof(type)));
        }
    }
}
