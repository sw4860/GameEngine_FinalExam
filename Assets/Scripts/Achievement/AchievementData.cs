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
    public float Value = 1;
    public string Description;
    public Sprite Icon;
    public bool CanUnlockInGame = false;
}
