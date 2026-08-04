# BJson Attribute Reference

This document is the authoritative reference for every public attribute in `Krampus.BinJson.Serialization`.

It covers:

- What each attribute does
- Where it can be applied
- How reflection and source-generated serializers interpret it
- Precedence and interaction rules
- Valid examples that match the current public API

## Table of Contents

1. Quick reference
2. Execution order and precedence
3. Contract shape attributes
4. Defaulting and validation attributes
5. Conversion and transformation attributes
6. Versioning attributes
7. Construction attributes
8. Polymorphism attributes
9. Preprocessor and external reference attributes
10. Lifecycle attributes
11. Source generator diagnostics
12. Complete example

## Quick Reference

| Attribute | Target | Purpose |
|---|---|---|
| `BJsonSerializable` | class, struct | Enables attribute-based CLR serialization and source generation |
| `BJsonConverter` | class, struct, property, field | Applies a specific converter type |
| `BJsonConverterFactory` | class, struct, property, field | Applies a converter factory |
| `BJsonProperty` | property, field | Sets name, order, and requiredness together |
| `BJsonPropertyName` | property, field | Sets the exact JSON key |
| `BJsonInclude` | property, field | Forces inclusion of a member that would otherwise be excluded |
| `BJsonRequired` | property, field | Makes a member always required during deserialization |
| `BJsonRequiredWhen` | property, field | Makes a member conditionally required |
| `BJsonIgnore` | property, field | Statically ignores a member depending on the configured condition |
| `BJsonIgnoreWhen` | property, field | Dynamically ignores a member through a static predicate |
| `BJsonExtensionData` | property, field | Captures unknown JSON keys into a dictionary |
| `BJsonValueMapper` | property, field | Transforms the `BJsonValue` representation during read and write |
| `BJsonDefaultValue` | property, field | Applies a compile-time constant default |
| `BJsonDefaultProvider` | property, field | Applies a default produced by a static method |
| `BJsonNumberHandling` | property, field | Controls numeric string interoperability and lossless writing |
| `BJsonVersionContext` | class, struct | Declares the current document version for the type |
| `BJsonVersion` | property, field, class, struct | Controls version ranges and legacy key migration |
| `BJsonAlias` | property, field | Accepts additional legacy read-time JSON names |
| `BJsonConstructor` | constructor | Selects the constructor used for deserialization |
| `BJsonFactoryMethod` | method | Selects a static factory for deserialization |
| `BJsonPolymorphic` | class, interface | Enables discriminator-based polymorphism |
| `BJsonDerivedType` | class | Registers allowed derived types |
| `BJsonDiscriminatorValue` | class, struct | Declares the discriminator token for a concrete type |
| `BJsonPreprocessor` | class, struct | Enables DOM preprocessing before typed binding |
| `BJsonAnchor` | property, field | Registers a named anchor for `$ref` replacement |
| `BJsonExternalRef` | property, field | Loads or writes a member value from/to an external BJson file |
| `BJsonOnSerializing` | method | Runs an instance hook before serialization |
| `BJsonOnDeserialized` | method | Runs an instance hook after deserialization |

## Execution Order and Precedence

### Serialization pipeline

When BinJson serializes an attributed CLR object, the effective flow is:

1. Runtime type resolution for polymorphic values.
2. Type-level version filtering.
3. Member-level ignore conditions from `BJsonIgnore`.
4. Dynamic ignore predicates from `BJsonIgnoreWhen`.
5. External-reference write handling from `BJsonExternalRef`.
6. Member conversion through `BJsonConverter` or `BJsonConverterFactory` when present.
7. Normal serialization when no custom converter applies.
8. `BJsonValueMapper` write transformation.
9. Numeric write adaptation from `BJsonNumberHandling`.
10. Lifecycle hook invocation from `BJsonOnSerializing` occurs before member emission starts.

### Deserialization pipeline

When BinJson deserializes into an attributed CLR type, the effective flow is:

