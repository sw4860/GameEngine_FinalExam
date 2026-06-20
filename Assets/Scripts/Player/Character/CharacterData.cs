using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Player/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string CharacterName;
    public Sprite CharacterIcon;
    public RuntimeAnimatorController AnimatorController;
    public PlayerStatData BaseStats;
    public SkillData BaseSkill;
    public int MaxSkillSlots = 6;

    [Header("Unlock Settings")]
    public bool IsDefaultUnlocked = false;
    public int UnlockGoldCost = 0;
    public string UnlockAchievementId = ""; // 비어있지 않으면 해당 업적이 해금되었을 때만 사용 가능
}
