using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LightningSkill", menuName = "Skills/Lightning")]
public class LightningSkillData : SkillData
{
    [Serializable]
    public struct LightningLevel
    {
        public float Damage;
        public int MinStrikeCount;
        public int MaxStrikeCount;
        public float StrikeDuration;
        public float StrikeRadius;
        public float SearchRadius;
        public float Cooldown;
        public BaseStatModifier[] StatModifiers;
        [TextArea] public string LevelDescription;
    }

    [SerializeField] private GameObject _lightningEffectPrefab;
    [SerializeField] private bool _useDirectPlay = true;
    [SerializeField] private string _animParameterName = "AnimIndex";
    [SerializeField] private string _animStatePrefix = "Lightning_";
    [SerializeField] private LightningLevel[] _levels;

    private float _cooldownTimer;
    private readonly Queue<GameObject> _lightningPool = new Queue<GameObject>();

    public override int MaxLevel => _levels.Length;
    private LightningLevel CurrentLevelData => _levels[Mathf.Clamp(CurrentLevel - 1, 0, MaxLevel - 1)];

    public override void OnEquip(GameObject owner)
    {
        base.OnEquip(owner);
        _cooldownTimer = 0f;
        _lightningPool.Clear();
        ApplyStatModifiers(CurrentLevelData.StatModifiers);
    }

    public override void OnUpdate(GameObject owner)
    {
        _cooldownTimer += Time.deltaTime;

        float actualCooldown = GetModifiedCooldown(CurrentLevelData.Cooldown);
        if (_cooldownTimer >= actualCooldown)
        {
            _cooldownTimer = 0f;
            TriggerLightningStorm(owner);
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
        return _levels[CurrentLevel].LevelDescription;
    }

    private void TriggerLightningStorm(GameObject owner)
    {
        PlayerSkillManager.Instance.StartCoroutine(SpawnLightningStorm(owner));
    }

    private IEnumerator SpawnLightningStorm(GameObject owner)
    {
        LightningLevel data = CurrentLevelData;
        int count = UnityEngine.Random.Range(data.MinStrikeCount, data.MaxStrikeCount + 1);

        List<EnemyEntity> targetEnemies = GetNearestEnemies(owner.transform.position, data.SearchRadius, count);

        for (int i = 0; i < count; i++)
        {
            Vector2 targetPos;

            if (targetEnemies.Count > 0 && i < targetEnemies.Count)
            {
                var target = targetEnemies[i];
                if (target != null && target.IsActive)
                {
                    targetPos = target.transform.position;
                }
                else
                {
                    Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * data.SearchRadius;
                    targetPos = (Vector2)owner.transform.position + randomOffset;
                }
            }
            else
            {
                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * data.SearchRadius;
                targetPos = (Vector2)owner.transform.position + randomOffset;
            }

            DropSingleLightning(targetPos, data);

            float delay = data.StrikeDuration / count;
            yield return new WaitForSeconds(UnityEngine.Random.Range(delay * 0.5f, delay * 1.5f));
        }
    }

    private void DropSingleLightning(Vector2 targetPos, LightningLevel data)
    {
        if (_lightningEffectPrefab != null)
        {
            GameObject fx = GetPooledLightning(targetPos);
            
            Animator animator = fx.GetComponent<Animator>();
            if (animator != null)
            {
                int randomAnimIndex = UnityEngine.Random.Range(0, 4);
                
                if (_useDirectPlay)
                {
                    string stateName = _animStatePrefix + randomAnimIndex;
                    animator.Play(stateName, 0, 0f);
                }
                else if (!string.IsNullOrEmpty(_animParameterName))
                {
                    animator.SetInteger(_animParameterName, randomAnimIndex);
                }
                
                animator.Update(0f);
            }
            PlayerSkillManager.Instance.StartCoroutine(ReturnLightningToPool(fx, 0.5f));
        }

        ApplyAreaDamage(targetPos, data);
    }

    private GameObject GetPooledLightning(Vector2 pos)
    {
        if (_lightningPool.Count > 0)
        {
            GameObject fx = _lightningPool.Dequeue();
            fx.transform.position = pos;
            fx.SetActive(true);
            return fx;
        }
        return Instantiate(_lightningEffectPrefab, pos, Quaternion.identity);
    }

    private IEnumerator ReturnLightningToPool(GameObject fx, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (fx != null)
        {
            fx.SetActive(false);
            _lightningPool.Enqueue(fx);
        }
    }

    private List<EnemyEntity> GetNearestEnemies(Vector2 playerPos, float searchRadius, int maxCount)
    {
        if (SpatialSystem.Instance == null || EnemyManager.Instance == null) return new List<EnemyEntity>();

        EnemyManager.Instance.CompleteLateJob();

        List<(EnemyEntity enemy, float distSq)> candidates = new List<(EnemyEntity, float)>();

        int startX = (int)Unity.Mathematics.math.floor((playerPos.x - searchRadius) / SpatialSystem.CELL_SIZE);
        int endX = (int)Unity.Mathematics.math.floor((playerPos.x + searchRadius) / SpatialSystem.CELL_SIZE);
        int startY = (int)Unity.Mathematics.math.floor((playerPos.y - searchRadius) / SpatialSystem.CELL_SIZE);
        int endY = (int)Unity.Mathematics.math.floor((playerPos.y + searchRadius) / SpatialSystem.CELL_SIZE);

        var grid = SpatialSystem.Instance.SpatialGrid;
        var activeSlots = EnemyManager.Instance._activeSlots;
        var enemyPositions = SpatialSystem.Instance.EnemyPositions;
        float radiusSq = searchRadius * searchRadius;

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                int hash = SpatialSystem.GetCellHash(new Unity.Mathematics.int2(x, y));
                if (grid.TryGetFirstValue(hash, out int enemyIndex, out var it))
                {
                    do
                    {
                        EnemyEntity enemy = activeSlots[enemyIndex];
                        if (enemy != null && enemy.IsActive)
                        {
                            Unity.Mathematics.float2 enemyPos = enemyPositions[enemyIndex];
                            float distSq = Unity.Mathematics.math.distancesq(new Unity.Mathematics.float2(playerPos.x, playerPos.y), enemyPos);
                            if (distSq <= radiusSq)
                            {
                                candidates.Add((enemy, distSq));
                            }
                        }
                    } while (grid.TryGetNextValue(out enemyIndex, ref it));
                }
            }
        }