1. Polymorphic discriminator resolution.
2. DOM preprocessing from `BJsonPreprocessor`.
3. Anchor replacement from `BJsonAnchor` and external reference loading from `BJsonExternalRef`.
4. Type-level and member-level version filtering.
5. Key lookup using current name, `RenamedFrom`, and `BJsonAlias` values.
6. Defaulting from `BJsonDefaultProvider` or `BJsonDefaultValue`.
7. Requiredness checks from `BJsonRequired` and `BJsonRequiredWhen`.
8. `BJsonIgnoreWhen` read-time predicate checks.
9. `BJsonValueMapper` read transformation.
10. Numeric string parsing from `BJsonNumberHandling`.
11. Member conversion through `BJsonConverter` or `BJsonConverterFactory` when present.
12. Constructor or factory materialization.
13. Lifecycle hook invocation from `BJsonOnDeserialized`.

### General precedence rules

- `BJsonPropertyName` wins over `BJsonProperty.Name`.
- `BJsonDefaultProvider` wins over `BJsonDefaultValue` when both are present.
- `BJsonFactoryMethod` takes precedence over `BJsonConstructor`.
- `BJsonIgnore` is evaluated before `BJsonIgnoreWhen`.
- `BJsonSerializerOptions.Version` overrides `BJsonVersionContext` at runtime.
- Source-generated serializers require referenced helper methods to be callable from generated code, typically `internal`, `public`, or `protected internal`.
- Reflection-based deserialization can use private factory methods; generated serializers cannot.

## Contract Shape Attributes

### `BJsonSerializable`

Use this on classes and structs that should participate in attribute-based CLR serialization.

```csharp
[BJsonSerializable(NamingPolicy = NamingPolicy.CamelCase)]
public sealed class PlayerProfile
{
    public string PlayerName { get; set; } = string.Empty;
    public int Level { get; set; }
}
```

Key properties:

- `IncludeFields`: include fields in addition to properties.
- `IncludePrivateMembers`: allow non-public members to participate.
- `NamingPolicy`: `Default`, `CamelCase`, `SnakeCase`, or `KebabCase`.

Use `BJsonInclude` when only a few non-public members should be included.

### `BJsonProperty`

Use this when you want to configure several contract options on one member.

```csharp
[BJsonProperty(Name = "player_name", Order = 0, Required = true)]
public string Name { get; set; } = string.Empty;
```

Rules:

- `Name` changes the JSON key unless `BJsonPropertyName` is also present.
- `Order` controls write order only.
- `Required = true` behaves like `BJsonRequired`.

### `BJsonPropertyName`

Use this when all you need is a stable explicit wire name.

```csharp
[BJsonPropertyName("player_level")]
public int Level { get; set; }
```

This is the strongest name override for the member.

### `BJsonInclude`

Use this to include a member that would otherwise be excluded by visibility.

```csharp
[BJsonSerializable]
public sealed class SessionState
{
    [BJsonInclude]
    internal string Token { get; set; } = string.Empty;
}
```

### `BJsonExtensionData`

Use this to preserve unknown keys for forward compatibility.

```csharp
[BJsonSerializable]
public sealed class ConfigDocument
{
    public string Name { get; set; } = string.Empty;

    [BJsonExtensionData]
    public Dictionary<string, BJsonValue>? ExtraData { get; set; }
}
```

Rules:

- Only one extension-data member is allowed per type.
- The member must be compatible with `IDictionary<string, BJsonValue>`.
- Unknown keys are re-emitted during serialization.

## Defaulting and Validation Attributes

### `BJsonRequired`

Use this when a member must always be present.

```csharp
[BJsonRequired]
public string UserId { get; set; } = string.Empty;
```

In strict mode, missing data throws during deserialization.

### `BJsonRequiredWhen`

Use this when requiredness depends on version or context.

```csharp
[BJsonRequiredWhen(nameof(IsNameRequired))]
public string? Name { get; set; }

internal static bool IsNameRequired(string memberName, IComparable? version)
    => version is Version semantic && semantic >= new Version(2, 0, 0);
```

