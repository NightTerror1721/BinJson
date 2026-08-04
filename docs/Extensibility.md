# Extensibility Guide

This guide explains how to extend BinJson beyond direct DOM usage.

It focuses on CLR object serialization and on the extension points exposed by `Krampus.BinJson.Serialization`.

## Overview

BinJson supports four main extensibility styles:

1. Attribute-driven contracts for DTO-style models.
2. Custom converters for reusable type-level formatting.
3. Converter factories for generic or shape-driven conversion.
4. Interface-based serialization when a type needs full control over its payload.

These styles can be combined. For example, a `BJsonSerializable` model can still use converter attributes, default providers, version guards, preprocessor hooks, and lifecycle hooks.

## Choosing the Right Approach

| Approach | Best for | Main trade-off |
|---|---|---|
| Attributes | Regular DTOs and configuration objects | Less freedom over exact payload shape |
| Custom converters | Reusable formatting for one target type | Converter owns the whole representation |
| Converter factories | Open generics and wrapper types | More infrastructure than a single converter |
| Interfaces | Fully custom read/write behavior | Highest maintenance cost |

## Attribute-Driven Contracts

Use attributes when your CLR type mostly maps to members.

```csharp
using Krampus.BinJson;
using Krampus.BinJson.Serialization;

[BJsonSerializable(NamingPolicy = NamingPolicy.CamelCase)]
public sealed class PlayerProfile
{
    [BJsonPropertyName("identifier")]
    public int Id { get; set; }

    [BJsonRequired]
    public string Name { get; set; } = string.Empty;

    [BJsonIgnore(Condition = BJsonIgnoreCondition.WhenWritingNull)]
    public string? Alias { get; set; }
}

var value = BJson.Serialize(new PlayerProfile { Id = 7, Name = "mage" });
var model = BJson.Deserialize<PlayerProfile>(value);
```

Use attributes when you want:

- Reflection support without custom plumbing
- Source generation for the same model
- Versioning, defaults, aliases, factories, extension data, or preprocessing

For the full attribute catalog, see `docs/Attributes.md`.

## Interface-Based Contracts

Implement `IBJsonSerializable` and `IBJsonDeserializable` when the payload should not mirror the public member structure.

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

Use interfaces when:

- Serialization needs branching that should live inside the type
- The wire payload has little resemblance to CLR members
- The type owns all invariants and wants to control repair logic directly

## Custom Converters

Create a converter by inheriting from `BJsonConverter<T>`.

```csharp
using System;
using System.Globalization;
using Krampus.BinJson;
using Krampus.BinJson.Serialization;

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
```

### Registering a converter globally

```csharp
var options = new BJsonSerializerOptions();
options.AddConverter(new DateOnlyStringConverter());
```

### Applying a converter to one member

```csharp
[BJsonSerializable]
public sealed class AuditEntry
{
    [BJsonConverter(typeof(DateOnlyStringConverter))]
    public DateTime CreatedAt { get; set; }
}
```

### Applying a converter to an entire type

```csharp
[BJsonConverter(typeof(DateOnlyStringConverter))]
public struct DateOnlyLike
{
    public DateTime Value { get; set; }
}
```

Use a normal converter when the target type is fixed and known.

## Converter Factories

Use `IBJsonConverterFactory` when conversion depends on a closed generic type.

```csharp
using System;
using Krampus.BinJson;
using Krampus.BinJson.Serialization;

public readonly struct Wrapped<T>
{
    public Wrapped(T value)
    {
        Value = value;
    }

    public T Value { get; }
}

public sealed class WrappedConverterFactory : IBJsonConverterFactory
{
    public bool CanConvert(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Wrapped<>);
    }

    public IBJsonConverter? CreateConverter(Type type)
    {
        var itemType = type.GetGenericArguments()[0];
        var converterType = typeof(WrappedConverter<>).MakeGenericType(itemType);
        return Activator.CreateInstance(converterType) as IBJsonConverter;
    }
}

public sealed class WrappedConverter<T> : BJsonConverter<Wrapped<T>>
{
    public override BJsonValue Serialize(Wrapped<T> value, BJsonSerializationContext context)
    {
        return context.Serialize(value.Value, typeof(T));
    }

    public override Wrapped<T> Deserialize(BJsonValue value, BJsonSerializationContext context)
    {
        return new Wrapped<T>(context.Deserialize<T>(value)!);
    }
}
```