        candidates.Sort((a, b) => a.distSq.CompareTo(b.distSq));

        List<EnemyEntity> found = new List<EnemyEntity>();
        int limit = Mathf.Min(candidates.Count, maxCount);
        for (int i = 0; i < limit; i++)
        {
            found.Add(candidates[i].enemy);
        }

        return found;
    }

    private void ApplyAreaDamage(Vector2 center, LightningLevel data)
    {
        if (SpatialSystem.Instance == null || EnemyManager.Instance == null) return;

        EnemyManager.Instance.CompleteLateJob();

        float radius = data.StrikeRadius;
        float radiusSq = radius * radius;
        float finalDamage = data.Damage;

        if (PlayerStats.Instance != null && PlayerStats.Instance.StatData != null)
        {
            finalDamage += PlayerStats.Instance.StatData.CurrentDamage;
        }

        int startX = (int)Unity.Mathematics.math.floor((center.x - radius) / SpatialSystem.CELL_SIZE);
        int endX = (int)Unity.Mathematics.math.floor((center.x + radius) / SpatialSystem.CELL_SIZE);
        int startY = (int)Unity.Mathematics.math.floor((center.y - radius) / SpatialSystem.CELL_SIZE);
        int endY = (int)Unity.Mathematics.math.floor((center.y + radius) / SpatialSystem.CELL_SIZE);

        var grid = SpatialSystem.Instance.SpatialGrid;
        var activeSlots = EnemyManager.Instance._activeSlots;
        var enemyPositions = SpatialSystem.Instance.EnemyPositions;

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                int hash = SpatialSystem.GetCellHash(new Unity.Mathematics.int2(x, y));
                if (grid.TryGetFirstValue(hash, out int enemyIndex, out var it))
                {
                    do
                    {
                        EnemyEntity enemy = activeSlots[enemyIndex];
                        if (enemy != null && enemy.IsActive)
                        {
                            Unity.Mathematics.float2 enemyPos = enemyPositions[enemyIndex];
                            float distSq = Unity.Mathematics.math.distancesq(new Unity.Mathematics.float2(center.x, center.y), enemyPos);
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
