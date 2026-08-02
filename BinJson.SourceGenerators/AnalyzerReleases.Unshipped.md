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
