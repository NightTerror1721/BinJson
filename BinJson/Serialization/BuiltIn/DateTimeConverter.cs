#nullable enable

using System;
using System.Globalization;

namespace Krampus.BinJson.Serialization.BuiltIn
{
    public sealed class DateTimeConverter : BJsonConverter<DateTime>
    {
        public override BJsonValue Serialize(DateTime value, BJsonSerializationContext context)
        {
            return BJsonValue.Create(value.ToString("O", CultureInfo.InvariantCulture));
        }

        public override DateTime Deserialize(BJsonValue value, BJsonSerializationContext context)
        {
            return DateTime.Parse(value.StringValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
    }
}