### Registering a factory globally

```csharp
var options = new BJsonSerializerOptions();
options.AddConverterFactory(new WrappedConverterFactory());
```

### Applying a factory through an attribute

```csharp
[BJsonSerializable]
public sealed class WrapperEnvelope
{
    [BJsonConverterFactory(typeof(WrappedConverterFactory))]
    public Wrapped<int> Count { get; set; }
}
```

Use a converter factory when:

- A family of related types shares one conversion strategy
- The converter must be selected from generic arguments
- A fixed `BJsonConverter<T>` would be too narrow

## Construction Customization

BinJson can materialize models with:

- A default constructor
- A constructor marked with `BJsonConstructor`
- A static factory method marked with `BJsonFactoryMethod`

### Constructor binding

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

### Factory method binding

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

### Explicit parameter mapping

```csharp
[BJsonSerializable]
public sealed class Point
{
    public int X { get; private set; }
    public int Y { get; private set; }

    [BJsonFactoryMethod(ParameterMapping = new[] { "x", "coord_x", "y", "coord_y" })]
    internal static Point Create(int x, int y)
    {
        return new Point { X = x, Y = y };
    }
}
```

## Defaults, Validation, and Versioning

These attributes are especially useful for long-lived wire contracts.

### Constant and provider defaults

```csharp
[BJsonSerializable]
public sealed class Profile
{
    [BJsonDefaultValue("guest")]
    public string Role { get; set; } = string.Empty;

    [BJsonDefaultProvider(nameof(GetDefaultMode))]
    public string Mode { get; set; } = string.Empty;

    internal static string GetDefaultMode(IComparable? version)
        => version is Version semantic && semantic >= new Version(3, 0, 0)
           ? "modern"
           : "legacy";
}
```

### Conditional requiredness

```csharp
[BJsonSerializable]
[BJsonVersionContext(typeof(Version), "2.0.0")]
public sealed class ConditionalModel
{
    [BJsonRequiredWhen(nameof(IsNameRequired))]
    public string? Name { get; set; }

    internal static bool IsNameRequired(string memberName, IComparable? version)
        => version is Version semantic && semantic >= new Version(2, 0, 0);
}
```

### Member and type version ranges

```csharp
[BJsonSerializable]
[BJsonVersionContext(typeof(Version), "3.0.0")]
[BJsonVersion(typeof(Version), introducedIn: "2.0.0")]
public sealed class VersionedSettings
{
    [BJsonVersion(typeof(Version), removedIn: "3.0.0")]
    public int LegacyValue { get; set; }

    [BJsonVersion(typeof(Version), introducedIn: "2.0.0", RenamedFrom = "score")]
    public int TotalScore { get; set; }
}
```

### Legacy aliases

```csharp
[BJsonAlias("legacy_name")]
[BJsonAlias("legacy_name_v2")]
public string Name { get; set; } = string.Empty;
```

## Preprocessing and External Resources

The preprocessor runs before typed binding and lets you work on the raw DOM.

### Enabling the built-in preprocessor

```csharp
[BJsonSerializable]
[BJsonPreprocessor]
public sealed class ThemeConfig
{
    [BJsonAnchor("primaryColor")]
    public string PrimaryColor { get; set; } = "#22CC88";

    public string DisplayColor { get; set; } = string.Empty;

    [BJsonExternalRef(Optional = true)]
    public Inventory? Inventory { get; set; }
}
```

### Supplying variables and base path

```csharp
var options = new BJsonSerializerOptions
{
    PreprocessorContext = new BJsonPreprocessorContext
    {
        BasePath = @"C:\game-data"
    },
    ExternalReferencePathPolicy = ExternalReferencePathPolicy.RestrictToBasePath
};

((BJsonPreprocessorContext)options.PreprocessorContext).SetVariable("Platform", "Desktop");
```

### Example payload with `$branches` and `$ref`

