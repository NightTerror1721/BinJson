#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    public interface IBJsonConverter
    {
        Type Type { get; }

        BJsonValue Serialize(object? value, BJsonSerializationContext context);

        object? Deserialize(BJsonValue value, BJsonSerializationContext context);
    }

    public abstract class BJsonConverter<T> : IBJsonConverter
    {
        public Type Type => typeof(T);

        public abstract BJsonValue Serialize(T? value, BJsonSerializationContext context);

        public abstract T? Deserialize(BJsonValue value, BJsonSerializationContext context);

        BJsonValue IBJsonConverter.Serialize(object? value, BJsonSerializationContext context)
        {
            return Serialize((T?)value, context);
        }

        object? IBJsonConverter.Deserialize(BJsonValue value, BJsonSerializationContext context)
        {
            return Deserialize(value, context);
        }
    }
}
