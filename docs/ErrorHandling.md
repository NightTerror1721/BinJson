# Error Handling

BinJson exposes domain exceptions under `Krampus.BinJson.Error`.

The error contract is designed to be both human-friendly and machine-friendly.

## Base Contract

All domain exceptions inherit from `BJsonException` and include:

- `ErrorCodeValue` (`BJsonErrorCode?`): typed enum code for programmatic handling.
- `ErrorCode` (`string?`): string representation (stable for logs and backwards compatibility).
- `DocumentPath` (`string?`): structured path to the failing node, when available (`$`, `$.user.name`, `$.items[2]`).
- `Details` (`IReadOnlyDictionary<string, object?>`): extra key-value diagnostic metadata.

## Error Code Catalog

The current official code enum is `BJsonErrorCode`.

### Parse Codes

- `ParseUnexpectedTrailingChar`
- `ParseUnexpectedEof`
- `ParseUnexpectedChar`
- `ParseInvalidNullLiteral`
- `ParseInvalidBoolLiteral`
- `ParseExpectedStringStart`
- `ParseUnexpectedEofInEscape`
- `ParseInvalidEscape`
- `ParseUnescapedControlChar`
- `ParseUnterminatedString`
- `ParseIncompleteUnicodeEscape`
- `ParseInvalidUnicodeEscape`
- `ParseInvalidNumber`
- `ParseInvalidFraction`
- `ParseInvalidExponent`
- `ParseInvalidFloat`
- `ParseNumberOutOfRange`
- `ParseExpectedArrayStart`
- `ParseUnexpectedEofInArray`
- `ParseExpectedArraySeparator`
- `ParseExpectedObjectStart`
- `ParseExpectedPropertyName`
- `ParseExpectedColon`
- `ParseDuplicateKey`
- `ParseUnexpectedEofInObject`
- `ParseExpectedObjectSeparator`
- `ParseUnterminatedBlockComment`

### Text I/O and Serialization Codes

- `TextReadParseError`
- `TextSerializationError`

### Binary Codes

- `BinaryFormatError`
- `BinarySerializationError`

### Generic Code

- `Unknown`

## Specialized Exception Metadata

Some exception types include additional typed fields:

- `BJsonParseException`: `Position`, `Line`, `Column`
- `BJsonBinaryFormatException`: `ByteOffset`, `Section`
- `BJsonSerializationException`: `ByteOffset`, `Operation`
- `BJsonDeserializationException`: `ByteOffset`, `Operation`
- `BJsonValidationException`: `ParameterName`
- `BJsonConverterException`: `ConverterType`, `TargetType`
- `BJsonMetadataException`: `RelatedType`, `MemberName`

## Suggested Usage

Use typed matching first, and optionally branch by `ErrorCodeValue`:

```csharp
try
{
    var value = BJson.Parse(input);
}
catch (BJsonParseException ex) when (ex.ErrorCodeValue == BJsonErrorCode.ParseUnexpectedChar)
{
    Console.WriteLine($"Unexpected token at {ex.DocumentPath} (line {ex.Line}, col {ex.Column}).");
}
catch (BJsonException ex)
{
    Console.WriteLine($"BinJson error ({ex.ErrorCode}): {ex.Message}");
}
```

## Stability Notes

- `ErrorCodeValue` is the recommended contract for code-level handling.
- `DocumentPath` is best effort and may be `null` when context is unavailable.
- `Details` keys are additive and may grow over time.
