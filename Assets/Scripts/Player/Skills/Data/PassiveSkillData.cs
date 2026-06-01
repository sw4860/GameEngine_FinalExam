using UnityEngine;
using System;

[CreateAssetMenu(fileName = "PassiveSkillData", menuName = "Skills/PassiveSkillData", order = 1)]
public class PassiveSkillData : SkillData
{
    [Serializable]
    public class LevelModifier : BaseStatModifier
    {
        [TextArea] public string LevelDescription;
    }

    [Header("Passive Skill Settings")]
    public LevelModifier[] Levels;

    public override int MaxLevel => Levels.Length;
    public override void OnEquip(GameObject owner)
    {
        base.OnEquip(owner);
        ApplyLevelEffect(owner);
    }

    public override void OnLevelUp(GameObject owner)
    {
        base.OnLevelUp(owner);
        ApplyLevelEffect(owner);
    }

    private void ApplyLevelEffect(GameObject owner)
    {
        if (CurrentLevel > 0 && CurrentLevel <= Levels.Length)
        {
            ApplyStatModifiers(new BaseStatModifier[] { Levels[CurrentLevel - 1] });
        }
    }

    public override string GetLevelUpDescription()
    {
        if (IsMaxLevel) return "MAX LEVEL";

        string modifierDesc = GetModifierDescription(Levels[CurrentLevel]);
        string levelText;

        levelText = (CurrentLevel + 1 >= MaxLevel) ? $"{CurrentLevel} -> MAX" : $"{CurrentLevel} -> {CurrentLevel + 1}";

        return $"{modifierDesc}";
    }
}
