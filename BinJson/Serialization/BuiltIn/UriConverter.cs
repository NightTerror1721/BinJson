#nullable enable

using System;

namespace Krampus.BinJson.Serialization.BuiltIn
{
    public sealed class UriConverter : BJsonConverter<Uri>
    {
        public override BJsonValue Serialize(Uri? value, BJsonSerializationContext context)
        {
            return BJsonValue.Create(value?.OriginalString);
        }

        public override Uri? Deserialize(BJsonValue value, BJsonSerializationContext context)
        {
            if (value.IsNull)
                return null;

            return new Uri(value.StringValue, UriKind.RelativeOrAbsolute);
        }
    }
}
