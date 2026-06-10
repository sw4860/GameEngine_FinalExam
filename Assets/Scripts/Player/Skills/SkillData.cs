using UnityEngine;
using System;

public enum StatType
{
    Damage,
    MoveSpeed,
    CooldownReduction,
    MagnetRadius,
    ExpMultiplier
}

public enum ModifierMode
{
    Flat,
    Percent
}

[Serializable]
public class BaseStatModifier
{
    public StatType Type;
    public float Value;
    public ModifierMode Mode;
    public bool ReverseSign;
}

public abstract class SkillData : ScriptableObject
{
    [Header("Common Info")]
    public string SkillName;
    [TextArea] public string Description;
    public Sprite Icon;

    [HideInInspector] public int CurrentLevel = 0;

    public abstract int MaxLevel { get; }
    public bool IsMaxLevel => CurrentLevel >= MaxLevel;

    public virtual void OnEquip(GameObject owner) 
    { 
        CurrentLevel = 1; 
    }

    public virtual void OnUpdate(GameObject owner) { }

    public virtual void OnLevelUp(GameObject owner) 
    { 
        if (CurrentLevel < MaxLevel) { CurrentLevel++; }
    }
    
    public abstract string GetLevelUpDescription();

    protected string GetModifierDescription(BaseStatModifier modifier)
    {
        if (modifier == null) return string.Empty;

        string statName = modifier.Type switch
        {
            StatType.Damage => "공격력",
            StatType.MoveSpeed => "이동 속도",
            StatType.CooldownReduction => "재사용 대기시간",
            StatType.MagnetRadius => "아이템 획득 범위",
            StatType.ExpMultiplier => "경험치 획득량",
            _ => modifier.Type.ToString()
        };

        float displayVal = modifier.ReverseSign ? -modifier.Value : modifier.Value;
        string sign = displayVal > 0 ? "+" : "";
        string valueStr = modifier.Mode == ModifierMode.Percent ? $"{displayVal * 100}%" : displayVal.ToString();

        return $"{statName} {sign}{valueStr}";
    }

    protected void ApplyStatModifiers(BaseStatModifier[] modifiers)
    {
        if (modifiers == null || PlayerStats.Instance == null || PlayerStats.Instance.StatData == null) return;
        
        var stats = PlayerStats.Instance.StatData;
        foreach (var modifier in modifiers)
        {
            if (modifier.Mode == ModifierMode.Percent)
            {
                switch (modifier.Type)
                {
                    case StatType.Damage: stats.DamageMultiplier += modifier.Value; break;
                    case StatType.MoveSpeed: stats.MoveSpeedMultiplier += modifier.Value; break;
                    case StatType.ExpMultiplier: stats.ExpMultiplier += modifier.Value; break;
                    case StatType.CooldownReduction: stats.CooldownReduction += modifier.Value; break;
                    case StatType.MagnetRadius: stats.MagnetRadiusBonus += modifier.Value; break;
                }
            }
            else
            {
                switch (modifier.Type)
                {
                    case StatType.Damage: stats.AdditionalDamage += modifier.Value; break;
                    case StatType.MoveSpeed: stats.AdditionalMoveSpeed += modifier.Value; break;
                    case StatType.CooldownReduction: stats.CooldownReduction += modifier.Value; break;
                    case StatType.ExpMultiplier: stats.ExpMultiplier += modifier.Value; break;
                }
            }
        }
    }
}
