# Compatibility Notes

This document records behavior clarifications and effective changes introduced while completing the attribute-system roadmap.

## Scope

These notes apply to runtime reflection and source-generated serializers in the current implementation line.

## Behavior Clarifications

- Source-generated serializers now support polymorphism, preprocessor-enabled models, anchors, and external references for supported shapes.
- Type-level and member-level version guards are composed during read and write flows.
- `WhenWritingCustomDefault` compares against provider-supplied defaults when `[BJsonDefaultProvider]` is configured.
- External reference serialization stores a string path token and writes member payload to the referenced file.
- `BJsonDefaultProvider` supports version-aware overloads that receive `IComparable? version`.
- `BJsonAlias` is read-only compatibility metadata: aliases are accepted during deserialization but are never emitted during serialization.
- `BJsonRequiredWhen` composes with strict mode and version flow, allowing conditional contract validation without custom converters.
- `BJsonNumberHandling` allows numeric strings on read and string emission on write for members that need wire-level compatibility.
- `BJsonOnSerializing` and `BJsonOnDeserialized` run as part of the attributed object pipeline and affect both reflection and generated flows.

## Effective Breaking Changes for Legacy Integrations

- Generated-path fallback diagnostic `BJSON011` is no longer part of active diagnostics. Integrations that expected this diagnostic should update checks to active IDs.
- Code that expected external reference inline object shapes (for example `{ "$ref": "path" }` as the member token) must switch to string path token expectations.
- Factory method validation is strict in both runtime and generator paths: multiple factories, invalid signatures, and invalid parameter mappings now fail predictably.

## Runtime vs Generated Differences to Keep in Mind

- Generated serializers require helper methods referenced by attributes to be callable from generated code (`public`, `internal`, or `protected internal`).
- Runtime reflection can discover private static factory methods marked with `[BJsonFactoryMethod]`.
- Generated serializers require the full 4-parameter `[BJsonValueMapper]` method signature.
- Runtime reflection also accepts the short `BJsonValue -> BJsonValue` mapper signature.
- Generated serializers validate helper signatures at build time; runtime reflection surfaces equivalent problems when the path is exercised.

## Diagnostics Baseline

Active source-generator diagnostics are:

- `BJSON001` to `BJSON010`
- `BJSON012` to `BJSON016`

See `docs/Attributes.md` for descriptions and `docs/AttributeCompatibilityMatrix.md` for parity tracking.