Accepted method signatures:

```csharp
static bool Method()
static bool Method(IComparable? version)
static bool Method(string memberName, IComparable? version)
```

### `BJsonIgnore`

Use this for fixed ignore behavior.

```csharp
[BJsonIgnore]
public string DebugInfo { get; set; } = string.Empty;

[BJsonIgnore(Condition = BJsonIgnoreCondition.WhenWritingNull)]
public string? Alias { get; set; }

[BJsonIgnore(Condition = BJsonIgnoreCondition.WhenWritingCustomDefault)]
public int Count { get; set; }
```

Supported conditions:

- `Always`
- `Never`
- `WhenWritingNull`
- `WhenWritingDefault`
- `WhenWritingCustomDefault`
- `WhenWriting`
- `WhenReading`

`WhenWritingCustomDefault` compares against the provider-supplied default when `BJsonDefaultProvider` is present.

### `BJsonIgnoreWhen`

Use this when ignore behavior depends on the current value or version.

```csharp
[BJsonIgnoreWhen(nameof(ShouldIgnoreScore))]
public int Score { get; set; }

internal static bool ShouldIgnoreScore(object? value, string propertyName, IComparable? version)
    => version is Version semantic
       && semantic >= new Version(2, 0, 0)
       && value is int score
       && score == 0;
```

Accepted signature:

```csharp
static bool Method(object? value, string propertyName, IComparable? version)
```

### `BJsonDefaultValue`

Use this for compile-time constant defaults.

```csharp
[BJsonDefaultValue(1)]
public int Level { get; set; }

[BJsonDefaultValue("guest")]
public string Role { get; set; } = string.Empty;
```

This applies when the key is missing and also when a non-nullable value member receives an explicit `null` token.

### `BJsonDefaultProvider`

Use this for computed or object defaults.

```csharp
[BJsonDefaultProvider(nameof(GetDefaultInventory))]
public Inventory Inventory { get; set; } = new Inventory();

internal static Inventory GetDefaultInventory()
    => new Inventory { Gold = 100 };
```

Version-aware defaults are supported:

```csharp
[BJsonDefaultProvider(nameof(GetModeDefault))]
public string Mode { get; set; } = string.Empty;

internal static string GetModeDefault(IComparable? version)
    => version is Version semantic && semantic >= new Version(3, 0, 0)
       ? "modern"
       : "legacy";
```

Accepted signatures:

```csharp
static T Method()
static object? Method()
static T Method(IComparable? version)
static object? Method(IComparable? version)
```

## Conversion and Transformation Attributes

### `BJsonConverter`

Use this when one fixed converter type should own the mapping.

```csharp
public sealed class DateOnlyStringConverter : BJsonConverter<DateTime>
{
    public override BJsonValue Serialize(DateTime value, BJsonSerializationContext context)
        => BJsonValue.Create(value.ToString("yyyy-MM-dd"));

    public override DateTime Deserialize(BJsonValue value, BJsonSerializationContext context)
        => DateTime.Parse(value.StringValue);
}

[BJsonSerializable]
public sealed class AuditEntry
{
    [BJsonConverter(typeof(DateOnlyStringConverter))]
    public DateTime CreatedAt { get; set; }
}
```

### `BJsonConverterFactory`

Use this when conversion depends on a closed generic or runtime type shape.

```csharp
[BJsonSerializable]
public sealed class Envelope
{
    [BJsonConverterFactory(typeof(WrappedConverterFactory))]
    public Wrapped<int> Count { get; set; }
}
```

This is appropriate for patterns like `Wrapped<T>`, `Optional<T>`, or other generic wrappers.

### `BJsonValueMapper`

Use this when you want to transform the serialized `BJsonValue` representation rather than replace the entire converter.

