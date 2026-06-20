using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BossData
{
    public EnemyData EnemyData;
    public int count;
}

[System.Serializable]
public class PhaseData
{
    public float RequiredTime;
    public int maxEnemyCount;
    public float SpawnInterval;
    public EnemyData[] enemyDatas;
    public BossData[] bossData;
}

[CreateAssetMenu(fileName = "StageData", menuName = "StageData")]
public class StageData : ScriptableObject
{
    [Header("Lobby Settings")]
    public string StageName = "New Stage";
    [TextArea] public string Description;
    public Sprite Thumbnail;

    [Header("Stage Settings")]
    public PhaseData[] phaseDatas;
    public float EndTime = 300.0f;
}
