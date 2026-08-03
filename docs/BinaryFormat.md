# BinJson Binary Format Specification v1.0 (Final)

## Overview

This is the official and only BinJson binary format v1.0.

- There is no legacy wire format compatibility.
- The format is compact, type-aware, and optimized for low-overhead DOM round-trips.

The format targets the BinJson DOM types:

- `BJsonValue`
- `BJsonArray`
- `BJsonObject`
- `BJsonBinary`

## Core Rules

### Endianness

All fixed-width multibyte numeric payloads are little-endian.

### Text Encoding

All text is UTF-8 without BOM.

### Size and Count Encoding

All lengths and counts are encoded as `VarUInt` (LEB128 unsigned variable-length integer).

- `VarUInt` is used only for lengths, counts, and indexes.
- `VarUInt` is not a DOM value type.

### Canonical Writing

Writers must produce the smallest encoding by byte size.

- Prefer fixed families when available (`FixInt`, `FixStr`, `FixArray`, `FixObject`).
- For alternative encodings with equal size, choose the simpler fixed form.

## Type Code Table

### Fixed Ranges

| Range | Name | Meaning |
|---|---|---|
| `0x00-0x7F` | Positive FixInt | Integer value embedded in the type byte (`0..127`) |
| `0x90-0xAF` | FixStr | UTF-8 string, length in low 5 bits (`0..31`) |
| `0xB0-0xBF` | FixArray | Array, element count in low 4 bits (`0..15`) |
| `0xC0-0xCF` | FixObject | Object, pair count in low 4 bits (`0..15`) |

### Single-Byte Tags

| Hex | Name | Payload |
|---|---|---|
| `0x80` | Null | none |
| `0x81` | BoolFalse | none |
| `0x82` | BoolTrue | none |
| `0x83` | Int8 | 1 byte |
| `0x84` | Int16 | 2 bytes LE |
| `0x85` | Int32 | 4 bytes LE |
| `0x86` | Int64 | 8 bytes LE |
| `0x87` | UInt8 | 1 byte |
| `0x88` | UInt16 | 2 bytes LE |
| `0x89` | UInt32 | 4 bytes LE |
| `0x8A` | UInt64 | 8 bytes LE |
| `0x8B` | Float32 | 4 bytes IEEE-754 LE |
| `0x8C` | Float64 | 8 bytes IEEE-754 LE |
| `0x8D` | VarInt | Reserved scalar tag (not used for DOM values) |
| `0x8E` | VarUInt | Reserved scalar tag (not used for DOM values) |
| `0x8F` | Reserved | invalid for current parser |
| `0xD0` | String8 | `VarUInt length` + UTF-8 bytes |
| `0xD1` | String16 | `VarUInt length` + UTF-8 bytes |
| `0xD2` | String32 | `VarUInt length` + UTF-8 bytes |
| `0xD3` | StringRef | `VarUInt index` into StringTable |
| `0xD4` | ArrayVar | `VarUInt count` + `count` values |
| `0xD5` | ObjectVar | `VarUInt pairCount` + pairs |
| `0xD6` | PackedArray | `[ElemTypeCode][VarUInt count][payload]` |
| `0xD7` | Binary | `VarUInt length` + raw bytes |
| `0xD8-0xDF` | Reserved | invalid |
| `0xE0` | HeaderMarker | `[Magic 'B''J'][Version][Flags]` |
| `0xE1` | StringTable | `VarUInt count` + entries |
| `0xE2` | ExtContainer | `VarUInt length` + extension payload |
| `0xE3-0xFF` | Reserved | invalid |

## Structures

### Strings

- `FixStr`: type byte carries length.
- `String8/16/32`: current canonical writer uses `String32` + `VarUInt length` for non-fix lengths.

### Arrays

- `FixArray`: small arrays (`0..15`).
- `ArrayVar`: large arrays (`VarUInt count`).

### Objects

- `FixObject`: small objects (`0..15`).
- `ObjectVar`: large objects (`VarUInt pair count`).
- Object keys are always encoded as: `VarUInt keyLength` + `UTF-8 key bytes`.

### Binary

- `Binary`: `VarUInt length` + raw bytes.

## Optional Header

`HeaderMarker` is optional and emitted only when needed (for example, when a `StringTable` block is present).

Header layout:

- `Magic`: ASCII `B`, `J`
- `Version`: `0x01`
- `Flags`:
	- bit 0: StringTable present
	- bit 1: ExtContainer present

The header advertises optional blocks. Blocks are still emitted as explicit tags (`StringTable`, `ExtContainer`) in the stream.

## StringTable and StringRef

### StringTable

`StringTable` defines interned strings in order:

- `VarUInt count`
- repeated `count` times: `VarUInt length` + UTF-8 bytes

### StringRef

`StringRef` payload is `VarUInt index`.

Reader behavior for invalid indexes is configurable:

- `Strict`: throw `BJsonBinaryFormatException`
- `CoerceNull`: treat invalid reference as `null`

### Emission Criterion

The writer emits references only when they reduce total payload size.

## PackedArray

`PackedArray` layout:

- `[PackedArrayTag][ElemTypeCode][VarUInt count][payload]`

Supported element categories are non-composed types:

- `null`, `bool`, integer scalars, float scalars, `string`, `binary`

Current payload strategies:

- `null`: count-only (no per-element bytes)
- `bool`: bit-packed
- scalar numeric: contiguous fixed-width values
- `string`: var-length strings or string refs depending on plan
- `binary`: per-element `VarUInt length` + raw bytes

Packed encoding is used only when it is strictly smaller than regular array encoding.

## Errors and Validation

Deserialization must fail with `BJsonBinaryFormatException` for:

- unknown/reserved type codes
- malformed `VarUInt`
- unsupported header versions/flags
- duplicate object keys
- truncated payloads

## Operational Limits

The current reference implementation enforces practical limits for decoded lengths/counts:

- Any decoded `VarUInt` used as a length/count/index must fit in signed 32-bit range (`<= Int32.MaxValue`).
- Values above `Int32.MaxValue` fail with a binary format error.
- Malformed `VarUInt` sequences (more than 10 continuation bytes without termination) fail with a binary format error.

### Defensive Recommendations

For untrusted payloads, implementations should also apply configurable guards:

- Maximum nesting depth for arrays/objects (recommended: 64-128)
- Maximum total string bytes
- Maximum total binary bytes
- Maximum container element/pair counts

These guards are additive hardening controls and do not change wire compatibility.

## Example

Object:

```json
{ "player": "Hero", "level": 10, "active": true }
```

Canonical binary bytes (hex):

```text
C3                         // FixObject, 3 pairs
06 70 6C 61 79 65 72       // key "player" (VarUInt len=6 + bytes)
94 48 65 72 6F             // FixStr "Hero"
05 6C 65 76 65 6C          // key "level" (VarUInt len=5 + bytes)
0A                         // Positive FixInt 10
06 61 63 74 69 76 65       // key "active" (VarUInt len=6 + bytes)
82                         // BoolTrue
```

## Version Policy

`v1.0` is final for this repository branch and replaces previous drafts.
