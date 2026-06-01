using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatData", menuName = "Player/StatData")]
public class PlayerStatData : ScriptableObject
{
    [Header("Base Stats")]
    public float BaseMoveSpeed = 5f;
    public float BaseDamage = 10f;
    public float BaseExpMultiplier = 1f;
    public float BaseMagnetRadius = 3f;
    public float BaseCooldownReduction = 0f;

    [Header("Runtime Modifiers (Percentages)")]
    public float MoveSpeedMultiplier = 1f;
    public float DamageMultiplier = 1f;
    public float ExpMultiplier = 1f;
    public float CooldownReduction = 0f;
    public float MagnetRadiusBonus = 0f;

    [Header("Runtime Modifiers (Additions)")]
    public float AdditionalDamage = 0f;
    public float AdditionalMoveSpeed = 0f;

    public float CurrentMoveSpeed => (BaseMoveSpeed + AdditionalMoveSpeed) * MoveSpeedMultiplier;
    public float CurrentDamage => (BaseDamage + AdditionalDamage) * DamageMultiplier;
    public float CurrentExpMultiplier => BaseExpMultiplier * ExpMultiplier;
    public float CurrentMagnetRadius => BaseMagnetRadius + MagnetRadiusBonus;
    public float CurrentCooldownReduction => Mathf.Clamp(BaseCooldownReduction + CooldownReduction, 0f, 0.9f);

    public void ResetModifiers()
    {
        MoveSpeedMultiplier = 1f;
        DamageMultiplier = 1f;
        ExpMultiplier = 1f;
        CooldownReduction = 0f;
        MagnetRadiusBonus = 0f;
        AdditionalDamage = 0f;
        AdditionalMoveSpeed = 0f;
    }

    public PlayerStatData Clone()
    {
        PlayerStatData clone = CreateInstance<PlayerStatData>();
        clone.BaseMoveSpeed = BaseMoveSpeed;
        clone.BaseDamage = BaseDamage;
        clone.BaseExpMultiplier = BaseExpMultiplier;
        clone.BaseMagnetRadius = BaseMagnetRadius;
        clone.BaseCooldownReduction = BaseCooldownReduction;
        clone.ResetModifiers();
        return clone;
    }
}
