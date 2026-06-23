using UnityEngine;

public enum AchievementType
{
    SurviveTime,
    TotalMoney,
    TotalConsumeMoney,
    CurrentMoney,
    TotalKill,
    TotalDeath,
    ReachPhase,
    SessionKill,
    PlayCount
}

public enum AchievementGrade
{
    Normal,
    Challenge
}

[CreateAssetMenu(fileName = "AchievementData", menuName = "AchievementData")]
public class AchievementData : ScriptableObject
{
    [SerializeField] private int _id;
    [SerializeField] private string _title;
    [SerializeField] private AchievementType _achievementType;
    [SerializeField] private AchievementGrade _grade;
    [SerializeField] private float _value = 1;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private bool _canUnlockInGame = false;
    [SerializeField] private string _targetStageName = "";

    public int Id => _id;
    public string Title => _title;
    public AchievementType AchievementType => _achievementType;
    public AchievementGrade Grade => _grade;
    public float Value => _value;
    public string Description => _description;
    public Sprite Icon => _icon;
    public bool CanUnlockInGame => _canUnlockInGame;
    public string TargetStageName => _targetStageName;

    public void InitForDebug(int id, string title, AchievementGrade grade, Sprite icon = null, string targetStageName = "")
    {
        _id = id;
        _title = title;
        _grade = grade;
        _icon = icon;
        _targetStageName = targetStageName;
    }
}
