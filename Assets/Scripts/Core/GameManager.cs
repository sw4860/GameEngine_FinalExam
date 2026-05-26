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

    // 일시 정지나 백그라운드 전환 시에도 저장하도록 설정
    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveGame();
    }
}
