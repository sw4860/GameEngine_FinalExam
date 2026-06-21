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

public enum AchievementGrade
{
    Normal,
    Challenge
}

[CreateAssetMenu(fileName = "AchievementData", menuName = "AchievementData")]
public class AchievementData : ScriptableObject
{
    [SerializeField] private string _title;
    [SerializeField] private AchievementType _achievementType;
    [SerializeField] private AchievementGrade _grade;
    [SerializeField] private float _value = 1;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private bool _canUnlockInGame = false;

    public string Title => _title;
    public AchievementType AchievementType => _achievementType;
    public AchievementGrade Grade => _grade;
    public float Value => _value;
    public string Description => _description;
    public Sprite Icon => _icon;
    public bool CanUnlockInGame => _canUnlockInGame;
}
