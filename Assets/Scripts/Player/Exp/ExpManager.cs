using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[DefaultExecutionOrder(-90)] // EnemyManager(-100) 보다는 늦게, 일반(0) 보다는 일찍
public class ExpManager : MonoBehaviour
{
    public static ExpManager Instance;

    [Header("Prefabs & Assets")]
    public GameObject ExpPrefab;
    public Sprite[] ExpSprites;
    public AudioClip CollectSound;

    [Header("Settings")]
    public float CollectRadius = 0.5f;
    public float MoveSpeed = 15.0f;

    [Header("Merge Settings")]
    public float MergeRadius = 1.0f;
    public int MergeThreshold = 100;

    private Queue<ExpEntity> _expPool = new Queue<ExpEntity>();
    private ExpEntity[] _activeSlots;
    private List<int> _activeIndices;
    private float _lastCollectSoundTime = -1f;

    private JobHandle _lateHandle;
    private JobHandle _mergeJobHandle;
    private NativeArray<int> _mergeTargets;
    private readonly List<int> _toRemove = new List<int>(256);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _activeSlots = new ExpEntity[SpatialSystem.MAX_EXPS];
            _activeIndices = new List<int>(SpatialSystem.MAX_EXPS);
            _mergeTargets = new NativeArray<int>(SpatialSystem.MAX_EXPS, Allocator.Persistent);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (_mergeTargets.IsCreated) _mergeTargets.Dispose();
    }

    void Start()
    {
        PreSpawnPool();
    }

    void PreSpawnPool()
    {
        for (int i = 0; i < SpatialSystem.MAX_EXPS; i++)
        {
            CreateNewExpEntity(i);
        }
    }

    private ExpEntity CreateNewExpEntity(int index)
    {
        GameObject go = Instantiate(ExpPrefab, transform);
        ExpEntity entity = go.GetComponent<ExpEntity>();
        if (entity == null) entity = go.AddComponent<ExpEntity>();

        entity.SpatialIndex = index;
        SpatialSystem.Instance.SetExpTransform(index, go.transform);

        go.SetActive(false);
        _expPool.Enqueue(entity);
        return entity;
    }

    public void SpawnExp(float2 pos, int expValue)
    {
        if (SpatialSystem.Instance == null) return;

        _lateHandle.Complete();

        if (_expPool.Count == 0) return;

        ExpEntity entity = _expPool.Dequeue();
        int index = entity.SpatialIndex;

        SpatialSystem.Instance.ActivateExp(index, pos, expValue);

        Sprite sprite = GetSpriteForValue(expValue);
        entity.Init(index, sprite);

        entity.transform.position = new Vector3(pos.x, pos.y, 0);
        entity.gameObject.SetActive(true);

        _activeSlots[index] = entity;
        _activeIndices.Add(index);
    }

    private Sprite GetSpriteForValue(int value)
    {
        if (ExpSprites == null || ExpSprites.Length == 0) return null;
        if (value >= 200 && ExpSprites.Length > 2) return ExpSprites[2];
        if (value >= 50 && ExpSprites.Length > 1) return ExpSprites[1];
        return ExpSprites[0];
    }

    void Update()
    {
        if (SpatialSystem.Instance == null) return;

        // ── Exp Grid Build Job ──────────────────────────
        SpatialSystem spatial = SpatialSystem.Instance;

        spatial.ExpGrid.Clear();
        var buildGridJob = new SpatialSystem.BuildGridJob
        {
            Positions = spatial.ExpPositions,
            Active = spatial.ExpActive,
            Grid = spatial.ExpGrid.AsParallelWriter()
        };
        JobHandle gridHandle = buildGridJob.Schedule(SpatialSystem.MAX_EXPS, 64);

        // ── Merge Detection Job ─────────────────────────
        JobHandle currentDependency = gridHandle;
        if (_activeIndices.Count >= MergeThreshold)
        {
            var mergeJob = new ExpMergeDetectionJob
            {
                Positions = spatial.ExpPositions,
                Active = spatial.ExpActive,
                Grid = spatial.ExpGrid,
                MergeRadiusSq = MergeRadius * MergeRadius,
                CellSize = SpatialSystem.CELL_SIZE,
                MergeTargets = _mergeTargets
            };
            _mergeJobHandle = mergeJob.Schedule(SpatialSystem.MAX_EXPS, 64, gridHandle);
            currentDependency = _mergeJobHandle;
        }

        // ── Magnet Job ──────────────────────────────────
        float currentMagnetRadius = 0f;
        if (PlayerStats.Instance != null && PlayerStats.Instance.StatData != null)
        {
            currentMagnetRadius = PlayerStats.Instance.StatData.CurrentMagnetRadius;
        }

        var collectJob = new ExpMagnetJob
        {
            Positions = spatial.ExpPositions,
            Active = spatial.ExpActive,
            Collected = spatial.ExpCollected,
            PlayerPos = spatial.PlayerPosition,
            DeltaTime = Time.deltaTime,
            MagnetRadiusSq = currentMagnetRadius * currentMagnetRadius,
            CollectRadiusSq = CollectRadius * CollectRadius,
            MoveSpeed = MoveSpeed
        };


        JobHandle collectHandle = collectJob.Schedule(SpatialSystem.MAX_EXPS, 64, currentDependency);

        // ── Visual Sync Job ─────────────────────────────
        var syncJob = new ExpVisualSyncJob
        {
            Positions = spatial.ExpPositions,
            Active = spatial.ExpActive
        };

        _lateHandle = syncJob.Schedule(spatial.ExpTransforms, collectHandle);
    }

    private void TryMergeExpGems()
    {
        if (_activeIndices.Count < MergeThreshold) return; 

        _mergeJobHandle.Complete();

        for (int i = SpatialSystem.MAX_EXPS - 1; i >= 0; i--)
        {
            int targetIdx = _mergeTargets[i];
            if (targetIdx != -1)
            {
                SpatialSystem.Instance.ExpValues[targetIdx] += SpatialSystem.Instance.ExpValues[i];
                SpatialSystem.Instance.DeactivateExp(i);

                ExpEntity entity = _activeSlots[i];
                if (entity != null)
                {
                    entity.gameObject.SetActive(false);
                    _activeSlots[i] = null;
                    _expPool.Enqueue(entity);
                }

                if (_activeSlots[targetIdx] != null)
                {
                    _activeSlots[targetIdx].UpdateSprite(GetSpriteForValue(SpatialSystem.Instance.ExpValues[targetIdx]));
                }
            }
        }
    }

    void LateUpdate()
    {
        if (SpatialSystem.Instance == null) return;

        _lateHandle.Complete();

        if (_activeIndices.Count >= MergeThreshold)
        {
            TryMergeExpGems();
        }

        HandleCollection();

        _activeIndices.RemoveAll(idx => !SpatialSystem.Instance.ExpActive[idx]);
    }

    private void HandleCollection()
    {
        var collectedArray = SpatialSystem.Instance.ExpCollected;
        var valueArray = SpatialSystem.Instance.ExpValues;
        var activeArray = SpatialSystem.Instance.ExpActive;

        for (int i = 0; i < _activeIndices.Count; i++)
        {
            int idx = _activeIndices[i];
            if (!activeArray[idx]) continue;

            if (collectedArray[idx])
            {
                int expValue = valueArray[idx];
                if (PlayerStats.Instance != null)
                {
                    PlayerStats.Instance.AddExp(expValue);
                }

                if (CollectSound != null && Time.time - _lastCollectSoundTime > 0.03f)
                {
                    AudioManager.Instance.PlaySFX(CollectSound);
                    _lastCollectSoundTime = Time.time;
                }

                SpatialSystem.Instance.DeactivateExp(idx);

                ExpEntity entity = _activeSlots[idx];
                if (entity != null)
                {
                    entity.IsActive = false;
                    entity.gameObject.SetActive(false);
                    _activeSlots[idx] = null;
                    _expPool.Enqueue(entity);
                }
            }
        }
    }

    // ── Jobs ────────────────────────────────────────────────

    [BurstCompile]
    public struct ExpMergeDetectionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float2> Positions;
        [ReadOnly] public NativeArray<bool> Active;
        [ReadOnly] public NativeParallelMultiHashMap<int, int> Grid;
        [ReadOnly] public float MergeRadiusSq;
        [ReadOnly] public float CellSize;

        public NativeArray<int> MergeTargets;

        public void Execute(int idxA)
        {
            MergeTargets[idxA] = -1;
            if (!Active[idxA]) return;

            float2 posA = Positions[idxA];
            int2 cell = (int2)math.floor(posA / CellSize);

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int hash = SpatialSystem.GetCellHash(new int2(cell.x + x, cell.y + y));
                    if (Grid.TryGetFirstValue(hash, out int idxB, out var it))
                    {
                        do
                        {
                            if (idxA <= idxB || !Active[idxB]) continue;

                            if (math.distancesq(posA, Positions[idxB]) < MergeRadiusSq)
                            {
                                MergeTargets[idxA] = idxB;
                                return;
                            }
                        } while (Grid.TryGetNextValue(out idxB, ref it));
                    }
                }
            }
        }
    }

    [BurstCompile]
    public struct ExpMagnetJob : IJobParallelFor
    {
        public NativeArray<float2> Positions;
        [ReadOnly] public NativeArray<bool> Active;
        [WriteOnly] public NativeArray<bool> Collected;
        
        [ReadOnly] public float2 PlayerPos;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float MagnetRadiusSq;
        [ReadOnly] public float CollectRadiusSq;
        [ReadOnly] public float MoveSpeed;

        public void Execute(int index)
        {
            if (!Active[index]) return;

            float2 pos = Positions[index];
            float2 diff = PlayerPos - pos;
            float distSq = math.lengthsq(diff);

            if (distSq <= CollectRadiusSq)
            {
                Collected[index] = true;
            }
            else if (distSq <= MagnetRadiusSq)
            {
                Collected[index] = false;
                float dist = math.sqrt(distSq);
                float2 dir = diff / dist;
                float speedMultiplier = 1.0f + (1.0f - math.min(distSq / MagnetRadiusSq, 1.0f)) * 2.0f;
                Positions[index] = pos + dir * MoveSpeed * speedMultiplier * DeltaTime;
            }
            else
            {
                Collected[index] = false;
            }
        }
    }

    [BurstCompile]
    public struct ExpVisualSyncJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<float2> Positions;
        [ReadOnly] public NativeArray<bool> Active;

        public void Execute(int index, TransformAccess transform)
        {
            if (!Active[index]) return;
            float2 pos = Positions[index];
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        }
    }
}
