using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PhaseData
{
    public float RequiredTime;
    public int maxEnemyCount;
    public EnemyData[] enemyDatas;
}

[CreateAssetMenu(fileName = "StageData", menuName = "StageData")]
public class StageData : ScriptableObject
{
    public PhaseData[] phaseDatas;
    public float EndTime = 300.0f;
}
