#nullable enable

using System;
using Krampus.BinJson.Error;
using Krampus.BinJson.Serialization.References;

namespace Krampus.BinJson.Serialization
{
    public sealed class BJsonDeserializationContext
    {
        private readonly BJsonObjectSerializer _serializer;

        internal BJsonDeserializationContext(BJsonObjectSerializer serializer, BJsonSerializerOptions options, Type targetType)
        {
            _serializer = serializer;
            Options = options ?? throw new BJsonValidationException("Parameter 'options' cannot be null.");
            TargetType = targetType ?? throw new BJsonValidationException("Parameter 'targetType' cannot be null.");
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
            return _serializer.DeserializeValue(value, type ?? throw new BJsonValidationException("Parameter 'type' cannot be null."));
        }
    }
}
