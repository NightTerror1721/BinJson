# BinJson

BinJson is a .NET library for representing JSON-like data using a lightweight DOM (`BJsonValue`, `BJsonArray`, `BJsonObject`, `BJsonBinary`) and serializing it both to a compact binary format and to JSON text.

## Project Status

Current status: functional for the DOM, binary serialization v1.0 (final, no legacy wire compatibility), and JSON text serialization **with no external dependencies**. Compatible with Unity 2021.2+.

It currently includes:
- A DOM for null, booleans, integers, floats, strings, arrays, objects, and binary values.
- Binary serialization in `Krampus.BinJson.Binary`.
- Official binary wire format v1.0 with fix types, fixed-width string lengths (`String8/16/32`), VarUInt counts/indexes, optional header blocks, StringTable/StringRef, and PackedArray support.
- JSON text serialization in `Krampus.BinJson.Text` with a **manual parser** (no `System.Text.Json`).
- Extensible object serialization in `Krampus.BinJson.Serialization`.
- Pretty-print and advanced configuration options.
- The public `Krampus.BinJson.BJson` facade.
- A test suite for values, collections, binary roundtrip, text roundtrip, extensibility, Unity compatibility, and performance.

## Features

- Compact, typed API for building JSON-like structures.
- Binary format documented in `docs/BinaryFormat.md`.
- Structural equality for arrays, objects, and binary values.
- Consistent numeric comparison between integers and floats.
- Safe numeric helper conversions with range checking.
- Reflection-based and generated object serialization.
- Attribute-driven contracts, custom converters, polymorphism, extension data, and constructor binding.
- Optional `$id` / `$ref` reference preservation for attributed object graphs.
- .NET Standard 2.1 compatibility for the library.
- **No external dependencies**: suitable for Unity and restricted environments.
- **Configurable pretty-printing** for readable JSON output.
- **Binary value validation**: binary values are forbidden by default in JSON text output.

## Sync and Async Architecture

BinJson uses an explicit split between synchronous and asynchronous APIs for text and binary I/O.

- Sync public types: `BJsonBinaryReader`, `BJsonBinaryWriter`, `BJsonTextReader`, `BJsonTextWriter`
- Async public types: `BJsonBinaryReaderAsync`, `BJsonBinaryWriterAsync`, `BJsonTextReaderAsync`, `BJsonTextWriterAsync`

To avoid behavior drift and duplicated protocol rules, shared logic is extracted into common layers:

- `*Base` classes: lifecycle concerns and common constructor validation
- `*Core` classes: shared format logic used by both sync and async wrappers

Design rules:

- Sync and async reader/writer classes do not call each other.
- Shared behavior belongs in `*Core` (or `*Base` when applicable).
- The `BJson` facade can expose both sync and async flows, but implementation remains split internally.

See [docs/Architecture.md](docs/Architecture.md) for details and contribution rules.

## Unity Compatibility

✅ **BinJson works on Unity 2021.2+ with no external dependencies.**

See the full guide: **[docs/UnitySetup.md](docs/UnitySetup.md)**

**Quick installation:**
1. Copy the `BinJson/` folder into `Assets/Scripts/BinJson/`.
2. Set Api Compatibility Level to **.NET Standard 2.1**.
3. Ready to use.

**Unity example:**
```csharp
using Krampus.BinJson;

var gameState = new BJsonObject {
    ["playerName"] = BJsonValue.Create("Hero"),
    ["health"] = BJsonValue.Create(100)
};
byte[] saveData = BJson.SerializeToBytes(BJsonValue.Create(gameState));
```

## Goals

- Keep the in-memory representation lightweight.
- Provide reliable roundtripping between the DOM, binary, and JSON text.
- Offer a simple facade for common use cases.
- Serve as a foundation for future performance improvements and format expansion.

## Quick Start

### Serialize a CLR object

```csharp
using Krampus.BinJson;
using Krampus.BinJson.Serialization;

[BJsonSerializable]
public sealed class SaveData
{
    public string PlayerName { get; set; } = string.Empty;
    public int Level { get; set; }
}

var value = BJson.Serialize(new SaveData { PlayerName = "Hero", Level = 10 });
var model = BJson.Deserialize<SaveData>(value);
```

