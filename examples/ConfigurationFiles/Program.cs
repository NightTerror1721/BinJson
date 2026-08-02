using Krampus.BinJson;
using Krampus.BinJson.Serialization;
using Krampus.BinJson.Text;

var incoming = new BJsonObject
{
    ["environment"] = BJsonValue.Create("production"),
    ["featureFlag"] = BJsonValue.Create(true),
    ["retryCount"] = BJsonValue.Create(5)
};

ConfigurationDocument? config = BJson.Deserialize<ConfigurationDocument>(BJsonValue.Create(incoming));
config!.AppName = "BinJson Demo";

BJsonValue outgoing = BJson.Serialize(config);
string json = BJsonTextWriter.Serialize(outgoing, new BJsonTextWriterOptions { Indented = true, IndentSize = 2 });

Console.WriteLine(json);
Console.WriteLine($"Unknown values preserved: {config.ExtraData?.Count ?? 0}");

[BJsonSerializable(NamingPolicy = NamingPolicy.CamelCase)]
public sealed class ConfigurationDocument
{
    public string AppName { get; set; } = string.Empty;

    public string Environment { get; set; } = string.Empty;

    [BJsonExtensionData]
    public Dictionary<string, BJsonValue>? ExtraData { get; set; }
}
