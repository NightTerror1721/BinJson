using System.IO;
using Krampus.BinJson;
using UnityEngine;

public sealed class GameStateSaver : MonoBehaviour
{
    private void Start()
    {
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
        byte[] binary = BJson.SerializeToBytes(value);
        string json = BJson.Stringify(value);

        File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "save.bin"), binary);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, "save.json"), json);
    }
}
