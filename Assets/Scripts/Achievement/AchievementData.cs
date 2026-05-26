using UnityEngine;

public enum AchievementType
{
    SurviveTime,
    TotalMoney,
    TotalConsumeMoney,
    CurrentMoney,
    TotalKill,
    TotalDeath
}

[CreateAssetMenu(fileName = "AchievementData", menuName = "AchievementData")]
public class AchievementData : ScriptableObject
{
    public AchievementType AchievementType;
    public float Value;
}
