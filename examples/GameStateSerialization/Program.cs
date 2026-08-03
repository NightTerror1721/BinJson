using Krampus.BinJson;
using Krampus.BinJson.Serialization;
using Krampus.BinJson.Text;

var save = new GameSave
{
    PlayerName = "Hero",
    Level = 12,
    LastCheckpoint = "Crystal Cave",
    Inventory =
    [
        new InventoryItem { Name = "Potion", Quantity = 3 },
        new InventoryItem { Name = "Key", Quantity = 1 }
    ]
};

var serializerOptions = new BJsonSerializerOptions();
var textOptions = new BJsonTextWriterOptions { Indented = true, IndentSize = 2 };

BJsonValue value = BJson.Serialize(save, serializerOptions);
byte[] binary = BJson.SerializeToBytes(value);
string json = BJsonTextWriter.Serialize(value, textOptions);
GameSave? roundTrip = BJson.Deserialize<GameSave>(value, serializerOptions);

Console.WriteLine(json);
Console.WriteLine($"Binary size: {binary.Length} bytes");
Console.WriteLine($"Roundtrip player: {roundTrip?.PlayerName} (Level {roundTrip?.Level})");

[BJsonSerializable]
public sealed class GameSave
{
    public string PlayerName { get; set; } = string.Empty;

    public int Level { get; set; }

    public string LastCheckpoint { get; set; } = string.Empty;

    public List<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
}

[BJsonSerializable]
public sealed class InventoryItem
{
    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }
}

[BJsonSerializable]
public sealed class TestDataGame
{
    private string _playerName = string.Empty;
    private int _level;
    private DateTime? _lastPlayed;
    private double _score;
    private bool _isActive;
    private List<string> _achievements = [];
    private GameDifficulty _difficulty;

    public string PlayerName
    {
        get => _playerName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Player name cannot be empty.", nameof(value));
            _playerName = value;
        }
    }

    public int Level
    {
        get => _level;
        set
        {
            if (value < 1 || value > 100)
                throw new ArgumentOutOfRangeException(nameof(value), "Level must be between 1 and 100.");
            _level = value;
        }
    }

    public DateTime? LastPlayed
    {
        get => _lastPlayed;
        set => _lastPlayed = value;
    }

    public double Score
    {
        get => _score;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Score cannot be negative.");
            _score = value;
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set => _isActive = value;
    }

    public List<string> Achievements
    {
        get => _achievements;
        set => _achievements = value ?? [];
    }

    public GameDifficulty Difficulty
    {
        get => _difficulty;
        set
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentException("Invalid difficulty value.", nameof(value));
            _difficulty = value;
        }
    }
}

public enum GameDifficulty
{
    Easy,
    Normal,
    Hard,
    Extreme
}

[BJsonSerializable(NamingPolicy = NamingPolicy.SnakeCase)]
[BJsonVersionContext(typeof(Version), "3.0.0")]
[BJsonPreprocessor]
public sealed class CharacterSave
{
    // ── Basic members ────────────────────────────────────────────

    [BJsonRequired]
    public string Name { get; set; } = string.Empty;

    // ── Version-controlled members ────────────────────────────────

    // Introduced in v1.5; documents before 1.5 receive default 1
    [BJsonVersion(typeof(Version), introducedIn: "1.5.0")]
    [BJsonDefaultValue(1)]
    public int Level { get; set; }

    // Existed from v1.0 to v2.0; ignore when writing in newer versions
    [BJsonVersion(typeof(Version), introducedIn: "1.0.0", removedIn: "2.0.0")]
    [BJsonIgnore(Condition = BJsonIgnoreCondition.WhenWritingDefault)]
    public int LegacyRank { get; set; }

    // Renamed from "score" to "total_score" in v2.0
    [BJsonVersion(typeof(Version), introducedIn: "2.0.0", RenamedFrom = "score")]
    [BJsonDefaultValue(0)]
    public int TotalScore { get; set; }

    // ── Dynamic ignore ────────────────────────────────────────────

    // Skip empty guilds only in v3+ documents
    [BJsonIgnoreWhen(nameof(ShouldIgnoreGuild))]
    public string? Guild { get; set; }

    internal static bool ShouldIgnoreGuild(object? value, string propertyName, IComparable? version)
        => version != null
           && version.CompareTo(new Version("3.0.0")) >= 0
           && string.IsNullOrEmpty(value as string);

    // ── Value mapping ─────────────────────────────────────────────

    // v1.x stored health as percentage 0–100; v2+ uses raw HP integer
    [BJsonValueMapper(nameof(MapHealth))]
    [BJsonVersion(typeof(Version), introducedIn: "1.0.0")]
    public int Health { get; set; }

    internal static BJsonValue MapHealth(BJsonValue value, string propertyName, IComparable? version, bool isReading)
    {
        if (isReading && version != null && version.CompareTo(new Version("2.0.0")) < 0)
        {
            // percentage → raw HP
            var percentage = value.IntValue;
            return BJsonValue.Create(percentage * 10);
        }
        return value;
    }

    // ── Complex default ───────────────────────────────────────────

    [BJsonVersion(typeof(Version), introducedIn: "2.5.0")]
    [BJsonDefaultProvider(nameof(GetDefaultLoadout))]
    public Loadout Loadout { get; set; } = new Loadout();

    internal static Loadout GetDefaultLoadout() => new Loadout { Weapon = "Sword", Armor = "Leather" };

    // ── Extension data (forward compatibility) ────────────────────

    [BJsonExtensionData]
    public Dictionary<string, BJsonValue>? Extra { get; set; }

    // ── Anchor for $ref resolution ────────────────────────────────

    [BJsonAnchor("charName")]
    public string DisplayName => Name;
}

[BJsonSerializable]
public sealed class Loadout
{
    public string Weapon { get; set; } = string.Empty;
    public string Armor { get; set; } = string.Empty;
}

// ── Polymorphic base ──────────────────────────────────────────────

[BJsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[BJsonDerivedType(typeof(SwordItem))]
[BJsonDerivedType(typeof(BowItem))]
public abstract class Item
{
    public string Id { get; set; } = string.Empty;
}

[BJsonSerializable]
public sealed class SwordItem : Item
{
    public int Damage { get; set; }
}

[BJsonSerializable]
public sealed class BowItem : Item
{
    public float Range { get; set; }
}

// ── Factory method ────────────────────────────────────────────────

[BJsonSerializable]
public sealed class Currency
{
    private Currency(decimal amount, string code)
    {
        Amount = amount;
        Code = code;
    }

    public decimal Amount { get; }
    public string Code { get; }

    [BJsonFactoryMethod]
    public static Currency Create(decimal amount, string code) => new(amount, code);
}
