using UnityEngine;
using System;

[CreateAssetMenu(fileName = "AutoAttackSkill", menuName = "Skills/AutoAttack")]
public class AutoAttackSkill : SkillData
{
    [Serializable]
    public struct AutoAttackLevel
    {
        public float Damage;
        public float AttackRange;
        public float Cooldown;
        public BaseStatModifier[] StatModifiers;
        [TextArea] public string LevelDescription;
    }

    [Header("AutoAttack Settings")]
    public AutoAttackLevel[] Levels;
    
    private float _timer;

    public override int MaxLevel => Levels.Length;
    private AutoAttackLevel CurrentLevelData => Levels[Mathf.Clamp(CurrentLevel - 1, 0, MaxLevel - 1)];

    public override void OnEquip(GameObject owner)
    {
        base.OnEquip(owner);
        _timer = 0;
        ApplyStatModifiers(CurrentLevelData.StatModifiers);
    }

    public override void OnUpdate(GameObject owner)
    {
        _timer += Time.deltaTime;

        float actualCooldown = GetModifiedCooldown(CurrentLevelData.Cooldown);
        if (_timer >= actualCooldown)
        {
            if (TryAttack(owner))
            {
                _timer = 0;
            }
        }
    }

    public override void OnLevelUp(GameObject owner)
    {
        base.OnLevelUp(owner);
        ApplyStatModifiers(CurrentLevelData.StatModifiers);
    }

    public override string GetLevelUpDescription()
    {
        if (IsMaxLevel) return "MAX LEVEL";
        return Levels[CurrentLevel].LevelDescription;
    }

    private bool TryAttack(GameObject owner)
    {
        Collider2D hit = Physics2D.OverlapCircle(owner.transform.position, CurrentLevelData.AttackRange, LayerMask.GetMask("Enemy"));
        
        if (hit != null)
        {
            EnemyEntity enemy = hit.GetComponent<EnemyEntity>();
            if (enemy != null)
            {
                enemy.TakeDamage(CurrentLevelData.Damage);
                return true;
            }
        }
        return false;
    }
}
