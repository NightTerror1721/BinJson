using Krampus.BinJson;
using Krampus.BinJson.Serialization;
using Krampus.BinJson.Text;

var save = new GameSave
{
    PlayerName = "Hero",
    Level = 12,
    LastCheckpoint = "Crystal Cave",
    Inventory = new List<InventoryItem>
    {
        new InventoryItem { Name = "Potion", Quantity = 3 },
        new InventoryItem { Name = "Key", Quantity = 1 }
    }
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
