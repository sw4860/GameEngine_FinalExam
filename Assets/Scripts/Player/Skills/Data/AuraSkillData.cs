using UnityEngine;
using Unity.Mathematics;
using System;

[CreateAssetMenu(fileName = "AuraSkill", menuName = "Skills/Aura")]
public class AuraSkillData : SkillData
{
    [Serializable]
    public struct AuraLevel
    {
        public float Damage;
        public float Radius;
        public float Interval;
        public BaseStatModifier[] StatModifiers;
        [TextArea] public string LevelDescription;
    }

    [Header("Aura Settings")]
    public GameObject AuraPrefab;
    public AuraLevel[] Levels;

    private float _timer;
    private Transform _auraTransform;

    public override int MaxLevel => Levels.Length;
    private AuraLevel CurrentLevelData => Levels[Mathf.Clamp(CurrentLevel - 1, 0, MaxLevel - 1)];

    public override void OnEquip(GameObject owner)
    {
        base.OnEquip(owner);
        if (AuraPrefab != null)
        {
            GameObject obj = Instantiate(AuraPrefab, owner.transform);
            obj.transform.localPosition = Vector3.zero;
            _auraTransform = obj.transform;
        }
        UpdateEffect(owner);
    }

    public override void OnUpdate(GameObject owner)
    {
        _timer += Time.deltaTime;
        if (_timer >= CurrentLevelData.Interval)
        {
            _timer = 0;
            ApplyDamage(owner);
        }
    }

    public override void OnLevelUp(GameObject owner)
    {
        base.OnLevelUp(owner);
        UpdateEffect(owner);
    }

    private void UpdateEffect(GameObject owner)
    {
        ApplyStatModifiers(CurrentLevelData.StatModifiers);
        if (_auraTransform != null)
        {
            float r = CurrentLevelData.Radius;
            _auraTransform.localScale = new Vector3(r * 2, r * 2, 1);
        }
    }

    public override string GetLevelUpDescription()
    {
        return $"Damage: {CurrentLevelData.Damage} -> {(CurrentLevel < MaxLevel ? Levels[CurrentLevel].Damage.ToString() : "MAX")}\n" +
               $"Radius: {CurrentLevelData.Radius} -> {(CurrentLevel < MaxLevel ? Levels[CurrentLevel].Radius.ToString() : "MAX")}\n" +
               $"Interval: {CurrentLevelData.Interval} -> {(CurrentLevel < MaxLevel ? Levels[CurrentLevel].Interval.ToString() : "MAX")}\n";
    }

    private void ApplyDamage(GameObject owner)
    {
        if (SpatialSystem.Instance == null || EnemyManager.Instance == null) return;

        Vector2 myPos = owner.transform.position;
        float radius = CurrentLevelData.Radius;
        float radiusSq = radius * radius;
        float finalDamage = CurrentLevelData.Damage;

        if (PlayerStats.Instance != null && PlayerStats.Instance.StatData != null)
        {
            finalDamage += PlayerStats.Instance.StatData.CurrentDamage;
        }

        int startX = (int)math.floor((myPos.x - radius) / SpatialSystem.CELL_SIZE);
        int endX = (int)math.floor((myPos.x + radius) / SpatialSystem.CELL_SIZE);
        int startY = (int)math.floor((myPos.y - radius) / SpatialSystem.CELL_SIZE);
        int endY = (int)math.floor((myPos.y + radius) / SpatialSystem.CELL_SIZE);

        var grid = SpatialSystem.Instance.SpatialGrid;
        var activeSlots = EnemyManager.Instance._activeSlots;
        var enemyPositions = SpatialSystem.Instance.EnemyPositions;

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                int hash = (x * 73856093) ^ (y * 19349663);
                if (grid.TryGetFirstValue(hash, out int enemyIndex, out var it))
                {
                    do
                    {
                        EnemyEntity enemy = activeSlots[enemyIndex];
                        if (enemy != null && enemy.IsActive)
                        {
                            float2 enemyPos = enemyPositions[enemyIndex];
                            float distSq = math.distancesq(new float2(myPos.x, myPos.y), enemyPos);
                            if (distSq <= radiusSq)
                            {
                                enemy.TakeDamage(finalDamage);
                            }
                        }
                    } while (grid.TryGetNextValue(out enemyIndex, ref it));
                }
            }
        }
    }
}