```csharp
[BJsonValueMapper(nameof(MapHealth))]
public int Health { get; set; }

internal static BJsonValue MapHealth(BJsonValue value, string propertyName, IComparable? version, bool isReading)
{
    if (isReading && version is Version semantic && semantic < new Version(2, 0, 0))
        return BJsonValue.Create(value.IntValue * 10);

    return value;
}
```

Accepted signatures:

```csharp
static BJsonValue Method(BJsonValue value, string propertyName, IComparable? version, bool isReading)
static BJsonValue Method(BJsonValue value)
```

Source-generated serializers require the full 4-parameter signature.

### `BJsonNumberHandling`

Use this when a numeric wire contract uses strings or requires a stable textual representation.

```csharp
[BJsonNumberHandling(BJsonNumberHandling.AllowReadingFromString)]
public int Count { get; set; }

[BJsonNumberHandling(BJsonNumberHandling.AllowReadingFromString | BJsonNumberHandling.Lossless)]
public decimal Amount { get; set; }
```

Meaning:

- `AllowReadingFromString`: parse numeric strings during deserialization.
- `WriteAsString`: emit numbers as strings during serialization.
- `Lossless`: keep a string representation suitable for precise round-tripping.

## Versioning Attributes

### `BJsonVersionContext`

Use this on a type to declare the default document version context.

```csharp
[BJsonSerializable]
[BJsonVersionContext(typeof(Version), "2.1.0")]
public sealed class SaveFile
{
    public string Name { get; set; } = string.Empty;
}
```

This version is passed to:

- `BJsonVersion`
- `BJsonIgnoreWhen`
- `BJsonRequiredWhen`
- `BJsonValueMapper`
- `BJsonDefaultProvider` version-aware overloads

### `BJsonVersion`

Use this to gate members or whole types by version.

```csharp
[BJsonVersion(typeof(Version), introducedIn: "2.0.0")]
public sealed class ModernOnlyPayload
{
    public int Count { get; set; }
}

[BJsonVersion(typeof(Version), introducedIn: "1.5.0")]
public int Level { get; set; }

[BJsonVersion(typeof(Version), introducedIn: "2.0.0", RenamedFrom = "score")]
public int TotalScore { get; set; }
```

Rules:

- `introducedIn` is inclusive.
- `removedIn` is exclusive.
- When no version context is active, version constraints are ignored.
- `RenamedFrom` provides one legacy read-time key.

### `BJsonAlias`

Use this for additional legacy keys beyond `RenamedFrom`.

```csharp
[BJsonAlias("legacy_count")]
[BJsonAlias("legacy_count_v2")]
public int Count { get; set; }
```

Serialization still emits the current configured key only.

## Construction Attributes

### `BJsonConstructor`

Use this when object creation must go through a specific constructor.

```csharp
[BJsonSerializable]
public sealed class Coordinate
{
    [BJsonConstructor]
    public Coordinate(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }
    public double Y { get; }
}
```

Parameter names are matched to configured serialized names.

### `BJsonFactoryMethod`

Use this when materialization must go through a static factory instead of a constructor.

```csharp
[BJsonSerializable]
public sealed class Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public string Currency { get; }

    [BJsonFactoryMethod]
    internal static Money Create(decimal amount, string currency) => new(amount, currency);
}
```

Explicit parameter mapping is supported:

```csharp
[BJsonFactoryMethod(ParameterMapping = new[] { "x", "coord_x", "y", "coord_y" })]
internal static Point Create(int x, int y) => new Point(x, y);
```

Rules:

- The method must be static.
- The return type must be the declaring type or a subtype.
- Multiple factory methods are invalid.
- Reflection can discover private factory methods; generated serializers require callable visibility.

## Polymorphism Attributes

### `BJsonPolymorphic`

Use this on a base class or interface to enable discriminator-based round trips.

```csharp
[BJsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[BJsonDerivedType(typeof(Mage), TypeDiscriminator = "mage")]
[BJsonDerivedType(typeof(Warrior), TypeDiscriminator = "warrior")]
public abstract class Character
{
    public string Name { get; set; } = string.Empty;
}
```

