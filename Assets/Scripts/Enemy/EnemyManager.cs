using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[DefaultExecutionOrder(-100)]
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;
    public GameObject EnemyPrefab;
    private Transform playerTransform;

    public Queue<EnemyEntity> enemyPool = new Queue<EnemyEntity>();

    public EnemyEntity[] _activeSlots   = new EnemyEntity[SpatialSystem.MAX_ENEMIES];
    public List<int>     _activeIndices = new List<int>(SpatialSystem.MAX_ENEMIES);
    private readonly bool[] _activeIndexSet = new bool[SpatialSystem.MAX_ENEMIES];
    public int activeCount => _activeIndices.Count;

    private float      _spawnTimer;
    private JobHandle  _lateHandle;          // LateUpdate에서 Complete할 핸들
    private readonly List<int> _toRemove = new List<int>(256);

    [Header("Movement Weights")]
    public float SeparationWeight = 5.0f;
    public float ObstacleWeight   = 10.0f;

    [Header("Spawning Config")]
    public float SpawnRadius    = 15f;
    public float DespawnDistance = 25f;

    // -------------------------------------------------------
    void Awake() { Instance = this; }

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        PreSpawnPool();
    }

    void PreSpawnPool()
    {
        for (int i = 0; i < SpatialSystem.MAX_ENEMIES; i++)
        {
            GameObject go = Instantiate(EnemyPrefab, transform);
            EnemyEntity entity = go.GetComponent<EnemyEntity>();
            entity.SetSortingOrder(i + 1);
            go.SetActive(false);
            enemyPool.Enqueue(entity);
        }
    }

    // -------------------------------------------------------
    void Update()
    {
        if (playerTransform == null) return;

        HandleSpawning();

        // ── Spatial 시스템 업데이트 ──────────────────────────
        SpatialSystem.Instance.PlayerPosition =
            new float2(playerTransform.position.x, playerTransform.position.y);

        // ReadOnlyPositions = 이번 프레임 시작 시점의 위치 스냅샷
        // (MoveJob이 EnemyPositions를 덮어쓰기 전에 복사)
        SpatialSystem.Instance.UpdateReadOnlyPositions();

        // ── Enemy Grid ──────────────────────────────────────
        SpatialSystem.Instance.SpatialGrid.Clear();
        var buildGridJob = new SpatialSystem.BuildSpatialGridJob
        {
            Positions = SpatialSystem.Instance.ReadOnlyPositions,
            Active    = SpatialSystem.Instance.EnemyActive,
            Grid      = SpatialSystem.Instance.SpatialGrid.AsParallelWriter()
        };
        JobHandle buildHandle = buildGridJob.Schedule(SpatialSystem.MAX_ENEMIES, 64);

        // ── Obstacle Grid ───────────────────────────────────
        NativeArray<ObstacleData> obstacles = default;
        bool      createdTemp  = false;
        JobHandle obsGridHandle = default;

        if (ObstacleManager.Instance != null)
        {
            ObstacleManager.Instance.UpdateObstacleData();
            obstacles = ObstacleManager.Instance.ObstacleDatas;

            SpatialSystem.Instance.ObstacleGrid.Clear();
            var obsBuildJob = new SpatialSystem.BuildObstacleGridJob
            {
                Obstacles = obstacles,
                Count     = ObstacleManager.Instance.GetActiveCount(),
                Grid      = SpatialSystem.Instance.ObstacleGrid.AsParallelWriter()
            };
            obsGridHandle = obsBuildJob.Schedule(ObstacleManager.Instance.GetActiveCount(), 32);
        }
        else
        {
            obstacles    = new NativeArray<ObstacleData>(0, Allocator.TempJob);
            createdTemp  = true;
        }

        JobHandle combinedBuildHandle =
            JobHandle.CombineDependencies(buildHandle, obsGridHandle);

        // ── Move Job ────────────────────────────────────────
        var moveJob = new EnemyMoveJob
        {
            PlayerPos              = SpatialSystem.Instance.PlayerPosition,
            DeltaTime              = Time.deltaTime,
            EnemyPositions         = SpatialSystem.Instance.EnemyPositions,
            ReadOnlyEnemyPositions = SpatialSystem.Instance.ReadOnlyPositions,
            EnemyActive            = SpatialSystem.Instance.EnemyActive,
            EnemySpeeds            = SpatialSystem.Instance.EnemySpeeds,
            EnemyRadii             = SpatialSystem.Instance.EnemyRadii,
            SpatialGrid            = SpatialSystem.Instance.SpatialGrid,
            ObstacleGrid           = SpatialSystem.Instance.ObstacleGrid,
            Obstacles              = obstacles,
            SeparationWeight       = SeparationWeight,
            ObstacleWeight         = ObstacleWeight
        };
        JobHandle moveHandle = moveJob.Schedule(SpatialSystem.MAX_ENEMIES, 64, combinedBuildHandle);

        // ── Despawn Mark Job ────────────────────────────────
        var despawnJob = new SpatialSystem.DespawnMarkJob
        {
            Positions     = SpatialSystem.Instance.EnemyPositions,
            Active        = SpatialSystem.Instance.EnemyActive,
            PlayerPos     = SpatialSystem.Instance.PlayerPosition,
            DespawnDistSq = DespawnDistance * DespawnDistance,
            PendingDespawn = SpatialSystem.Instance.PendingDespawn
        };
        JobHandle despawnHandle = despawnJob.Schedule(SpatialSystem.MAX_ENEMIES, 64, moveHandle);

        // ── Visual Sync Job ─────────────────────────────────
        // transform 이동 + flip 방향 계산을 Job 안에서 처리
        // → LateUpdate에서 spriteRenderer.flipX만 설정 (get_transform 호출 없음)
        var syncJob = new EnemyVisualSyncJob
        {
            EnemyPositions = SpatialSystem.Instance.EnemyPositions,
            PrevPositions  = SpatialSystem.Instance.ReadOnlyPositions,
            EnemyActive    = SpatialSystem.Instance.EnemyActive,
            FlipDirty      = SpatialSystem.Instance.FlipDirty,
            FlipLeft       = SpatialSystem.Instance.FlipLeft
        };
        // despawnHandle에 의존 — Despawn 판정 후 sync
        _lateHandle = syncJob.Schedule(SpatialSystem.Instance.EnemyTransforms, despawnHandle);

        if (createdTemp) obstacles.Dispose();
    }

    // -------------------------------------------------------
    void LateUpdate()
    {
        // Update()에서 스케줄한 모든 Job 완료 대기
        _lateHandle.Complete();

        HandleCleanup();
        SyncVisuals();
    }

    // -------------------------------------------------------
    void HandleSpawning()
    {
        if (StageManager.Instance == null) return;
        int       currentPhaseIdx = StageManager.Instance.currentPhase;
        PhaseData phase           = StageManager.Instance.StageData.phaseDatas[currentPhaseIdx];

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer < phase.SpawnInterval) return;

        int targetCount = Mathf.Min(phase.maxEnemyCount, SpatialSystem.MAX_ENEMIES);
        int deficit     = targetCount - _activeIndices.Count;

        if (deficit > 0)
        {
            int spawnCount = Mathf.Min(Mathf.Max(deficit / 10, 1), 500);
            for (int i = 0; i < spawnCount; i++)
            {
                EnemyData data = phase.enemyDatas[
                    UnityEngine.Random.Range(0, phase.enemyDatas.Length)];
                SpawnEnemy(data);
            }
        }
        _spawnTimer = 0f;
    }

    void SpawnEnemy(EnemyData data)
    {
        if (enemyPool.Count == 0) return;

        EnemyEntity entity    = enemyPool.Dequeue();
        Vector2     spawnPos  = (Vector2)playerTransform.position
                                + UnityEngine.Random.insideUnitCircle.normalized * SpawnRadius;
        entity.transform.position = spawnPos;
        entity.gameObject.SetActive(true);
        entity.Init(data);

        int spatialIndex = entity.SpatialIndex;
        if (spatialIndex == -1)
        {
            entity.gameObject.SetActive(false);
            enemyPool.Enqueue(entity);
            return;
        }

        // 같은 슬롯에 이미 다른 엔티티가 있으면 교체
        EnemyEntity oldEntity = _activeSlots[spatialIndex];
        if (oldEntity != null && oldEntity != entity)
        {
            oldEntity.gameObject.SetActive(false);
            oldEntity.IsActive = false;
            _activeSlots[spatialIndex] = null;
            if (_activeIndexSet[spatialIndex])
            {
                _activeIndexSet[spatialIndex] = false;
                _activeIndices.Remove(spatialIndex);
            }
            enemyPool.Enqueue(oldEntity);
        }

        if (GameManager.Instance != null) GameManager.Instance.AddSpawn();

        _activeSlots[spatialIndex] = entity;
        if (!_activeIndexSet[spatialIndex])
        {
            _activeIndexSet[spatialIndex] = true;
            _activeIndices.Add(spatialIndex);
        }
    }

    // -------------------------------------------------------
    void HandleCleanup()
    {
        _toRemove.Clear();

        for (int i = 0; i < _activeIndices.Count; i++)
        {
            int         spatialIdx = _activeIndices[i];
            EnemyEntity entity     = _activeSlots[spatialIdx];

            if (entity == null
                || !entity.IsActive
                || SpatialSystem.Instance.PendingDespawn[spatialIdx])
            {
                _toRemove.Add(i);
            }
        }

        // 뒤에서 앞으로 제거 — RemoveAt 인덱스 유지
        for (int i = _toRemove.Count - 1; i >= 0; i--)
        {
            int         listPos    = _toRemove[i];
            int         spatialIdx = _activeIndices[listPos];
            EnemyEntity entity     = _activeSlots[spatialIdx];

            if (entity != null)
            {
                entity.gameObject.SetActive(false);
                entity.IsActive = false;
                enemyPool.Enqueue(entity);
            }

            _activeSlots[spatialIdx] = null;
            _activeIndexSet[spatialIdx] = false;
            _activeIndices.RemoveAt(listPos);
        }
    }

    // -------------------------------------------------------
    // transform 접근 전혀 없음 — spriteRenderer.flipX만 설정
    void SyncVisuals()
    {
        var flipDirty = SpatialSystem.Instance.FlipDirty;
        var flipLeft  = SpatialSystem.Instance.FlipLeft;

        for (int i = 0; i < _activeIndices.Count; i++)
        {
            int spatialIdx = _activeIndices[i];

            // dirty 아니면 즉시 skip — 대부분 직진 중이면 방향 안 바뀜
            EnemyEntity entity = _activeSlots[spatialIdx];
            if (entity == null || !entity.IsActive) continue;

            if (flipDirty[spatialIdx])
                entity.ApplyVisuals(flipLeft[spatialIdx]);
        }
    }
}
