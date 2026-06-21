using System;

public class EventManager
{
    public static Action OnGameClear;
    public static Action OnPhaseChanged;
    public static Action OnEnemyDeath;
    public static Action OnPlayerDeath;
    public static Action OnPlayerHpChanged;
    public static Action<int> OnLevelUp;
    public static Action<AchievementData> OnAchievementUnlocked;
}
