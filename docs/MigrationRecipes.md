# Migration Recipes

This guide provides practical migrations from older attribute-system patterns to the current implementation.

## Recipe 1: Replace Unsupported-Fallback Expectations

Problem:

- Legacy tests or build checks expect generated fallback diagnostics for advanced attributes.

Migration:

1. Remove assertions that require unsupported fallback behavior.
2. Assert generated runtime parity for advanced features instead.
3. Validate against active diagnostics (`BJSON001` to `BJSON010`, `BJSON012` to `BJSON016`).

## Recipe 2: Move to Strict Factory Mapping

Problem:

- Factory parameter extraction relied on implicit or inconsistent key matching.

Migration:

1. Add explicit `ParameterMapping` on `[BJsonFactoryMethod]` when JSON keys differ from parameter names.
2. Ensure mapping is in alternating `paramName`, `jsonKey` pairs.
3. Ensure each parameter appears at most once and JSON keys are unique.

Example:

```csharp
[BJsonFactoryMethod(ParameterMapping = new[] { "x", "coord_x", "y", "coord_y" })]
public static Point Create(int x, int y) => new Point(x, y);
```

## Recipe 3: Update External Reference Expectations

Problem:

- Consumers expect inline object reference tokens at member positions.

Migration:

1. Read/write member tokens as string paths.
2. Set `PreprocessorContext.BasePath` for safe relative resolution.
3. Keep `ExternalReferencePathPolicy` as `RestrictToBasePath` unless unrestricted paths are required.

Example:

```csharp
var options = new BJsonSerializerOptions
{
    PreprocessorContext = new BJsonPreprocessorContext { BasePath = basePath }
};
```

## Recipe 4: Align Custom Default Semantics

Problem:

- `WhenWritingCustomDefault` was treated as CLR default comparison in legacy behavior.

Migration:

1. Add `[BJsonDefaultProvider]` for semantic defaults.
2. Keep `[BJsonIgnore(Condition = BJsonIgnoreCondition.WhenWritingCustomDefault)]` on the same member.
3. Verify that write omission now follows provider default value, not `default(T)`.

## Recipe 5: Adopt Type-Level Version Composition

Problem:

- Type-level `[BJsonVersion]` was not uniformly enforced in older flows.

Migration:

1. Add `[BJsonVersion]` to the type for broad range constraints.
2. Keep member `[BJsonVersion]` for narrower scopes.
3. Use `BJsonSerializerOptions.Version` to override context per operation when needed.

## Recipe 6: Generated Helper Visibility Fix

Problem:

- Source-generated builds fail because helper methods are private.

Migration:

1. Change helper methods referenced by attributes to `internal`, `public`, or `protected internal`.
2. Rebuild and verify no `BJSON008` accessibility warnings remain for required helpers.

## Recipe 7: Add Read-Only Legacy Names Safely

Problem:

- Older payloads may use several historical JSON keys for the same logical member.

Migration:

1. Keep the current canonical member name or `BJsonPropertyName`.
2. Use `BJsonVersion(..., RenamedFrom = ...)` for the primary historical key when a version boundary exists.
3. Add `BJsonAlias` for any extra legacy names still seen in production.

Example:

```csharp
[BJsonPropertyName("total_score")]
[BJsonVersion(typeof(Version), introducedIn: "2.0.0", RenamedFrom = "score")]
[BJsonAlias("legacyScore")]
public int TotalScore { get; set; }
```

## Recipe 8: Move Requiredness Into the Contract

Problem:

- Required member checks are currently implemented outside the serializer or spread across custom code.

Migration:

1. Use `BJsonRequired` for unconditional requirements.
2. Use `BJsonRequiredWhen` when requiredness depends on document version or migration phase.
3. Keep `StrictMode = true` for validation-sensitive call sites.

Example:

```csharp
[BJsonRequiredWhen(nameof(IsNameRequired))]
public string? Name { get; set; }

internal static bool IsNameRequired(string memberName, IComparable? version)
    => version is Version semantic && semantic >= new Version(2, 0, 0);
```

## Recipe 9: Migrate Numeric String Payloads Without a Full Converter

Problem:

- External systems send numbers as strings, or exact decimal text must be preserved.

Migration:

1. Add `BJsonNumberHandling(BJsonNumberHandling.AllowReadingFromString)` for integer-like compatibility.
2. Add `Lossless` when exact string preservation matters on write.
3. Reserve a full custom converter for cases where the entire representation changes.

Example:

```csharp
[BJsonNumberHandling(BJsonNumberHandling.AllowReadingFromString | BJsonNumberHandling.Lossless)]
public decimal Amount { get; set; }
```

## Recipe 10: Replace Post-Load Repair Code With Lifecycle Hooks

Problem:

- Consumers manually normalize objects after every deserialize call.

Migration:

1. Move invariant-repair logic into `BJsonOnDeserialized`.
2. Move last-minute write preparation into `BJsonOnSerializing`.
3. Keep these hooks focused on instance state, not I/O.

Example:

```csharp
[BJsonOnDeserialized]
internal void Normalize()
{
    CacheKey = Name.ToLowerInvariant();
}
```