### Build an object

```csharp
var obj = new BJsonObject();
obj.Add("id", 42);
obj.Add("name", "alice");
obj.Add("active", true);

BJsonValue value = BJsonValue.Create(obj);
```

### Serialize to binary

```csharp
byte[] bytes = BJson.SerializeToBytes(value);
```

### Deserialize from binary

```csharp
BJsonValue parsed = BJson.Deserialize(bytes);
```

### Convert to JSON text

```csharp
string json = BJson.Stringify(value);
```

### Convert to pretty-printed JSON

```csharp
using Krampus.BinJson.Text;

var options = new BJsonTextWriterOptions { Indented = true, IndentSize = 2 };
string prettyJson = BJsonTextWriter.Serialize(value, options);
```

### Parse JSON text

```csharp
BJsonValue parsed = BJson.Parse("{\"id\":42,\"name\":\"alice\"}");
```

### Try parse without exceptions

```csharp
if (BJson.TryParse("{\"id\":42}", out var value))
{
    int id = value.ObjectValue["id"].IntValue;
}
```

### Transform a DOM tree

```csharp
var input = BJsonValue.Create(new BJsonObject
{
    ["hp"] = 100,
    ["items"] = new BJsonArray { 1, 2, 3 }
});

var boosted = BJson.Transform(input, v =>
{
    if (v.IsInteger) return BJsonValue.Create(v.IntValue + 10);
    return v;
});
```

### Merge objects with strategy

```csharp
var baseConfig = new BJsonObject
{
    ["meta"] = new BJsonObject { ["version"] = 1, ["stable"] = true }
};

var patch = new BJsonObject
{
    ["meta"] = new BJsonObject { ["version"] = 2, ["name"] = "release" }
};

baseConfig.Merge(patch, BJsonMergeStrategy.DeepMerge);
```

### Binary codec helpers

```csharp
var bytes = new BJsonBinary(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
string base64 = bytes.ToBase64();
BJsonBinary roundTrip = BJsonBinary.FromBase64(base64);
```

## Custom Type Serialization

BinJson supports three main strategies for serializing custom CLR types.

### 1. Attributes

Use attributes when your type mostly maps to its public members and you want the smallest amount of custom code.

```csharp
using Krampus.BinJson.Serialization;

[BJsonSerializable]
public sealed class SaveData
{
    [BJsonPropertyName("player_name")]
    public string PlayerName { get; set; } = string.Empty;

    [BJsonIgnore(Condition = BJsonIgnoreCondition.WhenDefault)]
    public int Level { get; set; }
}
```

### 2. Custom converters

Use a converter when you want to control how a single value type or member is formatted.

```csharp
using Krampus.BinJson.Serialization;

public sealed class DateOnlyStringConverter : BJsonConverter<DateTime>
{
    public override BJsonValue Serialize(DateTime value, BJsonSerializationContext context)
    {
        return BJsonValue.Create(value.ToString("yyyy-MM-dd"));
    }

    public override DateTime Deserialize(BJsonValue value, BJsonSerializationContext context)
    {
        return DateTime.ParseExact(
            value.StringValue,
            "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
    }
}
```

### 3. Interfaces

Use interfaces when the serialized shape should be fully independent from the public members of the type.

```csharp
using Krampus.BinJson.Serialization;

public sealed class PlayerState : IBJsonSerializable, IBJsonDeserializable
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }

    public BJsonValue Serialize(BJsonSerializationContext context)
    {
        return BJsonValue.Create(new BJsonObject
        {
            ["playerName"] = BJsonValue.Create(Name),
            ["playerLevel"] = BJsonValue.Create(Level)
        });
    }

    public void Deserialize(BJsonValue value, BJsonDeserializationContext context)
    {
        var obj = value.ObjectValue;
        Name = obj["playerName"].StringValue;
        Level = obj["playerLevel"].IntValue;
    }
}
```

### Strategy comparison

| Strategy | Best for | Strengths | Trade-offs |
|----------|----------|-----------|------------|
| Attributes | DTO-style models | Minimal code, reflection support, source generator support | Less control over payload shape |
| Custom converters | Value formatting and reusable transformations | Centralized formatting logic, easy reuse | Focused on one target type at a time |
| Interfaces | Fully custom payloads | Complete control over read/write behavior | More manual code and maintenance |

