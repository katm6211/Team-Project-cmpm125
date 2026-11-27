using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SaveData
{
    public string sceneName;
    public Vector3 playerPosition;
    public int score;
    public float playTime;
}

public static class SaveSystem
{
    static string Path_ => Path.Combine(Application.persistentDataPath, "save.json");

    public static bool HasSave() => File.Exists(Path_);

    public static void Save(SaveData data)
    {
        File.WriteAllText(Path_, JsonUtility.ToJson(data, true));
        Debug.Log("Saved to " + Path_);
    }

    public static SaveData Load()
    {
        if (!HasSave()) return null;
        return JsonUtility.FromJson<SaveData>(File.ReadAllText(Path_));
    }

    public static void Delete()
    {
        if (HasSave()) File.Delete(Path_);
    }
}