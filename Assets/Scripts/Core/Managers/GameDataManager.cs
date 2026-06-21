using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance;

    public GameData CurrentData;
    public int SessionKillCount;
    public float SessionSurvivedTime;

    private System.IO.FileSystemWatcher _fileWatcher;
    private bool _needsReload;
    private float _lastSelfSaveTime;

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

    private void Start()
    {
        SetupFileWatcher();
    }

    private void SetupFileWatcher()
    {
        string dir = System.IO.Path.GetDirectoryName(SaveManager.SavePath);
        string fileName = System.IO.Path.GetFileName(SaveManager.SavePath);

        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        _fileWatcher = new System.IO.FileSystemWatcher();
        _fileWatcher.Path = dir;
        _fileWatcher.Filter = fileName;
        _fileWatcher.NotifyFilter = System.IO.NotifyFilters.LastWrite;
        _fileWatcher.Changed += OnSaveFileChanged;
        _fileWatcher.EnableRaisingEvents = true;
    }

    private void OnSaveFileChanged(object sender, System.IO.FileSystemEventArgs e)
    {
        _needsReload = true;
    }

    private void Update()
    {
        if (_needsReload)
        {
            _needsReload = false;
            if (Time.realtimeSinceStartup - _lastSelfSaveTime < 0.5f)
            {
                return;
            }
            StartCoroutine(ReloadGameWithDelay());
        }
    }

    private System.Collections.IEnumerator ReloadGameWithDelay()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        LoadGame();
    }

    public void SaveGame()
    {
        _lastSelfSaveTime = Time.realtimeSinceStartup;
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.SyncProgressToGameData();
        }
        SaveManager.Save(CurrentData);
    }

    public void LoadGame()
    {
        try
        {
            CurrentData = SaveManager.Load();
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.LoadProgress();
            }
            EventManager.OnGameDataReloaded?.Invoke();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Failed to read JSON save file (possibly locked by external process): {ex.Message}");
        }
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
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.UpdateProgress(AchievementType.TotalMoney, amount);
            AchievementManager.Instance.UpdateProgress(AchievementType.CurrentMoney, CurrentData.Gold);
        }
    }

    public void ConsumeGold(int amount)
    {
        CurrentData.Gold -= amount;
        Debug.Log($"골드 소비: {amount}. 현재 골드: {CurrentData.Gold}");
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.UpdateProgress(AchievementType.TotalConsumeMoney, amount);
            AchievementManager.Instance.UpdateProgress(AchievementType.CurrentMoney, CurrentData.Gold);
        }
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
        if (_fileWatcher != null)
        {
            _fileWatcher.Changed -= OnSaveFileChanged;
            _fileWatcher.Dispose();
        }
    }
}
