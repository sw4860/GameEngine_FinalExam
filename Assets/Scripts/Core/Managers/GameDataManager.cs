using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance;

    public GameData CurrentData;
    public int SessionKillCount;
    public float SessionSurvivedTime;

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

    public void AddGold(int amount)
    {
        CurrentData.Gold += amount;
        Debug.Log($"골드 획득: {amount}. 현재 골드: {CurrentData.Gold}");
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
