# Extensibility Guide

This guide explains how to use BinJson object serialization beyond the DOM layer.

## Overview

BinJson provides an extensible object serialization pipeline in `Krampus.BinJson.Serialization`.
You can combine:

- Interface-based serialization with `IBJsonSerializable` and `IBJsonDeserializable`
- Custom converters with `BJsonConverter<T>`
- Attribute-driven contracts with `[BJsonSerializable]` and related attributes
- Reference preservation with `$id` and `$ref`
- Optional source generators for attributed models

## I/O Extension Architecture Rule

When extending text or binary I/O behavior (readers/writers), follow the internal architecture described in [Architecture.md](Architecture.md):

- Keep sync wrappers and async wrappers separate.
- Do not call sync wrappers from async wrappers (or vice versa).
- Move shared protocol logic to common `*Core` classes.
- Keep lifecycle/ownership concerns (`leaveOpen`, option wiring, validation) in `*Base` classes.

This rule avoids hidden coupling and reduces regression risk when changing wire format logic.

## Basic Object Serialization

Use the `BJson` facade to serialize CLR objects into `BJsonValue` and deserialize them back:

```csharp
using Krampus.BinJson;
using Krampus.BinJson.Serialization;

var options = new BJsonSerializerOptions();
var value = BJson.Serialize(new PlayerProfile { Id = 7, Name = "mage" }, options);
var profile = BJson.Deserialize<PlayerProfile>(value, options);
```

## Interface-Based Contracts

Implement `IBJsonSerializable` when a type wants full control over serialization.
Implement `IBJsonDeserializable` when a type wants full control over deserialization.

```csharp
using Krampus.BinJson;
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

Use this approach when the payload shape does not match the public members of the type.

## Custom Converters

Create a converter by inheriting from `BJsonConverter<T>`:

```csharp
using Krampus.BinJson;
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

Register it through options:

```csharp
var options = new BJsonSerializerOptions();
options.AddConverter(new DateOnlyStringConverter());
```

You can also apply a converter with an attribute:

```csharp
[BJsonSerializable]
public sealed class AuditEntry
{
	[BJsonConverter(typeof(DateOnlyStringConverter))]
	public DateTime CreatedAt { get; set; }
}
```

## Attribute-Based Contracts

Mark a model with `[BJsonSerializable]` to enable attribute-driven reflection and source generation.

```csharp
using Krampus.BinJson.Serialization;

[BJsonSerializable(NamingPolicy = NamingPolicy.CamelCase)]
public sealed class PlayerProfile
{
	[BJsonPropertyName("identifier")]
	public int Id { get; set; }

	[BJsonRequired]
	public string Name { get; set; } = string.Empty;

	[BJsonIgnore(Condition = BJsonIgnoreCondition.WhenNull)]
	public string? Alias { get; set; }
}
```

Supported attributes include:

- `[BJsonSerializable]`
- `[BJsonProperty]`
- `[BJsonPropertyName]`
- `[BJsonInclude]`
- `[BJsonRequired]`
- `[BJsonIgnore]`
- `[BJsonExtensionData]`
- `[BJsonConstructor]`
- `[BJsonConverter]`
- `[BJsonPolymorphic]`
- `[BJsonDerivedType]`

## Parameterized Constructors

Use `[BJsonConstructor]` when the type should be materialized through a parameterized constructor:

```csharp
using Krampus.BinJson.Serialization;

[BJsonSerializable]
public sealed class CharacterSummary
{
	[BJsonConstructor]
	public CharacterSummary(string name, int level)
	{
		Name = name;
		Level = level;
	}

	public string Name { get; }
	public int Level { get; }
}
```

Constructor parameter names should match the configured serialized member names.

## Extension Data

Use `[BJsonExtensionData]` to keep unknown object members during deserialization and write them back later:

```csharp
using Krampus.BinJson;
using Krampus.BinJson.Serialization;

[BJsonSerializable]
public sealed class ConfigDocument
{
	public string Name { get; set; } = string.Empty;

	[BJsonExtensionData]
	public Dictionary<string, BJsonValue>? ExtraData { get; set; }
}
```

This is useful for forward compatibility and pass-through scenarios.

## Polymorphism

Use `[BJsonPolymorphic]` on a base type and `[BJsonDerivedType]` for allowed derived types:

