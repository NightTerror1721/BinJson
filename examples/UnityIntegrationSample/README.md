# Unity Integration Sample

This sample is intentionally documentation-only because it uses `UnityEngine` types that are supplied by the Unity editor.

## Files

- `GameStateSaver.cs` - saves a simple game state as BinJson binary and JSON text.

## Usage

1. Copy the `BinJson/` folder into your Unity project.
2. Copy `GameStateSaver.cs` into one of your Unity scripts folders.
3. Open a scene with a GameObject that uses the component.
4. Run the scene and inspect the generated save files in `Application.persistentDataPath`.

For full setup instructions, see `docs/UnitySetup.md`.
