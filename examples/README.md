# Examples

This folder contains small example projects that demonstrate common BinJson scenarios.

## Included examples

- `GameStateSerialization/` - serializing gameplay state objects into `BJsonValue`, binary, and JSON text.
- `ApiDtos/` - attribute-driven DTO contracts and custom converters.
- `ConfigurationFiles/` - preserving unknown settings with extension data.
- `PerformanceComparison/` - benchmark matrix for DOM, text, binary, async paths, and CLR reflection vs source-generated serializers.
- `UnityIntegrationSample/` - Unity-oriented sample scripts and usage notes.

## Building the examples

Each `.csproj` example references the local `BinJson` project source.

From the repository root:

```powershell
dotnet build .\examples\GameStateSerialization\GameStateSerialization.csproj
dotnet build .\examples\ApiDtos\ApiDtos.csproj
dotnet build .\examples\ConfigurationFiles\ConfigurationFiles.csproj
dotnet build .\examples\PerformanceComparison\PerformanceComparison.csproj
```

The Unity sample is documentation-only because it depends on `UnityEngine` types provided by the Unity editor.
