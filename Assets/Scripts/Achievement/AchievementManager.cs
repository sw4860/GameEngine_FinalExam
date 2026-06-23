using System;
using System.Collections.Generic;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    private static AchievementManager _instance;
    public static AchievementManager Instance => _instance;

    public List<AchievementData> AchievementDataList => _achievementDataList;

    private readonly List<AchievementData> _achievementDataList = new List<AchievementData>();
    private readonly Dictionary<AchievementType, List<AchievementData>> _achievementGroups = new Dictionary<AchievementType, List<AchievementData>>();
    private readonly Dictionary<string, float> _achievementProgressMap = new Dictionary<string, float>();
    private readonly HashSet<string> _unlockedAchievements = new HashSet<string>();
    private readonly Queue<AchievementData> _pendingInGameUnlocks = new Queue<AchievementData>();
    private readonly Dictionary<string, int> _nameToIdMap = new Dictionary<string, int>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAchievementsFromResources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadProgress();
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentData != null)
        {
            UpdateProgress(AchievementType.CurrentMoney, GameDataManager.Instance.CurrentData.Gold);
            UpdateProgress(AchievementType.TotalKill, GameDataManager.Instance.CurrentData.TotalKillCount);
            UpdateProgress(AchievementType.PlayCount, GameDataManager.Instance.CurrentData.PlayCount);
        }
    }

    private void Update()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameScene")
        {
            if (PlayerStats.Instance != null && PlayerStats.Instance.CurrentHp > 0)
            {
                float surviveTime = StageManager.Instance != null ? StageManager.Instance.ElapsedTime : Time.timeSinceLevelLoad;
                UpdateProgress(AchievementType.SurviveTime, surviveTime);
            }
        }
    }

    private void OnEnable()
    {
        EventManager.OnEnemyDeath += HandleEnemyDeath;
        EventManager.OnPlayerDeath += HandlePlayerDeath;
        EventManager.OnGameClear += HandleGameClear;
        EventManager.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        EventManager.OnEnemyDeath -= HandleEnemyDeath;
        EventManager.OnPlayerDeath -= HandlePlayerDeath;
        EventManager.OnGameClear -= HandleGameClear;
        EventManager.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void LoadAchievementsFromResources()
    {
        AchievementData[] dataArray = Resources.LoadAll<AchievementData>("SO/AchievementDatas");
        if (dataArray == null) return;

        _achievementDataList.AddRange(dataArray);
        _achievementDataList.Sort((a, b) => a.Id.CompareTo(b.Id));

        _nameToIdMap.Clear();
        foreach (var data in _achievementDataList)
        {
            if (!_nameToIdMap.ContainsKey(data.name))
            {
                _nameToIdMap[data.name] = data.Id;
            }

            if (!_achievementGroups.ContainsKey(data.AchievementType))
            {
                _achievementGroups[data.AchievementType] = new List<AchievementData>();
            }
            _achievementGroups[data.AchievementType].Add(data);
        }
    }

    private string GetAchievementKey(AchievementData data)
    {
        return data.Id > 0 ? data.Id.ToString() : data.name;
    }

    private void HandleEnemyDeath()
    {
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentData != null)
        {
            UpdateProgress(AchievementType.TotalKill, GameDataManager.Instance.CurrentData.TotalKillCount);
            UpdateProgress(AchievementType.SessionKill, GameDataManager.Instance.SessionKillCount);
        }
    }

    private void HandlePlayerDeath()
    {
        float surviveTime = StageManager.Instance != null ? StageManager.Instance.ElapsedTime : Time.timeSinceLevelLoad;
        UpdateProgress(AchievementType.SurviveTime, surviveTime);
        UpdateProgress(AchievementType.TotalDeath, 1f);
        TriggerAllPendingUnlocks();
    }

    private void HandleGameClear()
    {
        float surviveTime = StageManager.Instance != null ? StageManager.Instance.ElapsedTime : Time.timeSinceLevelLoad;
        UpdateProgress(AchievementType.SurviveTime, surviveTime);
        TriggerAllPendingUnlocks();
    }

    private void HandlePhaseChanged()
    {
        if (StageManager.Instance == null || StageManager.Instance.StageData == null) return;

        string currentStage = StageManager.Instance.StageData.StageName;
        int reachedPhase = StageManager.Instance.currentPhase;

        UpdatePhaseProgress(currentStage, reachedPhase);
    }

    public void UpdatePhaseProgress(string stageName, int phase)
    {
        if (!_achievementGroups.TryGetValue(AchievementType.ReachPhase, out var list)) return;

        foreach (var data in list)
        {
            string key = GetAchievementKey(data);
            if (_unlockedAchievements.Contains(key)) continue;

            if (!string.IsNullOrEmpty(data.TargetStageName) && data.TargetStageName != stageName) continue;

            float next = phase;
            _achievementProgressMap[key] = next;

            if (next >= data.Value)
            {
                UnlockAchievement(data);
            }
        }
    }

    public void UpdateProgress(AchievementType type, float amount)
    {
        if (!_achievementGroups.TryGetValue(type, out var list)) return;

        foreach (var data in list)
        {
            string key = GetAchievementKey(data);
            if (_unlockedAchievements.Contains(key)) continue;

            _achievementProgressMap.TryGetValue(key, out float current);
            float next = (type == AchievementType.SurviveTime || type == AchievementType.CurrentMoney || type == AchievementType.TotalKill || type == AchievementType.SessionKill || type == AchievementType.PlayCount) ? amount : current + amount;
            _achievementProgressMap[key] = next;

            if (next >= data.Value)
            {
                UnlockAchievement(data);
            }
        }
    }

    private void UnlockAchievement(AchievementData data)
    {
        string key = GetAchievementKey(data);
        if (!_unlockedAchievements.Contains(key))
        {
            _unlockedAchievements.Add(key);
            SaveProgress();

            bool shouldUnlockImmediately = data.CanUnlockInGame || 
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GameScene";

            if (shouldUnlockImmediately)
            {
                EventManager.OnAchievementUnlocked?.Invoke(data);
            }
            else
            {
                _pendingInGameUnlocks.Enqueue(data);
            }
        }
    }

    public void TriggerAllPendingUnlocks()
    {
        while (_pendingInGameUnlocks.Count > 0)
        {
            AchievementData data = _pendingInGameUnlocks.Dequeue();
            EventManager.OnAchievementUnlocked?.Invoke(data);
        }
    }

    public void SyncProgressToGameData()
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentData == null) return;

        var currentData = GameDataManager.Instance.CurrentData;

        UpdateProgress(AchievementType.TotalKill, currentData.TotalKillCount);
        UpdateProgress(AchievementType.PlayCount, currentData.PlayCount);

        currentData.UnlockedAchievements.Clear();
        currentData.UnlockedAchievements.AddRange(_unlockedAchievements);

        currentData.AchievementProgressList.Clear();
        foreach (var pair in _achievementProgressMap)
        {
            float val = IsSessionKillAchievement(pair.Key) ? 0f : pair.Value;
            currentData.AchievementProgressList.Add(new AchievementProgressData
            {
                Key = pair.Key,
                Value = val
            });
        }
    }

    private void SaveProgress()
    {
        SyncProgressToGameData();
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SaveGame();
        }
    }

    public void LoadProgress()
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentData == null) return;

        var currentData = GameDataManager.Instance.CurrentData;
        bool needsMigrationSave = false;

        _unlockedAchievements.Clear();
        foreach (var originalKey in currentData.UnlockedAchievements)
        {
            string key = originalKey;
            if (_nameToIdMap.TryGetValue(originalKey, out int id) && id > 0)
            {
                key = id.ToString();
                needsMigrationSave = true;
            }
            _unlockedAchievements.Add(key);
        }

        _achievementProgressMap.Clear();
        foreach (var progress in currentData.AchievementProgressList)
        {
            string key = progress.Key;
            if (_nameToIdMap.TryGetValue(progress.Key, out int id) && id > 0)
            {
                key = id.ToString();
                needsMigrationSave = true;
            }
            _achievementProgressMap[key] = IsSessionKillAchievement(key) ? 0f : progress.Value;
        }

        if (needsMigrationSave)
        {
            SaveProgress();
        }
    }

    private bool IsSessionKillAchievement(string key)
    {
        if (_achievementGroups.TryGetValue(AchievementType.SessionKill, out var list))
        {
            foreach (var data in list)
            {
                if (GetAchievementKey(data) == key)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void ResetSessionKillProgress()
    {
        if (_achievementGroups.TryGetValue(AchievementType.SessionKill, out var list))
        {
            foreach (var data in list)
            {
                string key = GetAchievementKey(data);
                if (_unlockedAchievements.Contains(key)) continue;

                _achievementProgressMap[key] = 0f;
            }
        }
    }
}

