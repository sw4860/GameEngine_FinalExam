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
}
