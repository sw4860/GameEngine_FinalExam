using System;
using System.Collections.Generic;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    private static AchievementManager _instance;
    public static AchievementManager Instance => _instance;

    private readonly List<AchievementData> _achievementDataList = new List<AchievementData>();
    private readonly Dictionary<AchievementType, List<AchievementData>> _achievementGroups = new Dictionary<AchievementType, List<AchievementData>>();
    private readonly Dictionary<string, float> _achievementProgressMap = new Dictionary<string, float>();
    private readonly HashSet<string> _unlockedAchievements = new HashSet<string>();
    private readonly Queue<AchievementData> _pendingInGameUnlocks = new Queue<AchievementData>();

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
        }
    }

    private void OnEnable()
    {
        EventManager.OnEnemyDeath += HandleEnemyDeath;
        EventManager.OnPlayerDeath += HandlePlayerDeath;
        EventManager.OnGameClear += HandleGameClear;
    }

    private void OnDisable()
    {
        EventManager.OnEnemyDeath -= HandleEnemyDeath;
        EventManager.OnPlayerDeath -= HandlePlayerDeath;
        EventManager.OnGameClear -= HandleGameClear;
    }

    private void LoadAchievementsFromResources()
    {
        AchievementData[] dataArray = Resources.LoadAll<AchievementData>("SO/AchievementDatas");
        if (dataArray == null) return;

        _achievementDataList.AddRange(dataArray);

        foreach (var data in dataArray)
        {
            if (!_achievementGroups.ContainsKey(data.AchievementType))
            {
                _achievementGroups[data.AchievementType] = new List<AchievementData>();
            }
            _achievementGroups[data.AchievementType].Add(data);
        }
    }

    private void HandleEnemyDeath()
    {
        UpdateProgress(AchievementType.TotalKill, 1f);
    }

    private void HandlePlayerDeath()
    {
        UpdateProgress(AchievementType.TotalDeath, 1f);
        TriggerAllPendingUnlocks();
    }

    private void HandleGameClear()
    {
        UpdateProgress(AchievementType.SurviveTime, Time.timeSinceLevelLoad);
        TriggerAllPendingUnlocks();
    }

    public void UpdateProgress(AchievementType type, float amount)
    {
        if (!_achievementGroups.TryGetValue(type, out var list)) return;

        foreach (var data in list)
        {
            string key = data.name;
            if (_unlockedAchievements.Contains(key)) continue;

            _achievementProgressMap.TryGetValue(key, out float current);
            float next = (type == AchievementType.SurviveTime || type == AchievementType.CurrentMoney) ? amount : current + amount;
            _achievementProgressMap[key] = next;

#if UNITY_EDITOR
            Debug.Log($"[업적 진행] {data.Title} - 진행 상태: {next} / {data.Value}");
#endif

            if (next >= data.Value)
            {
                UnlockAchievement(data);
            }
        }
    }

    private void UnlockAchievement(AchievementData data)
    {
        string key = data.name;
        if (!_unlockedAchievements.Contains(key))
        {
            _unlockedAchievements.Add(key);
            SaveProgress();

#if UNITY_EDITOR
            Debug.Log($"[업적 완료] {data.Title} 해금 완료 (등급: {data.Grade})");
#endif

            bool shouldUnlockImmediately = data.CanUnlockInGame || 
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GameScene";

            if (shouldUnlockImmediately)
            {
#if UNITY_EDITOR
                Debug.Log($"[업적 이벤트 발송] {data.Title} - 즉시 해금 연출 이벤트 트리거 (CanUnlockInGame: {data.CanUnlockInGame}, Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name})");
#endif
                EventManager.OnAchievementUnlocked?.Invoke(data);
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log($"[업적 보류 등록] {data.Title} - 인게임 내 보류 대상이므로 펜딩 큐에 임시 대기");
#endif
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

    private void SaveProgress()
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentData == null) return;

        var currentData = GameDataManager.Instance.CurrentData;

        currentData.UnlockedAchievements.Clear();
        currentData.UnlockedAchievements.AddRange(_unlockedAchievements);

        currentData.AchievementProgressList.Clear();
        foreach (var pair in _achievementProgressMap)
        {
            currentData.AchievementProgressList.Add(new AchievementProgressData
            {
                Key = pair.Key,
                Value = pair.Value
            });
        }

        GameDataManager.Instance.SaveGame();
    }

    public void LoadProgress()
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentData == null) return;

        var currentData = GameDataManager.Instance.CurrentData;

        _unlockedAchievements.Clear();
        foreach (var key in currentData.UnlockedAchievements)
        {
            _unlockedAchievements.Add(key);
        }

        _achievementProgressMap.Clear();
        foreach (var progress in currentData.AchievementProgressList)
        {
            _achievementProgressMap[progress.Key] = progress.Value;
        }
    }

#if UNITY_EDITOR
    public void DebugPrintAllStatus()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== [업적 진행 상태] ===");
        foreach (var data in _achievementDataList)
        {
            string key = data.name;
            bool isUnlocked = _unlockedAchievements.Contains(key);
            _achievementProgressMap.TryGetValue(key, out float progress);
            sb.AppendLine($"- [{data.Title}] 해금 여부: {(isUnlocked ? "해금 완료" : "잠김")} | 진행 수치: {progress} / {data.Value} (타입: {data.AchievementType})");
        }
        sb.AppendLine("=======================");
        Debug.Log(sb.ToString());
    }
#endif
}

