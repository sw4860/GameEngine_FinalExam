using System.IO;
using UnityEngine;

public static class SaveManager
{
    public static string SavePath => Path.Combine(Application.persistentDataPath, "game_data.json");

    public static void Save(GameData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }

    public static GameData Load()
    {
        if (!File.Exists(SavePath))
        {
            return new GameData();
        }

        string json = File.ReadAllText(SavePath);
        return JsonUtility.FromJson<GameData>(json);
    }
}
