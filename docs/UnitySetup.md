# Unity Setup Guide

This guide explains how to integrate **BinJson** into your Unity project.

## Overview

BinJson is a compact JSON DOM and binary serialization library designed to work with Unity. It has **no external dependencies** and targets **.NET Standard 2.1**, making it compatible with modern Unity versions (2021.2+).

---

## Installation

### Option 1: Copy Source Files (Recommended)

1. **Copy the `BinJson` folder** from this repository into your Unity project:
   ```
   <YourUnityProject>/Assets/Scripts/BinJson/
   ```

2. **Verify the folder structure**:
   ```
   Assets/
   └── Scripts/
	   └── BinJson/
		   ├── BJsonValue.cs
		   ├── BJsonArray.cs
		   ├── BJsonObject.cs
		   ├── BJsonBinary.cs
		   ├── BJson.cs
		   ├── Binary/
		   │   ├── BJsonBinaryWriter.cs
		   │   └── BJsonBinaryReader.cs
		   └── Text/
			   ├── BJsonTextWriter.cs
			   ├── BJsonTextReader.cs
			   ├── BJsonTextWriterOptions.cs
			   ├── BJsonTextReaderOptions.cs
			   └── JsonTextParser.cs
   ```

3. **Wait for Unity to recompile** the code.

### Option 2: Unity Package Manager (UPM)

If you are using a Git repository, you can add BinJson via UPM:

1. Open `Packages/manifest.json` in your Unity project.
2. Add this line to the `dependencies` section:
   ```json
   "com.nightterror1721.binjson": "https://github.com/NightTerror1721/BinJson.git"
   ```
3. Unity will automatically fetch and compile the package.

---

## Unity Version Requirements

| Unity Version  | Support Status                                      |
|----------------|-----------------------------------------------------|
| **2021.2+**    | ✅ Full support (Span<T>, .NET Standard 2.1)       |
| **2020.3 LTS** | ⚠️ Partial support (BJsonBinary.AsSpan may fail)   |
| **2019.4 LTS** | ❌ Not supported (.NET Standard 2.0 only)          |

> **Note:** If you encounter errors related to `Span<T>`, your Unity version may not fully support .NET Standard 2.1. Consider upgrading to Unity 2021.2 or later.

---

## API Level Settings

Ensure your Unity project is configured to use **.NET Standard 2.1**:

1. Go to **Edit > Project Settings > Player**.
2. Under **Other Settings**, find **Api Compatibility Level**.
3. Set it to **.NET Standard 2.1**.

---

## Basic Usage in Unity

### Example 1: Serialize Game State

```csharp
using UnityEngine;
using Krampus.BinJson;
using System.IO;

public class GameStateSaver : MonoBehaviour
{
	void Start()
	{
		// Create a game state object
		var gameState = new BJsonObject
		{
			["playerName"] = BJsonValue.Create("Hero"),
			["health"] = BJsonValue.Create(100),
			["position"] = BJsonValue.Create(new BJsonArray
			{
				transform.position.x,
				transform.position.y,
				transform.position.z
			})
		};

		var value = BJsonValue.Create(gameState);

		// Save as compact binary
		byte[] binaryData = BJson.SerializeToBytes(value);
		File.WriteAllBytes(Application.persistentDataPath + "/save.bin", binaryData);

		// Or save as readable JSON text
		string jsonText = BJson.Stringify(value);
		File.WriteAllText(Application.persistentDataPath + "/save.json", jsonText);

		Debug.Log("Game state saved!");
	}
}
```

### Example 2: Load Game State

