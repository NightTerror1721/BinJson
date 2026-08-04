# Examples

This folder contains small example projects that demonstrate common BinJson scenarios.

## Included examples

- `GameStateSerialization/` - serializing gameplay state objects into `BJsonValue`, binary, and JSON text; useful as the baseline end-to-end flow sample.
- `ApiDtos/` - attribute-driven DTO contracts, custom converters, naming overrides, and typical API-facing object mapping.
- `ConfigurationFiles/` - preserving unknown settings with extension data and showing how configuration payloads can evolve safely over time.
- `PerformanceComparison/` - benchmark matrix for DOM, text, binary, async paths, preprocessor scenarios, external references, and CLR reflection vs source-generated serializers.
- `UnityIntegrationSample/` - Unity-oriented sample scripts and usage notes for save files and runtime integration.

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

## Suggested Reading Order

1. Start with `GameStateSerialization/` if you want the most direct overview of BinJson usage.
2. Continue with `ApiDtos/` if your project uses CLR object contracts and serializer attributes.
3. Review `ConfigurationFiles/` if you need forward compatibility or pass-through configuration behavior.
4. Run `PerformanceComparison/` when you need throughput or allocation baselines for your workload.