### `BJsonDerivedType`

Use this on the base type once per supported subtype.

```csharp
[BJsonDerivedType(typeof(SwordItem), TypeDiscriminator = "sword")]
[BJsonDerivedType(typeof(BowItem), TypeDiscriminator = "bow")]
public abstract class Item
{
}
```

### `BJsonDiscriminatorValue`

Use this on the concrete type when you want the type to carry its own discriminator token.

```csharp
[BJsonDiscriminatorValue("mage")]
public sealed class Mage : Character
{
    public int Mana { get; set; }
}
```

This works well with `BJsonPolymorphic` and `BJsonDerivedType`.

## Preprocessor and External Reference Attributes

### `BJsonPreprocessor`

Use this to enable raw DOM preprocessing before typed binding.

```csharp
[BJsonSerializable]
[BJsonPreprocessor]
public sealed class ThemeConfig
{
    public string PrimaryColor { get; set; } = string.Empty;
}
```

Supported built-in capabilities:

- Variable substitution in strings
- Conditional branch selection
- Anchor registration and `$ref` replacement
- External file inclusion

Custom preprocessors can be plugged in:

```csharp
[BJsonSerializable]
[BJsonPreprocessor(PreprocessorType = typeof(MyPreprocessor))]
public sealed class ThemeConfig
{
}
```

### `BJsonAnchor`

Use this when one member should become a named anchor visible to `$ref` nodes.

```csharp
[BJsonSerializable]
[BJsonPreprocessor]
public sealed class Theme
{
    [BJsonAnchor("primaryColor")]
    public string PrimaryColor { get; set; } = "#22CC88";

    public string Display { get; set; } = string.Empty;
}
```

Input payload example:

```json
{
  "PrimaryColor": "#22CC88",
  "Display": { "$ref": "primaryColor" }
}
```

After preprocessing, `Display` receives the same value as `PrimaryColor`.

### `BJsonExternalRef`

Use this when a member payload lives in a separate BJson file.

```csharp
[BJsonExternalRef]
public LevelData? Level { get; set; }

[BJsonExternalRef(FixedPath = "data/inventory.bjson")]
public Inventory? Inventory { get; set; }

[BJsonExternalRef(Optional = true)]
public Settings? Settings { get; set; }
```

Behavior:

- On read, the external file is loaded and deserialized into the member.
- On write, the member payload is written to the target file and the current document stores a string path token.
- `Optional = true` suppresses missing-file failures and yields `null` or default member value semantics instead.

Path policy:

```csharp
var options = new BJsonSerializerOptions
{
    PreprocessorContext = new BJsonPreprocessorContext { BasePath = basePath },
    ExternalReferencePathPolicy = ExternalReferencePathPolicy.RestrictToBasePath
};
```

### Built-in conditional syntax

The built-in preprocessor supports branch arrays through `$branches`.

```json
{
  "$branches": [
    {
      "$if": { "$var": "Platform", "$eq": "Desktop" },
      "$then": {
        "Mode": "high",
        "DisplayColor": "#22CC88"
      }
    },
    {
      "$else": {
        "Mode": "safe",
        "DisplayColor": "#999999"
      }
    }
  ]
}
```

Variables are supplied through `BJsonPreprocessorContext` before deserialization.

## Lifecycle Attributes

### `BJsonOnSerializing`

Use this when the instance must normalize itself before write.

```csharp
[BJsonOnSerializing]
internal void PrepareForWrite()
{
    UpdatedAt = DateTime.UtcNow;
}
```

Accepted signatures:

```csharp
void Method()
void Method(BJsonSerializationContext context)
```

### `BJsonOnDeserialized`

Use this when the instance must repair invariants or populate caches after read.

```csharp
[BJsonOnDeserialized]
internal void RebuildCache()
{
    CacheKey = Name.ToLowerInvariant();
}
```

Accepted signatures:

```csharp
void Method()
void Method(BJsonDeserializationContext context)
```