```json
{
  "$branches": [
    {
      "$if": { "$var": "Platform", "$eq": "Desktop" },
      "$then": {
        "PrimaryColor": "#22CC88",
        "DisplayColor": { "$ref": "primaryColor" },
        "Inventory": "inventory.bjson"
      }
    },
    {
      "$else": {
        "PrimaryColor": "#999999",
        "DisplayColor": "fallback"
      }
    }
  ]
}
```

## Lifecycle Hooks

Use lifecycle hooks to update instance state before write or after read.

```csharp
[BJsonSerializable]
public sealed class LifecycleModel
{
    public string Name { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string CacheKey { get; private set; } = string.Empty;

    [BJsonOnSerializing]
    internal void OnSerializing()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    [BJsonOnDeserialized]
    internal void OnDeserialized()
    {
        CacheKey = Name.ToLowerInvariant();
    }
}
```

Supported signatures:

- `void Method()`
- `void Method(BJsonSerializationContext context)` for `BJsonOnSerializing`
- `void Method(BJsonDeserializationContext context)` for `BJsonOnDeserialized`

## Serializer Options

`BJsonSerializerOptions` controls runtime behavior for reflection and generated flows.

Relevant options include:

- `IgnoreNullValues`
- `PropertyNameCaseInsensitive`
- `MaxDepth`
- `ReferenceHandler`
- `PreprocessorContext`
- `ExternalReferencePathPolicy`
- `IncludeFields`
- `IncludePrivateMembers`
- `StrictMode`
- `NamingPolicy`
- `Version`
- `AddConverter(...)`
- `AddConverterFactory(...)`

Example:

```csharp
var options = new BJsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    StrictMode = true,
    NamingPolicy = NamingPolicy.CamelCase,
    Version = new Version(3, 0, 0)
};
```

## Reusable Runtime

When you repeatedly serialize or deserialize with the same configuration, use `BJsonRuntime`.

```csharp
var runtime = new BJsonRuntime(new BJsonSerializerOptions
{
    NamingPolicy = NamingPolicy.SnakeCase
});

var first = runtime.Serialize(new PlayerProfile { Id = 1, Name = "A" });
var second = runtime.Serialize(new PlayerProfile { Id = 2, Name = "B" });
```

This keeps metadata and converter caches warm across operations.

## Source Generators

BinJson includes a source generator that emits `{TypeName}_BJsonSerializer` for attributed models.

### When generation helps most

Source generation is especially useful for:

- Hot-path DTO serialization
- High repetition workloads
- AOT or reflection-sensitive environments
- Large model graphs with stable schemas

### Important constraints

- Helper methods referenced by attributes must be callable from generated code.
- Generic and nested unsupported shapes may raise diagnostics.
- Reflection remains the fallback path when no generated serializer exists.

## DOM Utility APIs Around the Serializer

Object serialization often benefits from DOM-level preprocessing or normalization.

### Safe parse and continue

```csharp
if (BJson.TryParse(jsonText, out var document))
{
    var model = BJson.Deserialize<PlayerProfile>(document);
}
```

### DOM transformation before typed deserialization

```csharp
var normalized = BJson.Transform(input, value =>
{
    if (value.IsString && value.StringValue == "N/A")
        return BJsonValue.Null;

    return value;
});
```

### Merge layered configuration documents

```csharp
baseConfig.Merge(environmentConfig, BJsonMergeStrategy.DeepMerge);
```

## Best Practices

- Prefer attributes for ordinary DTOs.
- Use a converter when one type always needs the same wire format.
- Use a converter factory when the conversion depends on generic arguments.
- Use interfaces only when the payload shape is truly custom.
- Keep helper methods `internal` unless they are part of your public API.
- Use `StrictMode = true` when contract validation matters.
- Set `PreprocessorContext.BasePath` when using external references with relative paths.
- Use `BJsonRuntime` for repeated operations under one stable configuration.

## Related Documentation

- `docs/Attributes.md`
- `docs/ErrorHandling.md`
- `docs/CompatibilityNotes.md`
- `docs/MigrationRecipes.md`
- `docs/Architecture.md`
