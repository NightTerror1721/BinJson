#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    public static class BJsonGeneratorRuntimeBridge
    {
        public static BJsonValue SerializeAttributed(object value, Type type, BJsonSerializationContext context)
        {
            return context.SerializeAttributed(value, type);
        }

        public static T? DeserializeAttributed<T>(BJsonValue value, BJsonSerializationContext context)
        {
            return (T?)context.DeserializeAttributed(value, typeof(T));
        }
    }
}
