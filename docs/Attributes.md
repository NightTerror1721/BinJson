# BJson Attribute System — Reference & Tutorial

This document is the complete reference for every attribute in `Krampus.BinJson.Serialization`.
It covers both the original set and all extensions introduced in the extended attribute system.

---

## Table of Contents

1. [Quick Reference](#quick-reference)
2. [Getting Started](#getting-started)
3. [Member Control](#member-control)
   - [BJsonProperty](#bjsonproperty)
   - [BJsonPropertyName](#bjsonpropertyname)
   - [BJsonRequired](#bjsonrequired)
   - [BJsonInclude](#bjsoninclude)
   - [BJsonIgnore](#bjsonignore)
   - [BJsonIgnoreWhen](#bjsonignorewhen)
   - [BJsonExtensionData](#bjsonextensiondata)
   - [BJsonConverter (member)](#bjsonconverter-member)
4. [Value Transformation](#value-transformation)
   - [BJsonValueMapper](#bjsonvaluemapper)
5. [Default Values](#default-values)
   - [BJsonDefaultValue](#bjsondefaultvalue)
   - [BJsonDefaultProvider](#bjsondefaultprovider)
   - [Priority Rules](#priority-rules)
6. [Version System](#version-system)
   - [BJsonVersionContext](#bjsonversioncontext)
   - [BJsonVersion](#bjsonversion)
   - [RenamedFrom — Key Migration](#renamedfrom--key-migration)
   - [Version Flow to Methods](#version-flow-to-methods)
7. [Instantiation](#instantiation)
   - [BJsonConstructor](#bjsonconstructor)
   - [BJsonFactoryMethod](#bjsonfactorymethod)
8. [Type-Level Control](#type-level-control)
   - [BJsonSerializable](#bjsonserializable)
   - [BJsonConverter (type)](#bjsonconverter-type)
   - [BJsonPolymorphic](#bjsonpolymorphic)
   - [BJsonDerivedType](#bjsonderivedtype)
9. [DOM Preprocessing](#dom-preprocessing)
   - [BJsonPreprocessor](#bjsonpreprocessor)
   - [BJsonAnchor](#bjsonanchor)
   - [BJsonExternalRef](#bjsonexternalref)
   - [Conditional Blocks Syntax](#conditional-blocks-syntax)
10. [Source Generator Diagnostics](#source-generator-diagnostics)
11. [Complete Worked Example](#complete-worked-example)

---

## Quick Reference

| Attribute | Target | Purpose |
|---|---|---|
| `[BJsonSerializable]` | class / struct | Marks type for source generation; sets `IncludeFields`, `IncludePrivateMembers`, `NamingPolicy` |
| `[BJsonProperty]` | property / field | Sets `Name`, `Order`, `Required` |
| `[BJsonPropertyName]` | property / field | Overrides JSON key name |
| `[BJsonRequired]` | property / field | Throws if key is absent during deserialization |
| `[BJsonInclude]` | property / field | Forces inclusion of a non-public member |
| `[BJsonIgnore]` | property / field | Static ignore condition (`Always`, `WhenWritingNull`, etc.) |
| `[BJsonIgnoreWhen]` | property / field | Dynamic ignore via a static predicate method on the same type |
| `[BJsonValueMapper]` | property / field | Transform value via a static mapper method on the same type |
| `[BJsonDefaultValue]` | property / field | Constant default applied when JSON key is absent |
| `[BJsonDefaultProvider]` | property / field | Static method provides default when JSON key is absent |
| `[BJsonVersion]` | property / field / class / struct | Version range (introducedIn / removedIn) and RenamedFrom |
| `[BJsonVersionContext]` | class / struct | Declares the current document version for the type |
| `[BJsonExtensionData]` | property / field | Collects unknown JSON keys into `IDictionary<string, BJsonValue>` |
| `[BJsonConverter]` | class / struct / property / field | Custom converter for type or member |
| `[BJsonConstructor]` | constructor | Designates deserialization constructor |
| `[BJsonFactoryMethod]` | static method | Designates static factory for deserialization (supersedes `[BJsonConstructor]`) |
| `[BJsonPolymorphic]` | class / interface | Enables polymorphic serialization with a type discriminator |
| `[BJsonDerivedType]` | class | Registers a derived type and its discriminator value |
| `[BJsonPreprocessor]` | class / struct | Enables DOM pre-processing (conditionals, anchors, variables) |
| `[BJsonAnchor]` | property / field | Registers member value as a named anchor for `$ref` resolution |
| `[BJsonExternalRef]` | property / field | Member value is loaded from / written to an external BJson file |

---

## Getting Started

Add `using Krampus.BinJson.Serialization;` and mark your type with `[BJsonSerializable]`:

```csharp
using Krampus.BinJson;
using Krampus.BinJson.Serialization;

[BJsonSerializable]
public class PlayerSave
{
	public string Name { get; set; } = string.Empty;
	public int Level { get; set; }
}

// Serialize
BJsonValue bson = BJson.Serialize(new PlayerSave { Name = "Hero", Level = 10 });

// Deserialize
PlayerSave save = BJson.Deserialize<PlayerSave>(bson)!;
```

The source generator automatically emits a zero-reflection serializer for the type.

### Important: Helper Method Visibility

When using attributes that reference static methods (`[BJsonIgnoreWhen]`, `[BJsonValueMapper]`, `[BJsonDefaultProvider]`, `[BJsonFactoryMethod]`), those methods **must be `internal` or `public`** — they cannot be `private`.

**Why:** The source generator emits a separate serializer class (e.g., `PlayerSave_BJsonSerializer`) in the same assembly and namespace. This class needs to call your helper methods directly, so `private` visibility will cause compilation errors.

**Recommendation:** Use `internal` for helper methods to keep them hidden from external assemblies while allowing the generated serializer to access them.

---

## Member Control

### BJsonProperty

Customises how a member participates in serialization.

```csharp
public class Item
{
	[BJsonProperty(Name = "item_name", Order = 0, Required = true)]
	public string Name { get; set; } = string.Empty;

	[BJsonProperty(Order = 1)]
	public int Quantity { get; set; }
}
```

| Property | Type | Description |
|---|---|---|
| `Name` | `string?` | Override JSON key name |
| `Order` | `int` | Controls serialization order (ascending). Default `0`. |
| `Required` | `bool` | Equivalent to placing `[BJsonRequired]` on the member |

> **Tip:** `[BJsonPropertyName]` takes precedence over `BJsonProperty.Name` when both are present.

---

### BJsonPropertyName

Directly sets the JSON key without the other options of `[BJsonProperty]`:

```csharp
[BJsonPropertyName("player_level")]
public int Level { get; set; }
```

---

### BJsonRequired

Throws `InvalidOperationException` during deserialization if the JSON key is absent (in strict mode):

```csharp
[BJsonRequired]
public string UserId { get; set; } = string.Empty;
```

---

### BJsonInclude

Forces a non-public member to be included even when `IncludePrivateMembers = false`:

```csharp
[BJsonSerializable]
public class Config
{
	[BJsonInclude]
	internal string InternalToken { get; set; } = string.Empty;
}
```

---

### BJsonIgnore

Excludes a member based on a static condition.

```csharp
// Always ignore
[BJsonIgnore]
public string DebugInfo { get; set; } = string.Empty;

// Ignore during serialization when value is null
[BJsonIgnore(Condition = BJsonIgnoreCondition.WhenWritingNull)]
public string? Alias { get; set; }

// Ignore during serialization when value equals CLR default
[BJsonIgnore(Condition = BJsonIgnoreCondition.WhenWritingDefault)]
public int Score { get; set; }
```

`BJsonIgnoreCondition` values:

| Value | Behaviour |
|---|---|
| `Never` (0) | Never ignore. Same as not placing the attribute. |
| `Always` (1) | Always ignore both on read and write. **Default when no condition is specified.** |
| `WhenWritingNull` (2) | Skip during serialization when the value is `null`. |
| `WhenWritingDefault` (3) | Skip during serialization when the value equals `default(T)`. |
| `WhenWritingCustomDefault` (4) | Like `WhenWritingDefault` but uses the custom default from `[BJsonDefaultProvider]` if present. |
| `WhenWriting` (5) | Skip only during serialization; member is still read during deserialization. |
| `WhenReading` (6) | Skip only during deserialization; member is still written during serialization. |

---

### BJsonIgnoreWhen

Provides dynamic ignore logic via a **static method on the same type**. This is evaluated at runtime (or emitted as a direct static call by the source generator).

**Method signature (required):**
```csharp
static bool MethodName(object? value, string propertyName, IComparable? version)
```

- `value` — the current member value (may be `null`).
- `propertyName` — the CLR member name.
- `version` — the active document version from `[BJsonVersionContext]`, or `null` if none.
- Returns `true` to **ignore** the member for the current operation.

**Visibility requirement:** The method must be `internal` or `public` (not `private`) because the source generator emits a separate serializer class in the same assembly.

```csharp
[BJsonSerializable]
[BJsonVersionContext(typeof(Version), "2.0.0")]
public class PlayerSave
{
	[BJsonIgnoreWhen(nameof(ShouldIgnoreScore))]
	public int Score { get; set; }

	internal static bool ShouldIgnoreScore(object? value, string propertyName, IComparable? version)
		=> version != null
		   && version.CompareTo(new Version("2.0.0")) >= 0
		   && (int)value! == 0;
}
```

`[BJsonIgnoreWhen]` is **orthogonal** to `[BJsonIgnore]` — both can appear on the same member.
`[BJsonIgnore]` is evaluated first; if it causes the member to be ignored, the predicate is not called.

---

### BJsonExtensionData

Designates a `Dictionary<string, BJsonValue>` (or `IDictionary<string, BJsonValue>`) member to collect all JSON keys that do not map to any declared member during deserialization.
During serialization, the dictionary entries are merged back into the output object.

```csharp
[BJsonSerializable]
public class FlexibleModel
{
	public string Name { get; set; } = string.Empty;

	[BJsonExtensionData]
	public Dictionary<string, BJsonValue>? Extra { get; set; }
}
```

Only one `[BJsonExtensionData]` member is allowed per type. Diagnostic `BJSON003` is emitted otherwise.

---

### BJsonConverter (member)

Applies a custom `BJsonConverter<T>` to a single member, overriding the type-level converter:

```csharp
[BJsonConverter(typeof(EpochMillisecondsConverter))]
public DateTime CreatedAt { get; set; }
```

---

## Value Transformation

### BJsonValueMapper

Transforms the member value through a **static method on the same type** both during serialization (before writing) and deserialization (after reading).

**Full signature (preferred):**
```csharp
static BJsonValue MethodName(BJsonValue value, string propertyName, IComparable? version, bool isReading)
```

**Fallback signature (no direction flag):**
```csharp
static BJsonValue MethodName(BJsonValue value)
```

- `value` — the BJsonValue being read or written.
- `propertyName` — the CLR member name.
- `version` — the active document version, or `null` if none.
- `isReading` — `true` during deserialization, `false` during serialization.
- Returns the transformed `BJsonValue`.

> Runtime reflection supports both signatures above. Source-generated serializers require the full signature with four parameters.

**Visibility requirement:** The method must be `internal` or `public` (not `private`) because the source generator emits a separate serializer class in the same assembly.

```csharp
[BJsonSerializable]
[BJsonVersionContext(typeof(Version), "2.0.0")]
public class LegacyData
{
	[BJsonValueMapper(nameof(MapScore))]
	public int Score { get; set; }

	internal static BJsonValue MapScore(BJsonValue value, string propertyName, IComparable? version, bool isReading)
	{
		// v1.x stored score * 10; normalise on read
		if (isReading && version != null && version.CompareTo(new Version("2.0.0")) < 0)
		{
			var scaledScore = value.IntValue;
			return BJsonValue.Create(scaledScore / 10);
		}
		return value;
	}
}
```

---

## Default Values

Both attributes act **only during deserialization**: when the JSON key is absent or its value is `null` on a non-nullable member, the default is applied instead of `default(T)`.

### BJsonDefaultValue

Accepts any compile-time constant: `bool`, numeric types, `char`, `string`, or an enum value cast to its underlying integer.

```csharp
[BJsonDefaultValue(1)]
public int Level { get; set; }

[BJsonDefaultValue("guest")]
public string Role { get; set; } = string.Empty;

[BJsonDefaultValue(true)]
public bool IsActive { get; set; }
```

### BJsonDefaultProvider

References a **static parameterless method** on the same type for complex or computed defaults:

**Visibility requirement:** The method must be `internal` or `public` (not `private`) because the source generator emits a separate serializer class in the same assembly.

```csharp
[BJsonDefaultProvider(nameof(GetDefaultInventory))]
public Inventory StartInventory { get; set; }

internal static Inventory GetDefaultInventory() => new Inventory { Gold = 100 };
```

**Method signatures accepted:**
```csharp
static T      MethodName()   // strongly typed — preferred
static object? MethodName()  // loosely typed — fallback
```

### Priority Rules

When multiple sources of default are present, they are resolved in this order:

1. `[BJsonDefaultProvider]` — always wins.
2. `[BJsonDefaultValue]` — used when no provider is present.
3. CLR `default(T)` — used when neither attribute is present.

If both `[BJsonDefaultValue]` and `[BJsonDefaultProvider]` appear on the same member, the source generator emits a warning and the provider takes effect.

### Composing with Version System

A common pattern for fields added in newer versions:

```csharp
[BJsonVersion(typeof(Version), introducedIn: "2.0.0")]
[BJsonDefaultValue(0)]
public int NewField { get; set; }
// Documents from v1.x that lack this key will receive 0 during deserialization.
```

---

## Version System

The version system lets you control which members are active for a given document version, handle renamed keys, and pass version information to predicate and mapper methods.

### BJsonVersionContext

Declares the current document format version for the **entire type**. Applied at the type level.

```csharp
[BJsonSerializable]
[BJsonVersionContext(typeof(Version), "2.1.0")]
public class SaveFile { ... }
```

The version type must implement `IComparable` and expose a static `Parse(string)` method.
The value set here can be overridden per-call via `BJsonSerializerOptions.Version`.

### BJsonVersion

Controls inclusion of individual members based on a version range. Applied at member or type level.

```csharp
[BJsonSerializable]
[BJsonVersionContext(typeof(Version), "2.1.0")]
public class PlayerStats
{
	// Always present
	public string Name { get; set; } = string.Empty;

	// Introduced in v1.5 — absent for documents older than 1.5
	[BJsonVersion(typeof(Version), introducedIn: "1.5.0")]
	public int Level { get; set; }

	// Existed from v1.0 to v2.0, removed after
	[BJsonVersion(typeof(Version), introducedIn: "1.0.0", removedIn: "2.0.0")]
	public int LegacyRank { get; set; }
}
```

| Constructor parameter | Description |
|---|---|
| `versionType` | Concrete version type. Must implement `IComparable` + have `static Parse(string)`. |
| `introducedIn` | First version this member appears in. `null` = always present. |
| `removedIn` | First version this member is **absent** in (exclusive upper bound). `null` = never removed. |

A member is included when: `introducedIn <= currentVersion < removedIn`.
If no version context is active, version constraints are ignored and all members participate.

### RenamedFrom — Key Migration

Use `RenamedFrom` to support reading old JSON keys during deserialization after a member is renamed:

```csharp
// In v2.0, "score" was renamed to "totalScore"
[BJsonVersion(typeof(Version), introducedIn: "2.0.0", RenamedFrom = "score")]
public int TotalScore { get; set; }
```

During deserialization, the engine tries `"totalScore"` first, then falls back to `"score"`.

### Version Flow to Methods

The active `IComparable?` version is automatically passed to:
- Static predicate methods referenced by `[BJsonIgnoreWhen]`
- Static mapper methods referenced by `[BJsonValueMapper]`
- *(Available for future use in `[BJsonDefaultProvider]` methods if a version-aware overload is added)*

This allows a single method to implement version-conditional logic without additional configuration.

---

## Instantiation

### BJsonConstructor

Designates which constructor to use during deserialization. Constructor parameters are matched to JSON properties by name (case-insensitive, respecting `NamingPolicy`).

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

Only one constructor may carry `[BJsonConstructor]`. Diagnostic `BJSON002` is emitted otherwise.

### BJsonFactoryMethod

Designates a **static method** as the deserialization factory, superseding `[BJsonConstructor]` when both are present.

The method must be:
- `static`
- Return the declaring type (or a subtype)
- Have parameters whose names match JSON properties (case-insensitive, respecting `NamingPolicy`)

**Visibility requirement:** The method must be `internal` or `public` (not `private`) because the source generator emits a separate serializer class in the same assembly.

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

**Parameter matching:**
- Factory method parameters are matched to JSON properties by name (case-insensitive)
- The matching respects `[BJsonPropertyName]` and `NamingPolicy` settings
- If a parameter cannot be matched to a member, it reads directly from the JSON key with the parameter name
- Parameters are extracted from JSON and passed to the factory method in order

**Parameterless factory methods:**
```csharp
[BJsonFactoryMethod]
internal static Config CreateDefault() => new Config { Port = 8080 };
```

Only one method per type may carry `[BJsonFactoryMethod]`.

---

## Type-Level Control

### BJsonSerializable

Marks a type for BJson source generation and/or reflection-based serialization.

```csharp
[BJsonSerializable(
	IncludeFields = false,           // include public fields (default: false)
	IncludePrivateMembers = false,   // include non-public members (default: false)
	NamingPolicy = NamingPolicy.CamelCase)]
public class MyModel { ... }
```

`NamingPolicy` values: `Default`, `CamelCase`, `SnakeCase`, `KebabCase`.

### BJsonConverter (type)

Applies a custom `BJsonConverter<T>` to an entire type, used for all serialization of that type:

```csharp
[BJsonConverter(typeof(ColorHexConverter))]
public struct Color { ... }
```

### BJsonPolymorphic

Enables polymorphic serialization. A type discriminator property (`$type` by default) is written
to identify the concrete type during serialization and read back during deserialization.

```csharp
[BJsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[BJsonDerivedType(typeof(Dog), "dog")]
[BJsonDerivedType(typeof(Cat), "cat")]
public abstract class Animal
{
	public string Name { get; set; } = string.Empty;
}
```

### BJsonDerivedType

Registers a derived type and its discriminator value. Applied to the base type, repeatable.

```csharp
[BJsonDerivedType(typeof(Dog))]            // uses type name as discriminator
[BJsonDerivedType(typeof(Cat), "cat")]     // explicit discriminator value
```

| Constructor parameter | Description |
|---|---|
| `derivedType` | The concrete derived type. |
| `TypeDiscriminator` (property) | Optional explicit discriminator string. Uses type name if null. |

---

## DOM Preprocessing

The DOM preprocessor pipeline transforms the raw BJson DOM node **before** typed deserialization occurs. It enables conditional blocks, anchor resolution, variable substitution, and external file inclusion.

### BJsonPreprocessor

Opts the type into DOM preprocessing. When applied without `PreprocessorType`, the built-in preprocessor is used.

```csharp
[BJsonSerializable]
[BJsonPreprocessor]
public class AppConfig { ... }

// Custom preprocessor
[BJsonSerializable]
[BJsonPreprocessor(PreprocessorType = typeof(MyPreprocessor))]
public class AppConfig { ... }
```

A custom preprocessor must implement `IBJsonPreprocessor`:

```csharp
public class MyPreprocessor : IBJsonPreprocessor
{
	public object Process(object node, IBJsonPreprocessorContext context)
	{
		context.SetVariable("env", "production");
		// transform node...
		return node;
	}
}
```

### BJsonAnchor

Registers a member's value as a named anchor accessible anywhere in the same document via `{ "$ref": "anchorName" }`. Requires `[BJsonPreprocessor]` on the type.

```csharp
[BJsonSerializable]
[BJsonPreprocessor]
public class Theme
{
	[BJsonAnchor("primaryColor")]
	public string Primary { get; set; } = "#0078D4";

	// Other members in the JSON document can reference this value:
	// "background": { "$ref": "primaryColor" }
}
```

### BJsonExternalRef

Marks a member as a reference to an external BJson file.

```csharp
// Path resolved from the JSON string value at runtime
[BJsonExternalRef]
public LevelData? Level { get; set; }

// Fixed path relative to the document root
[BJsonExternalRef(FixedPath = "data/inventory.bjson")]
public Inventory? Inventory { get; set; }

// Missing file produces null instead of throwing
[BJsonExternalRef(Optional = true)]
public Settings? Settings { get; set; }
```

During **deserialization**, the referenced file is loaded and deserialized in place.
During **serialization**, the member is written to a separate file and replaced by a `{ "$ref": "path" }` token.

### Conditional Blocks Syntax

When `[BJsonPreprocessor]` is active, the built-in preprocessor supports `$if/$then/$elif/$else` blocks in the JSON document, resolved before typed deserialization:

```json
{
  "name": "Hero",
  "$if":   { "$var": "Platform", "$eq": "PC" },
  "$then": { "graphicsQuality": "Ultra" },
  "$elif": { "$var": "Platform", "$eq": "Mobile" },
  "$then": { "graphicsQuality": "Low" },
  "$else": { "graphicsQuality": "Medium" }
}
```

Variables are set programmatically via `IBJsonPreprocessorContext.SetVariable(name, value)` before deserialization starts.

---

## Source Generator Diagnostics

The source generator emits the following diagnostics at compile time:

| ID | Severity | Description |
|---|---|---|
| `BJSON001` | Warning | `[BJsonExtensionData]` member type is not `IDictionary<string, BJsonValue>` |
| `BJSON002` | Error | Multiple constructors carry `[BJsonConstructor]` |
| `BJSON003` | Error | Multiple members carry `[BJsonExtensionData]` |
| `BJSON004` | Warning | Custom converter type referenced by `[BJsonConverter]` was not found |
| `BJSON005` | Warning | Two or more members would produce the same JSON key |
| `BJSON006` | Warning | A constructor/factory parameter could not be matched to any member |
| `BJSON007` | Warning | Method referenced by `[BJsonIgnoreWhen]`, `[BJsonValueMapper]`, or `[BJsonDefaultProvider]` was not found |
| `BJSON008` | Warning | Referenced attribute method is not accessible from generated code (`public`, `internal`, or `protected internal` required) |
| `BJSON009` | Warning | Referenced attribute method has an invalid signature |
| `BJSON010` | Warning | Unsupported type shape for source generation (for example generic or nested type) |

---

## Complete Worked Example

The following example combines the version system, predicates, mappers, and default values in a realistic save-file type.

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
			return BJsonValue.Create(value.IntValue * 10); // percentage → raw HP
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
	public string Armor  { get; set; } = string.Empty;
}

// ── Polymorphic base ──────────────────────────────────────────────

[BJsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[BJsonDerivedType(typeof(SwordItem), "sword")]
[BJsonDerivedType(typeof(BowItem),   "bow")]
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
		Code   = code;
	}

	public decimal Amount { get; }
	public string  Code   { get; }

	[BJsonFactoryMethod]
	public static Currency Create(decimal amount, string code) => new(amount, code);
}
```

### Serialization

```csharp
var character = new CharacterSave
{
	Name      = "Aria",
	Level     = 42,
	TotalScore = 9800,
	Health    = 350,
	Loadout   = new Loadout { Weapon = "Longbow", Armor = "Chainmail" }
};

BJsonValue bson  = BJson.Serialize(character);
string     json  = BJson.Stringify(character);
byte[]     bytes = BJson.SerializeToBytes<CharacterSave>(character);
```

### Deserialization

```csharp
// From JSON text (v1.x legacy document)
string legacyJson = """{"name":"Aria","score":980,"health":35}""";
CharacterSave legacy = BJson.Parse<CharacterSave>(legacyJson)!;
// legacy.TotalScore == 980 (read from "score" via RenamedFrom)
// legacy.Health     == 350 (mapped: 35 * 10)

// From bytes
CharacterSave fromBytes = BJson.Deserialize<CharacterSave>(bytes)!;
```
