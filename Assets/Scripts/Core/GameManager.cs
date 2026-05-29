using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameData CurrentData;
    public int SessionKillCount;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }

        EventManager.OnEnemyDeath += AddKill;
    }

    public void SaveGame()
    {
        SaveManager.Save(CurrentData);
    }

    public void LoadGame()
    {
        CurrentData = SaveManager.Load();
    }

    public void AddKill()
    {
        CurrentData.TotalKillCount++;
        SessionKillCount++;
    }

    public void AddSpawn()
    {
        CurrentData.TotalSpawnedCount++;
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveGame();
    }

    void OnDestroy()
    {
        EventManager.OnEnemyDeath -= AddKill;
    }
}
