# BinJson Binary Format Specification v1.0

## Overview

BinJson is a compact binary encoding format for JSON-like data structures. It is designed for efficient serialization and deserialization with minimal overhead while maintaining compatibility with standard JSON semantics.

## Design Goals

- **Compact**: Minimal size overhead
- **Fast**: Efficient to parse and generate
- **Type-preserving**: Maintains numeric type information (Int8, Int16, Int32, Int64, Float32, Float64, etc.)
- **Self-describing**: Type information embedded in the format
- **Platform-independent**: Fixed endianness and encoding

## General Format

### Endianness
All multi-byte numeric values are encoded in **little-endian** byte order.

### String Encoding
All strings are encoded in **UTF-8** without BOM (Byte Order Mark).

### Length Prefixes
Variable-length data (strings, arrays, objects, binary) use **Int32** (4 bytes, little-endian) length prefixes.

### Type Codes
Every value starts with a single-byte type code that identifies its type:

| Type Code | Hex  | Type        | Description                          |
|-----------|------|-------------|--------------------------------------|
| `0x00`    | 0    | Null        | Represents JSON null                 |
| `0x01`    | 1    | Int8        | Signed 8-bit integer (sbyte)         |
| `0x02`    | 2    | Int16       | Signed 16-bit integer (short)        |
| `0x03`    | 3    | Int32       | Signed 32-bit integer (int)          |
| `0x04`    | 4    | Int64       | Signed 64-bit integer (long)         |
| `0x05`    | 5    | UInt8       | Unsigned 8-bit integer (byte)        |
| `0x06`    | 6    | UInt16      | Unsigned 16-bit integer (ushort)     |
| `0x07`    | 7    | UInt32      | Unsigned 32-bit integer (uint)       |
| `0x08`    | 8    | UInt64      | Unsigned 64-bit integer (ulong)      |
| `0x09`    | 9    | Float32     | IEEE 754 single-precision float      |
| `0x0A`    | 10   | Float64     | IEEE 754 double-precision float      |
| `0x0B`    | 11   | BoolTrue    | Boolean true                         |
| `0x0C`    | 12   | BoolFalse   | Boolean false                        |
| `0x0D`    | 13   | String      | UTF-8 encoded string                 |
| `0x0E`    | 14   | Array       | Ordered collection of values         |
| `0x0F`    | 15   | Object      | Key-value dictionary (string keys)   |
| `0x10`    | 16   | Binary      | Raw byte array                       |

## Type Encoding Details

### Null (0x00)
```
[TypeCode: 0x00]
```
Single byte, no additional data.

### Integers (0x01-0x08)
```
[TypeCode] [Value: N bytes]
```
Where N is determined by the type:
- Int8/UInt8: 1 byte
- Int16/UInt16: 2 bytes (little-endian)
- Int32/UInt32: 4 bytes (little-endian)
- Int64/UInt64: 8 bytes (little-endian)

**Example**: Int32 value `42`
```
0x03 0x2A 0x00 0x00 0x00
```

### Floats (0x09-0x0A)
```
[TypeCode] [Value: N bytes]
```
- Float32: 4 bytes (IEEE 754, little-endian)
- Float64: 8 bytes (IEEE 754, little-endian)

**Example**: Float64 value `3.14159`
```
0x0A [8 bytes of IEEE 754 double]
```

### Booleans (0x0B, 0x0C)
```
[TypeCode]
```
Single byte, no additional data.
- `0x0B`: true
- `0x0C`: false

### String (0x0D)
```
[TypeCode: 0x0D] [Length: Int32] [UTF-8 Bytes]
```
- **Length**: Number of bytes in UTF-8 encoding (not character count)
- **UTF-8 Bytes**: Raw UTF-8 encoded string data

**Example**: String `"hello"`
```
0x0D 0x05 0x00 0x00 0x00 0x68 0x65 0x6C 0x6C 0x6F
```

### Array (0x0E)
```
[TypeCode: 0x0E] [Count: Int32] [Element1] [Element2] ... [ElementN]
```
- **Count**: Number of elements in the array
- **Elements**: Each element is a complete BinJson value (with its own type code)

**Example**: Array `[1, "test", null]`
```
0x0E                     // Array type code
0x03 0x00 0x00 0x00      // Count: 3
0x05 0x01                // Element 0: UInt8 value 1
0x0D 0x04 0x00 0x00 0x00 0x74 0x65 0x73 0x74  // Element 1: String "test"
0x00                     // Element 2: Null
```

### Object (0x0F)
```
[TypeCode: 0x0F] [Count: Int32] [Key1] [Value1] [Key2] [Value2] ... [KeyN] [ValueN]
```
- **Count**: Number of key-value pairs
- **Keys**: Each key is encoded as a String (without type code prefix, just Length + UTF-8 bytes)
- **Values**: Each value is a complete BinJson value (with its own type code)

**Example**: Object `{"name": "Alice", "age": 30}`
```
0x0F                     // Object type code
0x02 0x00 0x00 0x00      // Count: 2 pairs

// Pair 1: "name" => "Alice"
0x04 0x00 0x00 0x00      // Key length: 4
0x6E 0x61 0x6D 0x65      // Key bytes: "name"
0x0D 0x05 0x00 0x00 0x00 0x41 0x6C 0x69 0x63 0x65  // Value: String "Alice"

// Pair 2: "age" => 30
0x03 0x00 0x00 0x00      // Key length: 3
0x61 0x67 0x65           // Key bytes: "age"
0x05 0x1E                // Value: UInt8 30
```

### Binary (0x10)
```
[TypeCode: 0x10] [Length: Int32] [Raw Bytes]
```
- **Length**: Number of bytes in the binary data
- **Raw Bytes**: Raw byte data

**Example**: Binary `[0xFF, 0xAA, 0x55]`
```
0x10 0x03 0x00 0x00 0x00 0xFF 0xAA 0x55
```

## Size Limits

- **String length**: Maximum 2,147,483,647 bytes (Int32.MaxValue)
- **Array count**: Maximum 2,147,483,647 elements (Int32.MaxValue)
- **Object count**: Maximum 2,147,483,647 pairs (Int32.MaxValue)
- **Binary length**: Maximum 2,147,483,647 bytes (Int32.MaxValue)

## Implementation Notes

### Numeric Type Selection
When serializing, the smallest appropriate type should be used:
- Integers that fit in smaller types should use Int8/Int16/Int32 instead of always using Int64
- However, exact type preservation is allowed if the source specified a particular numeric type

### Object Key Ordering
Object keys have no guaranteed ordering in the format. Implementations may serialize keys in any order.

### Duplicate Keys
Duplicate keys in objects are **not allowed**. Behavior for duplicate keys is undefined and may result in an error during deserialization.

### Invalid Type Codes
Any type code not defined in this specification should result in a deserialization error with a clear exception message.

### Nesting Depth
Implementations should protect against excessive nesting depth (e.g., deeply nested arrays/objects) to prevent stack overflow attacks. A reasonable limit is 64-128 levels of nesting.

## Version History

### v1.0 (Current)
- Initial specification
- Type codes 0x00-0x10 defined
- Little-endian encoding
- UTF-8 strings
- Int32 length prefixes

## Future Considerations

Reserved type code ranges for future extensions:
- `0x11-0x1F`: Reserved for extended primitive types
- `0x20-0x7F`: Reserved for structured types
- `0x80-0xFF`: Reserved for application-specific extensions

Potential future additions:
- Compressed strings
- Reference encoding for shared objects
- Schema versioning
- Optional metadata headers
