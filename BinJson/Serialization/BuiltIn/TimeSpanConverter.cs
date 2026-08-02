#nullable enable

using System;
using System.Globalization;

namespace Krampus.BinJson.Serialization.BuiltIn
{
    public sealed class TimeSpanConverter : BJsonConverter<TimeSpan>
    {
        public override BJsonValue Serialize(TimeSpan value, BJsonSerializationContext context)
        {
            return BJsonValue.Create(value.ToString("c", CultureInfo.InvariantCulture));
        }

        public override TimeSpan Deserialize(BJsonValue value, BJsonSerializationContext context)
        {
            return TimeSpan.ParseExact(value.StringValue, "c", CultureInfo.InvariantCulture);
        }
    }
}
