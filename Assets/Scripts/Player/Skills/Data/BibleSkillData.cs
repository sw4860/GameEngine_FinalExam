using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BibleSkill", menuName = "Skills/Bible")]
public class BibleSkillData : SkillData
{
    [Serializable]
    public struct BibleLevel
    {
        public float Damage;
        public int ProjectileCount;
        public float OrbitRadius;
        public float RotationSpeed;
        public float HitRadius;
        public float HitInterval;
        public BaseStatModifier[] StatModifiers;
        [TextArea] public string LevelDescription;
    }

    [SerializeField] private GameObject _biblePrefab;
    [SerializeField] private BibleLevel[] _levels;

    private GameObject _orbitParent;
    private readonly Dictionary<int, float> _enemyHitCooldowns = new Dictionary<int, float>();
    private readonly Queue<GameObject> _biblePool = new Queue<GameObject>();
    private float _currentAngle = 0f;

    public override int MaxLevel => _levels.Length;

    private BibleLevel CurrentLevelData
    {
        get
        {
#if UNITY_EDITOR
            if (OriginAsset != null)
            {
                var originBible = (BibleSkillData)OriginAsset;
                if (originBible._levels != null && originBible._levels.Length > 0)
                {
                    return originBible._levels[Mathf.Clamp(CurrentLevel - 1, 0, originBible._levels.Length - 1)];
                }
            }
#endif
            return _levels[Mathf.Clamp(CurrentLevel - 1, 0, MaxLevel - 1)];
        }
    }

    public override void OnEquip(GameObject owner)
    {
        base.OnEquip(owner);
        _currentAngle = 0f;
        _biblePool.Clear();
        ApplyStatModifiers(CurrentLevelData.StatModifiers);
        SpawnBibles(owner);
    }

    public override void OnUpdate(GameObject owner)
    {
        if (_orbitParent != null)
        {
            _orbitParent.transform.position = owner.transform.position;
            
            _currentAngle += CurrentLevelData.RotationSpeed * Time.deltaTime;
            
            int childCount = _orbitParent.transform.childCount;
            if (childCount > 0)
            {
                float angleStep = 360f / childCount;
                float radius = CurrentLevelData.OrbitRadius;
                for (int i = 0; i < childCount; i++)
                {
                    Transform child = _orbitParent.transform.GetChild(i);
                    float finalAngle = _currentAngle + (i * angleStep);
                    float rad = finalAngle * Mathf.Deg2Rad;
                    child.localPosition = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;
                }
            }
            
            ApplyOrbitDamage();
        }
    }

    public override void OnLevelUp(GameObject owner)
    {
        base.OnLevelUp(owner);
        _currentAngle = 0f;
        ApplyStatModifiers(CurrentLevelData.StatModifiers);
        DespawnBibles();
        SpawnBibles(owner);
    }

    public override string GetLevelUpDescription()
    {
        if (IsMaxLevel) return "MAX LEVEL";
        return _levels[CurrentLevel].LevelDescription;
    }

    private void SpawnBibles(GameObject owner)
    {
        if (_biblePrefab == null) return;

        _orbitParent = new GameObject("BibleOrbitParent");
        _orbitParent.transform.position = owner.transform.position;

        int count = CurrentLevelData.ProjectileCount;
        float radius = CurrentLevelData.OrbitRadius;
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * radius;

            GameObject bible = GetPooledBible(_orbitParent.transform.position + offset, _orbitParent.transform);
            
            Rigidbody2D rb = bible.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = false;
            }

            BibleProjectile proj = bible.GetComponent<BibleProjectile>();
            if (proj == null)
            {
                proj = bible.AddComponent<BibleProjectile>();
            }
            proj.Init(CurrentLevelData.Damage);
        }
        _enemyHitCooldowns.Clear();
    }

    private void DespawnBibles()
    {
        if (_orbitParent != null)
        {
            int childCount = _orbitParent.transform.childCount;
            List<GameObject> activeBibles = new List<GameObject>();
            for (int i = 0; i < childCount; i++)
            {
                activeBibles.Add(_orbitParent.transform.GetChild(i).gameObject);
            }

            foreach (var bible in activeBibles)
            {
                bible.transform.SetParent(null);
                bible.SetActive(false);
                _biblePool.Enqueue(bible);
            }

            Destroy(_orbitParent);
            _orbitParent = null;
        }
        _enemyHitCooldowns.Clear();
    }

    private GameObject GetPooledBible(Vector3 pos, Transform parent)
    {
        if (_biblePool.Count > 0)
        {
            GameObject bible = _biblePool.Dequeue();
            bible.transform.position = pos;
            bible.transform.SetParent(parent);
            bible.SetActive(true);
            return bible;
        }
        return Instantiate(_biblePrefab, pos, Quaternion.identity, parent);
    }

    private void ApplyOrbitDamage()
    {
        if (SpatialSystem.Instance == null || EnemyManager.Instance == null || _orbitParent == null) return;

        EnemyManager.Instance.CompleteLateJob();

        float radius = CurrentLevelData.HitRadius;
        float radiusSq = radius * radius;
        float finalDamage = CurrentLevelData.Damage;

        if (PlayerStats.Instance != null && PlayerStats.Instance.StatData != null)
        {
            finalDamage += PlayerStats.Instance.StatData.CurrentDamage;
        }

        var grid = SpatialSystem.Instance.SpatialGrid;
        var activeSlots = EnemyManager.Instance._activeSlots;
        var enemyPositions = SpatialSystem.Instance.EnemyPositions;

        int childCount = _orbitParent.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform bibleTrans = _orbitParent.transform.GetChild(i);
            Vector2 center = bibleTrans.position;

            int startX = (int)Unity.Mathematics.math.floor((center.x - radius) / SpatialSystem.CELL_SIZE);
            int endX = (int)Unity.Mathematics.math.floor((center.x + radius) / SpatialSystem.CELL_SIZE);
            int startY = (int)Unity.Mathematics.math.floor((center.y - radius) / SpatialSystem.CELL_SIZE);
            int endY = (int)Unity.Mathematics.math.floor((center.y + radius) / SpatialSystem.CELL_SIZE);

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
                                    int poolIdx = enemy.PoolIndex;
                                    _enemyHitCooldowns.TryGetValue(poolIdx, out float nextHitTime);
                                    if (Time.time >= nextHitTime)
                                    {
                                        enemy.TakeDamage(finalDamage);
                                        _enemyHitCooldowns[poolIdx] = Time.time + (CurrentLevelData.HitInterval > 0.05f ? CurrentLevelData.HitInterval : 0.8f);
                                    }
                                }
                            }
                        } while (grid.TryGetNextValue(out enemyIndex, ref it));
                    }
                }
            }
        }
    }
}
