# Unity Integration Sample

This sample is intentionally documentation-only because it uses `UnityEngine` types that are supplied by the Unity editor.

## Files

- `GameStateSaver.cs` - saves a simple game state as BinJson binary and JSON text.

## What the sample demonstrates

- Creating a DOM payload from Unity runtime values.
- Writing compact binary save data for production use.
- Writing JSON text save data for debugging and inspection.
- Using `Application.persistentDataPath` as the storage location.

## Usage

1. Copy the `BinJson/` folder into your Unity project.
2. Copy `GameStateSaver.cs` into one of your Unity scripts folders.
3. Open a scene with a GameObject that uses the component.
4. Run the scene and inspect the generated save files in `Application.persistentDataPath`.

## Expected output

After the component runs, the sample writes:

- one binary save file suitable for compact storage
- one JSON text file suitable for manual inspection while iterating in the editor

For full setup instructions, see `docs/UnitySetup.md`.
