using UnityEngine;

[System.Serializable]
public struct LobbyUpgradeLevelInfo
{
    public int Cost;
    public float Bonus;
}

[CreateAssetMenu(fileName = "LobbyUpgradeData", menuName = "Lobby/UpgradeData")]
public class LobbyUpgradeData : ScriptableObject
{
    [Header("Basic Info")]
    public StatType TargetStat; // 수정할 능력치 타입 (enum으로 중앙 집중 관리)
    public string UpgradeName;
    [TextArea] public string Description;
    public Sprite Icon;

    [Header("Display Format")]
    [Tooltip("수치를 퍼센트(%) 형식으로 표시할지 여부")]
    public bool IsPercentage = false;
    [Tooltip("UI에서 부호를 반대로 표시할지 여부 (예: Cooldown 0.1f -> -10%로 표시)")]
    public bool InvertSignInUI = false;

    [Header("Upgrade Levels")]
    public LobbyUpgradeLevelInfo[] Levels;

    public int MaxLevel => Levels != null ? Levels.Length : 0;

    public int GetCurrentLevel(GameData data)
    {
        if (data == null) return 0;

        switch (TargetStat)
        {
            case StatType.MaxHp:
                return data.MaxHpUpgrade;
            case StatType.Damage:
                return data.DamageUpgrade;
            case StatType.MagnetRadius:
                return data.MagnetUpgrade;
            case StatType.CooldownReduction:
                return data.CoolDownUpgrade;
            case StatType.ExpMultiplier:
                return data.ExpUpgrade;
            case StatType.MoveSpeed:
                return data.MoveSpeedUpgrade;
            default:
                return 0;
        }
    }

    public void SetCurrentLevel(GameData data, int level)
    {
        if (data == null) return;

        int clampedLevel = Mathf.Clamp(level, 0, MaxLevel);

        switch (TargetStat)
        {
            case StatType.MaxHp:
                data.MaxHpUpgrade = clampedLevel;
                break;
            case StatType.Damage:
                data.DamageUpgrade = clampedLevel;
                break;
            case StatType.MagnetRadius:
                data.MagnetUpgrade = clampedLevel;
                break;
            case StatType.CooldownReduction:
                data.CoolDownUpgrade = clampedLevel;
                break;
            case StatType.ExpMultiplier:
                data.ExpUpgrade = clampedLevel;
                break;
            case StatType.MoveSpeed:
                data.MoveSpeedUpgrade = clampedLevel;
                break;
        }
    }

    public int GetCostForNextLevel(int currentLevel)
    {
        if (Levels == null || currentLevel < 0 || currentLevel >= Levels.Length)
        {
            return -1;
        }
        return Levels[currentLevel].Cost;
    }

    public float GetBonusForLevel(int level)
    {
        if (level <= 0 || Levels == null || Levels.Length == 0) return 0f;
        int clampedLevel = Mathf.Clamp(level, 1, Levels.Length);
        return Levels[clampedLevel - 1].Bonus;
    }
}
