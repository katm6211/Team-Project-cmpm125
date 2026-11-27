using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public Transform player;
    float playTime;

    void Start()
    {
        // Auto-load if a save exists
        if (SaveSystem.HasSave()) LoadGame();
    }

    void Update()
    {
        playTime += Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.F5)) SaveGame();   // quick save
        if (Input.GetKeyDown(KeyCode.F9)) LoadGame();   // quick load
    }

    public void SaveGame()
    {
        SaveData data = new SaveData
        {
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            playerPosition = player.position,
            playTime = playTime
        };
        SaveSystem.Save(data);
    }

    public void LoadGame()
    {
        SaveData data = SaveSystem.Load();
        if (data == null) return;

        // CharacterController overrides position unless disabled first
        var cc = player.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        player.position = data.playerPosition;
        if (cc) cc.enabled = true;

        playTime = data.playTime;
    }

    void OnApplicationQuit()
    {
        SaveGame(); // auto-save when the game closes
    }
}