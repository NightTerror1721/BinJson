#nullable enable

namespace Krampus.BinJson.Serialization
{
    public interface IBJsonDeserializable
    {
        void Deserialize(BJsonValue value, BJsonDeserializationContext context);
    }
}