For a complete guide, see [docs/Extensibility.md](docs/Extensibility.md).

## Main API

- `BJson.Serialize(BJsonValue, Stream)`
- `BJson.Serialize(BJsonValue, Stream, BJsonBinaryWriterOptions, bool)`
- `BJson.Serialize<T>(T?, BJsonSerializerOptions?)`
- `BJson.Serialize(object?, Type, BJsonSerializerOptions?)`
- `BJson.Deserialize(Stream)`
- `BJson.Deserialize(Stream, BJsonBinaryReaderOptions, bool)`
- `BJson.Deserialize<T>(BJsonValue, BJsonSerializerOptions?)`
- `BJson.Deserialize(BJsonValue, Type, BJsonSerializerOptions?)`
- `BJson.SerializeToBytes(BJsonValue)`
- `BJson.SerializeToBytes(BJsonValue, BJsonBinaryWriterOptions)`
- `BJson.SerializeToBytes<T>(T?, BJsonSerializerOptions?)`
- `BJson.SerializeToBytesAsync(BJsonValue, CancellationToken)`
- `BJson.SerializeToBytesAsync(BJsonValue, BJsonBinaryWriterOptions, CancellationToken)`
- `BJson.SerializeToBytesAsync<T>(T?, BJsonSerializerOptions?, CancellationToken)`
- `BJson.Deserialize(ReadOnlySpan<byte>)`
- `BJson.Deserialize(ReadOnlySpan<byte>, BJsonBinaryReaderOptions)`
- `BJson.DeserializeAsync(ReadOnlyMemory<byte>, CancellationToken)`
- `BJson.DeserializeAsync(ReadOnlyMemory<byte>, BJsonBinaryReaderOptions, CancellationToken)`
- `BJson.TryDeserialize(ReadOnlySpan<byte>, out BJsonValue)`
- `BJson.TryDeserializeAsync(ReadOnlyMemory<byte>, CancellationToken)`
- `BJson.SerializeToFile(string, BJsonValue)`
- `BJson.SerializeToFile(string, object?, Type, BJsonSerializerOptions?)`
- `BJson.SerializeToFileAsync(string, BJsonValue, CancellationToken)`
- `BJson.SerializeToFileAsync(string, object?, Type, BJsonSerializerOptions?, CancellationToken)`
- `BJson.DeserializeFromFile(string)`
- `BJson.DeserializeFromFile<T>(string, BJsonSerializerOptions?)`
- `BJson.DeserializeFromFile(string, Type, BJsonSerializerOptions?)`
- `BJson.DeserializeFromFileAsync(string, CancellationToken)`
- `BJson.DeserializeFromFileAsync<T>(string, BJsonSerializerOptions?, CancellationToken)`
- `BJson.DeserializeFromFileAsync(string, Type, BJsonSerializerOptions?, CancellationToken)`
- `BJson.Parse(string)`
- `BJson.TryParse(string, out BJsonValue)`
- `BJson.TryParse(string, BJsonTextReaderOptions?, out BJsonValue)`
- `BJson.ParseAsync(TextReader, ...)` and `BJson.ParseJsonAsync(Stream, ...)`
- `BJson.ParseFile(string, BJsonTextReaderOptions?, Encoding?)`
- `BJson.ParseFile<T>(string, BJsonSerializerOptions?, BJsonTextReaderOptions?, Encoding?)`
- `BJson.ParseFileAsync(string, BJsonTextReaderOptions?, Encoding?, CancellationToken)`
- `BJson.ParseFileAsync<T>(string, BJsonSerializerOptions?, BJsonTextReaderOptions?, Encoding?, CancellationToken)`
- `BJson.Parse<T>(string, BJsonSerializerOptions?, BJsonTextReaderOptions?)`
- `BJson.Stringify(BJsonValue)`
- `BJson.Stringify<T>(T?, BJsonSerializerOptions?, BJsonTextWriterOptions?)`
- `BJson.StringifyAsync(BJsonValue, BJsonTextWriterOptions?, CancellationToken)`
- `BJson.StringifyAsync<T>(T?, BJsonSerializerOptions?, BJsonTextWriterOptions?, CancellationToken)`
- `BJson.Stringify(TextWriter, BJsonValue, bool)` and `BJson.StringifyAsync(TextWriter, BJsonValue, ..., CancellationToken)`
- `BJson.StringifyToFile(string, BJsonValue, BJsonTextWriterOptions?, Encoding?)`
- `BJson.StringifyToFileAsync(string, BJsonValue, BJsonTextWriterOptions?, Encoding?, CancellationToken)`
- `BJson.Transform(BJsonValue, Func<BJsonValue, BJsonValue>, int)`
- `BJsonTextWriter.Serialize(BJsonValue, BJsonTextWriterOptions?)`
- `BJsonTextWriterAsync.SerializeAsync(TextWriter, BJsonValue, BJsonTextWriterOptions?, bool, CancellationToken)`
- `BJsonTextReader.Deserialize(string, BJsonTextReaderOptions?)`
- `BJsonTextReaderAsync.DeserializeAsync(TextReader|Stream, BJsonTextReaderOptions?, bool, CancellationToken)`
- `BJsonBinaryWriterAsync.SerializeAsync(Stream, BJsonValue, bool, CancellationToken, BJsonBinaryWriterOptions?)`
- `BJsonBinaryWriterAsync.SerializeAsync(BJsonValue, CancellationToken, BJsonBinaryWriterOptions?)`
- `BJsonBinaryReaderAsync.DeserializeAsync(Stream, bool, CancellationToken, BJsonBinaryReaderOptions?)`
- `BJsonBinaryReaderAsync.DeserializeAsync(ReadOnlyMemory<byte>, CancellationToken, BJsonBinaryReaderOptions?)`

