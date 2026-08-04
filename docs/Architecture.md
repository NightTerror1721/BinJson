# Architecture

This document describes the internal separation strategy for synchronous and asynchronous I/O in BinJson.

## Goals

- Keep sync and async public APIs explicit and predictable.
- Prevent accidental coupling between sync and async implementations.
- Share protocol logic in one place to avoid behavior regressions.

## Public Types

### Facade Layer

- `BJson`: generic compatibility facade (broad API surface)
- `BJsonBinaryFacade`: binary-focused facade
- `BJsonTextFacade`: text-focused facade
- `BJsonTypedFacade`: CLR object-focused facade
- `BJsonDomFacade`: DOM-focused utility facade

The specialized facades are preferred for new code when a workflow is domain-specific, because they improve discoverability and keep call sites focused.

### Binary

- Sync: `BJsonBinaryReader`, `BJsonBinaryWriter`
- Async: `BJsonBinaryReaderAsync`, `BJsonBinaryWriterAsync`

### Text

- Sync: `BJsonTextReader`, `BJsonTextWriter`
- Async: `BJsonTextReaderAsync`, `BJsonTextWriterAsync`

## Internal Layering

### Base Layer (`*Base`)

`*Base` types are responsible for:

- Constructor validation
- Shared options access
- Resource ownership (`leaveOpen`) and disposal semantics

Examples:

- `BJsonBinaryReaderBase`
- `BJsonBinaryWriterBase`
- `BJsonTextReaderBase`
- `BJsonTextWriterBase`

### Core Layer (`*Core`)

`*Core` types hold shared serialization/deserialization logic.

Examples:

- `BJsonBinaryReaderCore`
- `BJsonBinaryWriterCore`
- `BJsonTextWriterCore`

This layer centralizes format rules (type codes, VarUInt handling, packed arrays, string table behavior, JSON text emission, etc.).

### Wrapper Layer (Sync/Async Public Types)

Public sync and async classes are thin wrappers that:

- own API shape (`Read` vs `ReadAsync`, `Write` vs `WriteAsync`)
- delegate shared format work to `*Core`
- preserve error contracts and options behavior

## Non-Coupling Rule

The split is intentional and strict:

- Async wrappers must not call sync reader/writer wrappers.
- Sync wrappers must not call async reader/writer wrappers.
- Both may use shared `*Core` / `*Base` abstractions.

This rule prevents hidden dependencies and keeps each execution model independently maintainable.

## Facade Interaction

`BJson` facade methods may route to sync or async wrapper types depending on the API used. This is expected and does not violate the non-coupling rule, because coupling is prohibited at reader/writer wrapper level, not facade level.

## Contribution Checklist

When changing I/O behavior:

1. Update shared protocol logic in `*Core` first.
2. Keep sync and async wrappers as thin orchestration layers.
3. Do not introduce direct calls between sync and async wrappers.
4. Preserve exception metadata contracts (`errorCode`, path, offsets, operation/section).
5. Run `dotnet test BinJson.slnx`.