## Source Generator Diagnostics

| ID | Severity | Meaning |
|---|---|---|
| `BJSON001` | Warning | Invalid `BJsonExtensionData` member type |
| `BJSON002` | Error | Multiple constructors marked with `BJsonConstructor` |
| `BJSON003` | Error | Multiple members marked with `BJsonExtensionData` |
| `BJSON004` | Warning | Converter type referenced by `BJsonConverter` not found |
| `BJSON005` | Warning | Conflicting effective JSON property names |
| `BJSON006` | Warning | Constructor or factory parameter cannot be matched to a member |
| `BJSON007` | Warning | Referenced helper method not found |
| `BJSON008` | Warning | Referenced helper method is not accessible from generated code |
| `BJSON009` | Warning | Referenced helper method has an invalid signature |
| `BJSON010` | Warning | Unsupported type shape for source generation |
| `BJSON012` | Warning | Invalid `BJsonFactoryMethod.ParameterMapping` declaration |
| `BJSON013` | Warning | `BJsonDefaultValue` and `BJsonDefaultProvider` declared together |
| `BJSON014` | Error | Multiple methods marked with `BJsonFactoryMethod` |
| `BJSON015` | Error | Invalid `BJsonFactoryMethod` signature |
| `BJSON016` | Warning | Invalid factory parameter mapping target |

## Complete Example

```csharp
using System;
using System.Collections.Generic;
using Krampus.BinJson;
using Krampus.BinJson.Serialization;

[BJsonSerializable(NamingPolicy = NamingPolicy.SnakeCase)]
[BJsonVersionContext(typeof(Version), "3.0.0")]
[BJsonPreprocessor]
public sealed class CharacterSave
{
    [BJsonRequired]
    public string Name { get; set; } = string.Empty;

    [BJsonVersion(typeof(Version), introducedIn: "2.0.0", RenamedFrom = "score")]
    [BJsonDefaultValue(0)]
    public int TotalScore { get; set; }

    [BJsonAlias("legacy_guild")]
    public string? Guild { get; set; }

    [BJsonIgnoreWhen(nameof(ShouldIgnoreAuditTrail))]
    public string AuditTrail { get; set; } = string.Empty;

    [BJsonValueMapper(nameof(MapHealth))]
    public int Health { get; set; }

    [BJsonDefaultProvider(nameof(GetDefaultLoadout))]
    public Loadout Loadout { get; set; } = new Loadout();

    [BJsonAnchor("primary_name")]
    public string DisplayName => Name;

    [BJsonExternalRef(Optional = true)]
    public CharacterState? ExternalState { get; set; }

    [BJsonExtensionData]
    public Dictionary<string, BJsonValue>? ExtraData { get; set; }

    internal static bool ShouldIgnoreAuditTrail(object? value, string propertyName, IComparable? version)
        => value is string text && string.IsNullOrWhiteSpace(text);

    internal static BJsonValue MapHealth(BJsonValue value, string propertyName, IComparable? version, bool isReading)
    {
        if (isReading && version is Version semantic && semantic < new Version(2, 0, 0))
            return BJsonValue.Create(value.IntValue * 10);

        return value;
    }

    internal static Loadout GetDefaultLoadout(IComparable? version)
        => new Loadout { Weapon = "Sword", Armor = "Leather" };

    [BJsonOnDeserialized]
    internal void NormalizeAfterRead()
    {
        Guild ??= "none";
    }
}

[BJsonSerializable]
public sealed class Loadout
{
    public string Weapon { get; set; } = string.Empty;
    public string Armor { get; set; } = string.Empty;
}

[BJsonSerializable]
public sealed class CharacterState
{
    public int Power { get; set; }
    public string Flags { get; set; } = string.Empty;
}
```

Related guides:

- `docs/Extensibility.md`
- `docs/CompatibilityNotes.md`
- `docs/MigrationRecipes.md`
- `docs/ErrorHandling.md`
