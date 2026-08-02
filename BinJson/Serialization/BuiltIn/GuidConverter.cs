#nullable enable

using System;

namespace Krampus.BinJson.Serialization.BuiltIn
{
    public sealed class GuidConverter : BJsonConverter<Guid>
    {
        public override BJsonValue Serialize(Guid value, BJsonSerializationContext context)
        {
            return BJsonValue.Create(value.ToString("D"));
        }

        public override Guid Deserialize(BJsonValue value, BJsonSerializationContext context)
        {
            return Guid.Parse(value.StringValue);
        }
    }
}
