; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
BJSON001 | BinJson.SourceGenerator | Warning | Invalid ExtensionData member type (must be IDictionary<string, BJsonValue>)
BJSON002 | BinJson.SourceGenerator | Error | Multiple constructors marked with [BJsonConstructor]
BJSON003 | BinJson.SourceGenerator | Error | Multiple members marked with [BJsonExtensionData]
BJSON004 | BinJson.SourceGenerator | Warning | Custom converter type not found
BJSON005 | BinJson.SourceGenerator | Warning | Conflicting JSON property names
BJSON006 | BinJson.SourceGenerator | Warning | Constructor parameter cannot be matched to member
BJSON007 | BinJson.SourceGenerator | Warning | Referenced attribute method not found on declaring type
BJSON008 | BinJson.SourceGenerator | Warning | Referenced attribute method is not accessible from generated code
BJSON009 | BinJson.SourceGenerator | Warning | Referenced attribute method has invalid signature
BJSON010 | BinJson.SourceGenerator | Warning | Unsupported type shape for source generation (generic or nested)