## DOM Utility API

In addition to basic add/get operations, the DOM types expose utility APIs for conversion, cloning, query and composition.

- `BJsonValue`: `AsInt`, `AsLong`, `AsDouble`, `DeepClone`, relational operators (`<`, `<=`, `>`, `>=`), implicit/explicit conversions.
- `BJsonArray`: `Capacity`, `EnsureCapacity`, `TrimExcess`, `GetOrDefault`, typed default getters, `Find*`, `First`, `Last`, `Where`, `Select`, `Clone`, `DeepClone`.
- `BJsonObject`: `TryAdd`, `AddOrUpdate`, typed default getters, `Merge` with `BJsonMergeStrategy`, `Update`, `RenameKey`, `GetKeysByType`, `Clone`, `DeepClone`.
- `BJsonBinary`: `ToArray`, `CopyTo`, `ToBase64`/`FromBase64`, `ToHex`/`FromHex`, `FromString`, `DecodeString`.

## Known Limitations

- **Binary values in JSON**: forbidden by default. Use `BJsonTextWriterOptions { AllowBinaryAsBase64 = true }` if you need to serialize them as base64.
- **NaN/Infinity**: JSON text cannot represent `NaN` or infinities; an exception is thrown unless `SkipValidation = true`.
- **Manual parser**: JSON text parsing uses an internal parser with no external dependencies (it does not use `System.Text.Json`).
- **Pretty-print**: available through `BJsonTextWriterOptions.Indented = true`.

## Source Generator

BinJson includes a C# source generator that emits high-performance serializers for types marked with `[BJsonSerializable]`:

### Optimization Features

- **Zero reflection** - generated code uses direct member access
- **Optimized primitives** - uses `BJsonValue.Create()` and typed getters (`.IntValue`, `.StringValue`, etc.)
- **Deterministic member ordering** - respects `Order` attribute for predictable output
- **Constructor-based deserialization** - supports immutable types with `[BJsonConstructor]`
- **Static factory deserialization** - supports `[BJsonFactoryMethod]` with zero allocations
- **Ignore conditions** - all `BJsonIgnoreCondition` values handled at generation time
- **Static predicate calls** - `[BJsonIgnoreWhen]` emits a direct static method call, no allocations
- **Static mapper calls** - `[BJsonValueMapper]` emits a direct static method call, version-aware
- **Version-aware generation** - `[BJsonVersion]` range guards emitted inline
- **Default value injection** - `[BJsonDefaultValue]` and `[BJsonDefaultProvider]` handled at read time
- **Extension data** - collects unknown properties into `Dictionary<string, BJsonValue>`
- **Custom converters** - seamlessly integrates with `[BJsonConverter(typeof(...))]`

