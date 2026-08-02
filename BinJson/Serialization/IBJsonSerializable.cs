#nullable enable

namespace Krampus.BinJson.Serialization
{
    public interface IBJsonSerializable
    {
        BJsonValue Serialize(BJsonSerializationContext context);
    }
}
