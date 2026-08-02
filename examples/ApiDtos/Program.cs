using System.Globalization;
using Krampus.BinJson;
using Krampus.BinJson.Serialization;

var response = new PlayerResponse
{
    Id = 7,
    DisplayName = "mage",
    CreatedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)
};

BJsonValue payload = BJson.Serialize(response);
PlayerResponse? roundTrip = BJson.Deserialize<PlayerResponse>(payload);

Console.WriteLine(BJson.Stringify(payload));
Console.WriteLine($"Roundtrip DTO: {roundTrip?.DisplayName} created {roundTrip?.CreatedAt:O}");

[BJsonSerializable(NamingPolicy = NamingPolicy.CamelCase)]
public sealed class PlayerResponse
{
    public int Id { get; set; }

    [BJsonPropertyName("name")]
    public string DisplayName { get; set; } = string.Empty;

    [BJsonConverter(typeof(DateOnlyStringConverter))]
    public DateTime CreatedAt { get; set; }
}

public sealed class DateOnlyStringConverter : BJsonConverter<DateTime>
{
    public override BJsonValue Serialize(DateTime value, BJsonSerializationContext context)
    {
        return BJsonValue.Create(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    public override DateTime Deserialize(BJsonValue value, BJsonSerializationContext context)
    {
        return DateTime.ParseExact(
            value.StringValue,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}