### Supported Attributes

| Attribute | Purpose |
|-----------|---------|
| `[BJsonSerializable]` | Marks type for generation; configures `IncludeFields`, `IncludePrivateMembers`, `NamingPolicy` |
| `[BJsonProperty]` | Customizes member serialization with `Name`, `Order`, `Required` |
| `[BJsonPropertyName]` | Specifies JSON property name (takes precedence over `BJsonProperty.Name`) |
| `[BJsonRequired]` | Requires member during deserialization |
| `[BJsonInclude]` | Forces inclusion of a non-public member |
| `[BJsonIgnore]` | Excludes member with static conditions: `Always`, `WhenWritingNull`, `WhenWritingDefault`, `WhenWritingCustomDefault`, `WhenWriting`, `WhenReading` |
| `[BJsonIgnoreWhen]` | Dynamic ignore via a static predicate method `(object?, string, IComparable?) → bool` on the declaring type |
| `[BJsonValueMapper]` | Transforms member value via a static mapper method on the declaring type; receives version and read/write direction |
| `[BJsonDefaultValue]` | Compile-time constant default applied when JSON key is absent during deserialization |
| `[BJsonDefaultProvider]` | Static method on the declaring type provides a complex default when JSON key is absent |
| `[BJsonVersion]` | Version range (`introducedIn` / `removedIn`) and `RenamedFrom` for legacy key migration |
| `[BJsonVersionContext]` | Declares the current document version for a type; used by version range checks and passed to predicate/mapper methods |
| `[BJsonExtensionData]` | Marks `IDictionary<string, BJsonValue>` for unknown properties |
| `[BJsonConverter]` | Applies custom converter to type or member |
| `[BJsonConstructor]` | Specifies deserialization constructor |
| `[BJsonFactoryMethod]` | Designates a static factory method for deserialization; supersedes `[BJsonConstructor]` |
| `[BJsonPolymorphic]` | Enables polymorphic serialization with type discriminators |
| `[BJsonDerivedType]` | Registers derived type with discriminator value |
| `[BJsonPreprocessor]` | Enables DOM pre-processing (conditionals, anchor resolution, variable substitution) before typed deserialization |
| `[BJsonAnchor]` | Registers a member value as a named anchor for `{ "$ref": "name" }` resolution within the document |
| `[BJsonExternalRef]` | Member is loaded from / written to an external BJson file |

### Diagnostics

The generator reports issues at compile time:

| ID | Description |
|----|-------------|
| BJSON001 | Invalid ExtensionData member type (`IDictionary<string, BJsonValue>` required) |
| BJSON002 | Multiple constructors with `[BJsonConstructor]` |
| BJSON003 | Multiple members with `[BJsonExtensionData]` |
| BJSON004 | Custom converter type not found |
| BJSON005 | Conflicting JSON property names |
| BJSON006 | Constructor/factory parameter cannot be matched to member |
| BJSON007 | Referenced attribute method not found |
| BJSON008 | Referenced attribute method is not accessible from generated code |
| BJSON009 | Referenced attribute method has invalid signature |
| BJSON010 | Unsupported type shape for source generation (for example generic or nested) |

## Performance

The project includes basic performance tests (`BJsonPerformanceTests`) that verify:
- Binary and text serialization/deserialization complete in under 1 second for large objects.
- The binary format is competitive in size with JSON text, depending on the payload.
- DOM construction is fast (1000 small objects in under 500ms).
- **Source-generated serializers** eliminate reflection overhead for attributed types.

## Additional Documentation

- Binary specification: `docs/BinaryFormat.md`
- Unity integration guide: `docs/UnitySetup.md`
- Extensibility guide: `docs/Extensibility.md`
- Error handling and error code catalog: `docs/ErrorHandling.md`
- **Attribute system reference & tutorial: `docs/Attributes.md`**
- Sync/async implementation architecture: `docs/Architecture.md`
