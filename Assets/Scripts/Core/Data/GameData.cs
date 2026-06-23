using System;
using System.Collections.Generic;

[Serializable]
public class AchievementProgressData
{
    public string Key;
    public float Value;
}

[Serializable]
public class GameData
{
    public int TotalKillCount = 0;
    public int TotalSpawnedCount = 0;
    public int Gold = 0;
    public int PlayCount = 0;
    public bool IsDamageTextEnabled = true;

    public int DamageUpgrade = 0;
    public int MaxHpUpgrade = 0;
    public int MagnetUpgrade = 0;
    public int CoolDownUpgrade = 0;
    public int ExpUpgrade = 0;
    public int MoveSpeedUpgrade = 0;

    public List<string> UnlockedCharacters = new();
    public List<string> UnlockedAchievements = new();
    public List<AchievementProgressData> AchievementProgressList = new();

    public string SelectedCharacterName = "";
    public string SelectedStageName = "";
}