```csharp
using Krampus.BinJson.Serialization;

[BJsonPolymorphic]
[BJsonDerivedType(typeof(Warrior))]
[BJsonDerivedType(typeof(Mage))]
public abstract class Character
{
	public string Name { get; set; } = string.Empty;
}

public sealed class Warrior : Character
{
	public int Armor { get; set; }
}

public sealed class Mage : Character
{
	public int Mana { get; set; }
}
```

BinJson writes a discriminator property named `$type` by default.
You can change that property name through `BJsonPolymorphicAttribute.TypeDiscriminatorPropertyName`.

## Reference Preservation

To preserve object identity and circular references, enable `ReferenceHandler.Preserve`:

```csharp
using Krampus.BinJson;
using Krampus.BinJson.Serialization;
using Krampus.BinJson.Serialization.References;

var options = new BJsonSerializerOptions
{
	ReferenceHandler = ReferenceHandler.Preserve
};

var value = BJson.Serialize(graphRoot, options);
var roundTrip = BJson.Deserialize<Node>(value, options);
```

When enabled, BinJson writes `$id` and `$ref` metadata for attributed object graphs.

## Serializer Options

`BJsonSerializerOptions` currently supports:

- `IgnoreNullValues`
- `PropertyNameCaseInsensitive`
- `MaxDepth`
- `ReferenceHandler`
- `IncludeFields`
- `IncludePrivateMembers`
- `StrictMode`
- `NamingPolicy`
- `AddConverter(...)`

Example:

```csharp
var options = new BJsonSerializerOptions
{
	PropertyNameCaseInsensitive = true,
	IgnoreNullValues = true,
	NamingPolicy = NamingPolicy.CamelCase
};
```

## Built-In Converters

The serializer registers built-in converters for:

- `DateTime`
- `Guid`
- `TimeSpan`
- `Uri`

You can override behavior by registering your own converter for the same target type.

## Source Generators

BinJson includes a source generator project that emits serializer types for models annotated with `[BJsonSerializable]`.
Generated serializers follow the naming pattern `{TypeName}_BJsonSerializer` in the model namespace.

### Setup

The main project already references the generator as an analyzer:

```xml
<ProjectReference Include="..\BinJson.SourceGenerators\BinJson.SourceGenerators.csproj"
				  OutputItemType="Analyzer"
				  ReferenceOutputAssembly="false" />
```

If your project consumes BinJson source directly, keep that analyzer reference so serializer classes are emitted at build time.

### Runtime behavior

At runtime, BinJson first looks for a generated serializer for the target type.
If none is found, it falls back to custom converters, interfaces, and reflection-based attribute serialization.

## Best Practices

- Prefer `[BJsonSerializable]` for regular DTO-style models.
- Use custom converters for value formatting concerns.
- Use interfaces only when a type needs full control over its payload shape.
- Enable `ReferenceHandler.Preserve` only when object identity matters.
- Keep `StrictMode = true` when validating required data is important.
- Use extension data for schema evolution and passthrough payloads.

## Limitations

- Source generation currently targets attributed classes discovered at compile time.
- Reference preservation applies to object graphs serialized through the attributed object pipeline.
- Constructor binding depends on constructor parameter names matching serialized member names.

## DOM Utility APIs

Although this guide focuses on object serialization, recent DOM APIs are useful when building pre- and post-processing pipelines around the serializer.

### Safe parse and deserialize

Use the non-throwing façade methods when ingesting untrusted payloads:

```csharp
if (BJson.TryParse(jsonText, out var document))
{
	// continue processing
}

if (BJson.TryDeserialize(binaryPayload, out var root))
{
	// continue processing
}
```

### Recursive transformations

Use `BJson.Transform` to normalize data before typed deserialization:

```csharp
var normalized = BJson.Transform(input, value =>
{
	if (value.IsString && value.StringValue == "N/A")
		return BJsonValue.Null;
	return value;
});
```

### Merge strategies for configuration layering

`BJsonObject.Merge` supports explicit strategies through `BJsonMergeStrategy`:

- `Overwrite`: incoming values replace existing values.
- `KeepExisting`: existing values are preserved; only missing keys are added.
- `DeepMerge`: nested objects are merged recursively.

```csharp
baseConfig.Merge(environmentConfig, BJsonMergeStrategy.DeepMerge);
```
