#nullable enable

using System;
using Krampus.BinJson.Error;
using Krampus.BinJson.Serialization.Metadata;

namespace Krampus.BinJson.Serialization
{
    internal sealed class AttributeBasedConverter<T> : BJsonConverter<T>
    {
        private readonly BJsonObjectSerializer _serializer;

        public AttributeBasedConverter(BJsonObjectSerializer serializer)
        {
            _serializer = serializer ?? throw new BJsonValidationException("Parameter 'serializer' cannot be null.");
        }

        public override BJsonValue Serialize(T? value, BJsonSerializationContext context)
        {
            if (value is null)
                return BJsonValue.Null;

            return _serializer.SerializeAttributedObject(value!, typeof(T));
        }

        public override T? Deserialize(BJsonValue value, BJsonSerializationContext context)
        {
            return (T?)_serializer.DeserializeAttributedObject(value, typeof(T));
        }
    }
}