```csharp
using UnityEngine;
using Krampus.BinJson;
using System.IO;

public class GameStateLoader : MonoBehaviour
{
	void Start()
	{
		string binaryPath = Application.persistentDataPath + "/save.bin";

		if (File.Exists(binaryPath))
		{
			byte[] binaryData = File.ReadAllBytes(binaryPath);
			var value = BJson.Deserialize(binaryData);

			if (value.IsObject)
			{
				var gameState = value.ObjectValue;

				if (gameState.TryGetString("playerName", out string playerName))
					Debug.Log("Player: " + playerName);

				if (gameState.TryGetInt("health", out int health))
					Debug.Log("Health: " + health);

				if (gameState.TryGetValue("position", out var posValue) && posValue.IsArray)
				{
					var posArray = posValue.ArrayValue;
					if (posArray.Count == 3)
					{
						float x = (float)posArray[0].DoubleValue;
						float y = (float)posArray[1].DoubleValue;
						float z = (float)posArray[2].DoubleValue;
						transform.position = new Vector3(x, y, z);
						Debug.Log($"Position restored: ({x}, {y}, {z})");
					}
				}
			}
		}
	}
}
```

### Example 3: Pretty-Print Config Files

```csharp
using UnityEngine;
using Krampus.BinJson;
using Krampus.BinJson.Text;
using System.IO;

public class ConfigWriter : MonoBehaviour
{
	void Start()
	{
		var config = new BJsonObject
		{
			["graphics"] = BJsonValue.Create(new BJsonObject
			{
				["quality"] = BJsonValue.Create("High"),
				["vsync"] = BJsonValue.True,
				["resolution"] = BJsonValue.Create(new BJsonArray { 1920, 1080 })
			}),
			["audio"] = BJsonValue.Create(new BJsonObject
			{
				["masterVolume"] = BJsonValue.Create(0.8),
				["musicVolume"] = BJsonValue.Create(0.6)
			})
		};

		var value = BJsonValue.Create(config);
		var options = new BJsonTextWriterOptions { Indented = true, IndentSize = 2 };

		string prettyJson = BJsonTextWriter.Serialize(value, options);
		File.WriteAllText(Application.dataPath + "/config.json", prettyJson);

		Debug.Log("Config written:\n" + prettyJson);
	}
}
```

---

## Features

✅ **No external dependencies** (no `System.Text.Json` required)  
✅ **Binary format** for compact save files and network payloads  
✅ **JSON text format** with pretty-print support for human-readable configs  
✅ **Binary values prohibited by default** in JSON text (opt-in with `AllowBinaryAsBase64 = true`)  
✅ **Structural equality** for deep comparisons  
✅ **Compact memory layout**: `BJsonValue` is only 16 bytes  

---

## Limitations

⚠️ **Span<T>**: `BJsonBinary.AsSpan()` requires Unity 2021.2+ or .NET Standard 2.1 full support.  
⚠️ **No async I/O**: Serialization/deserialization are synchronous. Use Unity coroutines or background threads if needed.  
⚠️ **No schema validation**: BinJson is a DOM library, not a schema validator.  

---

## Performance Tips

1. **Use binary format** for save files and network payloads (smaller and faster).
2. **Pre-size collections** when you know the final count:
   ```csharp
   var array = new BJsonArray(capacity: 100);
   var obj = new BJsonObject(capacity: 50);
   ```
3. **Avoid deep nesting** (especially in structural equality checks).
4. **Cache serialized data** if you serialize the same value multiple times.

---

## Troubleshooting

### Error: "Span<T> not found"
- **Solution**: Upgrade to Unity 2021.2+ or change API Compatibility Level to .NET Standard 2.1.

### Error: "System.Text.Json dependency missing"
- **Solution**: This should not happen. BinJson has no external dependencies. Verify you copied all source files.

### Serialization throws "Binary values are not allowed"
- **Solution**: If you need to serialize `BJsonBinary` to JSON text, use:
  ```csharp
  var options = new BJsonTextWriterOptions { AllowBinaryAsBase64 = true };
  string json = BJsonTextWriter.Serialize(value, options);
  ```

---

## Support

- 📖 **Documentation**: [README.md](../README.md) | [BinaryFormat.md](BinaryFormat.md)  
- 🐛 **Issues**: [GitHub Issues](https://github.com/NightTerror1721/BinJson/issues)  
- 💬 **Discussions**: [GitHub Discussions](https://github.com/NightTerror1721/BinJson/discussions)  

---

## License

BinJson is licensed under the **MIT License**. See [LICENSE](../LICENSE) for details.
