using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Random = UnityEngine.Random;

[DefaultExecutionOrder(-100)]
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;
    public GameObject EnemyPrefab;

    [Header("Heal Item Spawning")]
    public GameObject HealItemPrefab;
    [Range(0f, 1f)] public float HealItemDropChance = 0.05f;

    public Queue<EnemyEntity> enemyPool = new Queue<EnemyEntity>();

    public EnemyEntity[] _activeSlots;
    private List<int> _activeIndices;
    private bool[] _activeIndexSet;
    public int activeCount => _activeIndices != null ? _activeIndices.Count : 0;

    private struct BossSpawnInfo
    {
        public EnemyData EnemyData;
        public float CurrentHp;
        public bool IsRespawn;

        public BossSpawnInfo(EnemyData data, float hp, bool isRespawn)
        {
            EnemyData = data;
            CurrentHp = hp;
            IsRespawn = isRespawn;
        }
    }

    private float _spawnTimer;
    private JobHandle _lateHandle;
    private readonly List<int> _toRemove = new List<int>(256);
    private readonly List<int> _dyingIndices = new List<int>(128);
    private readonly List<BossSpawnInfo> _pendingSpawnBosses = new List<BossSpawnInfo>();

    [Header("Movement Weights")]
    public float SeparationWeight = 5.0f;
    public float ObstacleWeight = 10.0f;

    [Header("Spawning Config")]
    public float SpawnRadius = 15f;
    public float DespawnDistance = 25f;

    private float[] _attackTimers;
    private float[] _flashTimers;
    private bool[] _isFlashing;
    private List<int> _flashingIndices;
    private const float FLASH_DURATION = 0.1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _activeSlots = new EnemyEntity[SpatialSystem.MAX_ENEMIES];
            _activeIndices = new List<int>(SpatialSystem.MAX_ENEMIES);
            _activeIndexSet = new bool[SpatialSystem.MAX_ENEMIES];
            _attackTimers = new float[SpatialSystem.MAX_ENEMIES];

            _flashTimers = new float[SpatialSystem.MAX_ENEMIES];
            _isFlashing = new bool[SpatialSystem.MAX_ENEMIES];
            _flashingIndices = new List<int>(SpatialSystem.MAX_ENEMIES);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        EventManager.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        EventManager.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged()
    {
        if (StageManager.Instance == null || StageManager.Instance.StageData == null) return;

        int currentPhaseIdx = StageManager.Instance.currentPhase;
        PhaseData phase = StageManager.Instance.StageData.phaseDatas[currentPhaseIdx];

        if (phase.bossData != null && phase.bossData.Length > 0)
        {
            foreach (var boss in phase.bossData)
            {
                if (boss.EnemyData != null)
                {
                    for (int i = 0; i < boss.count; i++)
                    {
                        _pendingSpawnBosses.Add(new BossSpawnInfo(boss.EnemyData, boss.EnemyData.MaxHp, false));
                    }
                }
            }
        }
    }

    public void RegisterDyingEnemy(int index)
    {
        if (!_dyingIndices.Contains(index))
        {
            _dyingIndices.Add(index);
        }
    }

    void Start()
    {
        PreSpawnPool();
    }

    void PreSpawnPool()
    {
        for (int i = 0; i < SpatialSystem.MAX_ENEMIES; i++)
        {
            GameObject go = Instantiate(EnemyPrefab, transform);
            EnemyEntity entity = go.GetComponent<EnemyEntity>();
            
            entity.PoolIndex = i;
            SpatialSystem.Instance.PreRegisterEnemyTransform(go.transform);
            
            go.SetActive(false);
            enemyPool.Enqueue(entity);
        }
    }

    void Update()
    {
        if (SpatialSystem.Instance == null) return;

        HandleSpawning();
        HandleDyingEnemies();
        HandleEnemyAttacks();

        SpatialSystem spatial = SpatialSystem.Instance;
        spatial.UpdateReadOnlyPositions();

        spatial.SpatialGrid.Clear();
        var buildGridJob = new SpatialSystem.BuildGridJob
        {
            Positions = spatial.ReadOnlyPositions,
            Active    = spatial.EnemyActive,
            Grid      = spatial.SpatialGrid.AsParallelWriter()
        };
        JobHandle buildHandle = buildGridJob.Schedule(SpatialSystem.MAX_ENEMIES, 64);

        NativeArray<ObstacleData> obstacles = default;
        bool createdTemp = false;
        int obsCount = 0;

        if (ObstacleManager.Instance != null)
        {
            ObstacleManager.Instance.UpdateObstacleData();
            obstacles = ObstacleManager.Instance.ObstacleDatas;
            obsCount = ObstacleManager.Instance.GetActiveCount();
            
            spatial.ObstacleGrid.Clear();
        }
        else
        {
            obstacles = new NativeArray<ObstacleData>(0, Allocator.TempJob);
            createdTemp = true;
        }

        var moveJob = new EnemyMoveJob
        {
            PlayerPos = spatial.PlayerPosition,
            DeltaTime = Time.deltaTime,
            EnemyPositions = spatial.EnemyPositions,
            ReadOnlyEnemyPositions = spatial.ReadOnlyPositions,
            EnemyActive = spatial.EnemyActive,
            EnemySpeeds = spatial.EnemySpeeds,
            EnemyRadii = spatial.EnemyRadii,
            SpatialGrid = spatial.SpatialGrid,
            Obstacles = obstacles,
            ObstacleCount = obsCount,
            SeparationWeight = SeparationWeight,
            ObstacleWeight = ObstacleWeight
        };
        JobHandle moveHandle = moveJob.Schedule(SpatialSystem.MAX_ENEMIES, 64, buildHandle);

        var despawnJob = new SpatialSystem.DespawnMarkJob
        {
            Positions = spatial.EnemyPositions,
            Active = spatial.EnemyActive,
            PlayerPos = spatial.PlayerPosition,
            DespawnDistSq = DespawnDistance * DespawnDistance,
            PendingDespawn = spatial.PendingDespawn
        };
        JobHandle despawnHandle = despawnJob.Schedule(SpatialSystem.MAX_ENEMIES, 64, moveHandle);

        var syncJob = new EnemyVisualSyncJob
        {
            EnemyPositions = spatial.EnemyPositions,
            EnemyActive = spatial.EnemyActive,
            EnemyDying = spatial.FlipDying,
            PlayerPos = spatial.PlayerPosition,
            FlipLeft = spatial.FlipLeft
        };
        _lateHandle = syncJob.Schedule(spatial.EnemyTransforms, despawnHandle);

        if (createdTemp) obstacles.Dispose();
    }

    public void TriggerFlash(int index)
    {
        if (index < 0 || index >= SpatialSystem.MAX_ENEMIES) return;
        _flashTimers[index] = FLASH_DURATION;
        if (!_isFlashing[index])
        {
            _flashingIndices.Add(index);
            _isFlashing[index] = true;
        }
    }

    private void UpdateFlashing()
    {
        float dt = Time.deltaTime;
        for (int i = _flashingIndices.Count - 1; i >= 0; i--)
        {
            int idx = _flashingIndices[i];
            EnemyEntity entity = _activeSlots[idx];

            if (entity == null || !entity.IsActive)
            {
                _isFlashing[idx] = false;
                _flashingIndices.RemoveAt(i);
                continue;
            }

            _flashTimers[idx] -= dt;
            if (_flashTimers[idx] <= 0f)
            {
                entity.ResetFlash();
                _isFlashing[idx] = false;
                _flashingIndices.RemoveAt(i);
            }
            else
            {
                float progress = _flashTimers[idx] / FLASH_DURATION;
                float flashVal = progress * progress; // Ease Out Quad
                entity.SetFlash(flashVal);
            }
        }
    }

    void LateUpdate()
    {
        _lateHandle.Complete();
        HandleCleanup();
        UpdateFlashing();
        SyncVisuals();
    }

    public void CompleteLateJob()
    {
        _lateHandle.Complete();
    }

    private void HandleSpawning()
    {
        if (StageManager.Instance == null) return;

        if (_pendingSpawnBosses.Count > 0)
        {
            for (int i = _pendingSpawnBosses.Count - 1; i >= 0; i--)
            {
                BossSpawnInfo bossData = _pendingSpawnBosses[i];
                if (enemyPool.Count > 0)
                {
                    SpawnEnemy(bossData.EnemyData, bossData.CurrentHp, bossData.IsRespawn);
                    _pendingSpawnBosses.RemoveAt(i);
                }
            }
        }

        int currentPhaseIdx = StageManager.Instance.currentPhase;
        PhaseData phase = StageManager.Instance.StageData.phaseDatas[currentPhaseIdx];

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer < phase.SpawnInterval) return;

        int targetCount = Mathf.Min(phase.maxEnemyCount, SpatialSystem.MAX_ENEMIES);
        int deficit = targetCount - _activeIndices.Count;

        if (deficit > 0 && phase.enemyDatas != null && phase.enemyDatas.Length > 0)
        {
            int maxBatch = Mathf.Clamp(deficit / 4, 1, 500);
            int spawnCount = Random.Range(1, maxBatch + 1);

            for (int i = 0; i < spawnCount; i++)
            {
                EnemyData data = phase.enemyDatas[Random.Range(0, phase.enemyDatas.Length)];
                SpawnEnemy(data);
            }
        }
        _spawnTimer = 0f;
    }

    private void HandleEnemyAttacks()
    {
        if (PlayerStats.Instance == null || SpatialSystem.Instance == null) return;

        float dt = Time.deltaTime;
        float2 playerPos = SpatialSystem.Instance.PlayerPosition;

        // 모든 활성 적의 타이머 감소
        for (int i = 0; i < _activeIndices.Count; i++)
        {
            _attackTimers[_activeIndices[i]] -= dt;
        }

        // 공간 그리드를 사용하여 플레이어 주변의 적들만 검사
        int2 playerCell = (int2)math.floor(playerPos / SpatialSystem.CELL_SIZE);
        var grid = SpatialSystem.Instance.SpatialGrid;

        if (!grid.IsCreated) return;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int hash = SpatialSystem.GetCellHash(new int2(playerCell.x + x, playerCell.y + y));

                if (grid.TryGetFirstValue(hash, out int enemyIdx, out var it))
                {
                    do
                    {
                        EnemyEntity enemy = _activeSlots[enemyIdx];
                        if (enemy == null || enemy.isDying || _attackTimers[enemyIdx] > 0) continue;

                        float2 enemyPos = SpatialSystem.Instance.EnemyPositions[enemyIdx];
                        float distSq = math.distancesq(playerPos, enemyPos);
                        float range = enemy.EnemyData.AttackRange;

                        if (distSq <= range * range)
                        {
                            PlayerStats.Instance.TakeDamage(enemy.EnemyData.Damage);
                            _attackTimers[enemyIdx] = enemy.EnemyData.AttackInterval;
                        }
                    } while (grid.TryGetNextValue(out enemyIdx, ref it));
                }
            }
        }
    }

    private void SpawnEnemy(EnemyData data, float customHp = -1f, bool isRespawn = false)
    {
        if (enemyPool.Count == 0) return;

        EnemyEntity entity = enemyPool.Dequeue();
        float2 spawnPos = GetRandomSpawnPos();
        entity.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0);
        
        entity.gameObject.SetActive(true);
        entity.Init(data);

        if (customHp >= 0f)
        {
            entity.CurrentHp = customHp;
        }

        if (!isRespawn && GameDataManager.Instance != null)
        {
            GameDataManager.Instance.AddSpawn();
        }

        int idx = entity.PoolIndex;
        if (idx == -1)
        {
            entity.gameObject.SetActive(false);
            enemyPool.Enqueue(entity);
            return;
        }

        _activeSlots[idx] = entity;
        _attackTimers[idx] = 0f;

        if (!_activeIndexSet[idx])
        {
            _activeIndices.Add(idx);
            _activeIndexSet[idx] = true;
        }
    }

    private float2 GetRandomSpawnPos()
    {
        float randomRadius = SpawnRadius + Random.Range(-2f, 3f);
        float angle = Random.Range(0f, math.PI * 2f);
        float2 offset = new float2(math.cos(angle), math.sin(angle)) * randomRadius;
        return SpatialSystem.Instance.PlayerPosition + offset;
    }

    private void HandleCleanup()
    {
        _toRemove.Clear();
        var pending = SpatialSystem.Instance.PendingDespawn;

        for (int i = 0; i < _activeIndices.Count; i++)
        {
            int idx = _activeIndices[i];
            EnemyEntity entity = _activeSlots[idx];
            
            if (entity == null || !entity.IsActive || pending[idx])
            {
                _toRemove.Add(idx);
            }
        }

        if (_toRemove.Count == 0) return;

        foreach (int idx in _toRemove)
        {
            EnemyEntity entity = _activeSlots[idx];
            if (entity != null)
            {
                if (entity.EnemyData != null && entity.EnemyData.IsBoss && !entity.isDying && entity.CurrentHp > 0)
                {
                    _pendingSpawnBosses.Add(new BossSpawnInfo(entity.EnemyData, entity.CurrentHp, true));
                }

                if (entity.gameObject.activeSelf)
                {
                    entity.gameObject.SetActive(false);
                }
                enemyPool.Enqueue(entity);
            }
            _activeSlots[idx] = null;
            _activeIndexSet[idx] = false;
        }

        _activeIndices.RemoveAll(idx => !_activeIndexSet[idx]);
    }

    private void SyncVisuals()
    {
        var left = SpatialSystem.Instance.FlipLeft;

        for (int i = 0; i < _activeIndices.Count; i++)
        {
            int idx = _activeIndices[i];
            _activeSlots[idx].ApplyVisuals(left[idx]);
        }
    }

    private void HandleDyingEnemies()
    {
        float dt = Time.deltaTime;
        for (int i = _dyingIndices.Count - 1; i >= 0; i--)
        {
            int idx = _dyingIndices[i];
            EnemyEntity entity = _activeSlots[idx];
            
            if (entity == null || !entity.isDying)
            {
                _dyingIndices.RemoveAt(i);
                continue;
            }

            entity.DeathTimer -= dt;
            if (entity.DeathTimer <= 0)
            {
                entity.Die();
                _dyingIndices.RemoveAt(i);
            }
        }
    }
}
