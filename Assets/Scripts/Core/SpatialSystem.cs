using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class SpatialSystem : MonoBehaviour
{
    public static SpatialSystem Instance;

    public const int   MAX_ENEMIES = 5000;
    public const float CELL_SIZE   = 1.0f;

    public float2 PlayerPosition;

    public NativeArray<float2> EnemyPositions;
    public NativeArray<float2> ReadOnlyPositions;
    public NativeArray<bool>   EnemyActive;
    public NativeArray<float>  EnemySpeeds;
    public NativeArray<float>  EnemyRadii;
    public NativeArray<bool>   PendingDespawn;
    public NativeArray<bool>   FlipDirty;
    public NativeArray<bool>   FlipLeft;
    public TransformAccessArray EnemyTransforms;

    public NativeParallelMultiHashMap<int, int> SpatialGrid;
    public NativeParallelMultiHashMap<int, int> ObstacleGrid;

    // 핵심: 빈 슬롯 인덱스를 미리 관리 — RegisterEnemy O(1)
    private readonly Queue<int> _freeSlots = new Queue<int>(MAX_ENEMIES);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        EnemyPositions    = new NativeArray<float2>(MAX_ENEMIES, Allocator.Persistent);
        ReadOnlyPositions = new NativeArray<float2>(MAX_ENEMIES, Allocator.Persistent);
        EnemyActive       = new NativeArray<bool>  (MAX_ENEMIES, Allocator.Persistent);
        EnemySpeeds       = new NativeArray<float> (MAX_ENEMIES, Allocator.Persistent);
        EnemyRadii        = new NativeArray<float> (MAX_ENEMIES, Allocator.Persistent);
        PendingDespawn    = new NativeArray<bool>  (MAX_ENEMIES, Allocator.Persistent);
        FlipDirty         = new NativeArray<bool>  (MAX_ENEMIES, Allocator.Persistent);
        FlipLeft          = new NativeArray<bool>  (MAX_ENEMIES, Allocator.Persistent);

        EnemyTransforms = new TransformAccessArray(MAX_ENEMIES);
        for (int i = 0; i < MAX_ENEMIES; i++)
        {
            EnemyTransforms.Add(transform); // 더미 transform으로 슬롯 확보
            _freeSlots.Enqueue(i);          // 전체 슬롯을 미리 free로 등록
        }

        SpatialGrid  = new NativeParallelMultiHashMap<int, int>(MAX_ENEMIES * 2, Allocator.Persistent);
        ObstacleGrid = new NativeParallelMultiHashMap<int, int>(1000,            Allocator.Persistent);
    }

    // O(1) — 선형 탐색 완전 제거
    public int RegisterEnemy(Transform trans, float2 pos, float speed, float radius)
    {
        if (_freeSlots.Count == 0) return -1;

        int index = _freeSlots.Dequeue();

        EnemyPositions[index]    = pos;
        ReadOnlyPositions[index] = pos;
        EnemySpeeds[index]       = speed;
        EnemyRadii[index]        = radius;
        EnemyActive[index]       = true;
        EnemyTransforms[index]   = trans;

        return index;
    }

    // O(1) — 해제 즉시 큐에 반납
    public void UnregisterEnemy(int index)
    {
        if (index < 0 || index >= MAX_ENEMIES) return;

        EnemyActive[index]     = false;
        EnemyTransforms[index] = transform; // 더미로 리셋
        _freeSlots.Enqueue(index);          // 슬롯 반납
    }

    public void UpdateReadOnlyPositions()
    {
        NativeArray<float2>.Copy(EnemyPositions, ReadOnlyPositions);
    }

    public static int GetCellHash(float2 pos)
    {
        int2 cell = (int2)math.floor(pos / CELL_SIZE);
        return (cell.x * 73856093) ^ (cell.y * 19349663);
    }

    // ── Jobs ────────────────────────────────────────────────

    [BurstCompile]
    public struct BuildSpatialGridJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float2> Positions;
        [ReadOnly] public NativeArray<bool>   Active;
        public NativeParallelMultiHashMap<int, int>.ParallelWriter Grid;

        public void Execute(int index)
        {
            if (!Active[index]) return;
            int hash = GetCellHash(Positions[index]);
            Grid.Add(hash, index);
        }
    }

    [BurstCompile]
    public struct BuildObstacleGridJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<ObstacleData> Obstacles;
        [ReadOnly] public int Count;
        public NativeParallelMultiHashMap<int, int>.ParallelWriter Grid;

        public void Execute(int index)
        {
            if (index >= Count) return;
            int2 cell = (int2)math.floor(Obstacles[index].Position / CELL_SIZE);
            int  hash = (cell.x * 73856093) ^ (cell.y * 19349663);
            Grid.Add(hash, index);
        }
    }

    [BurstCompile]
    public struct DespawnMarkJob : IJobParallelFor
    {
        [ReadOnly]  public NativeArray<float2> Positions;
        [ReadOnly]  public NativeArray<bool>   Active;
        [ReadOnly]  public float2              PlayerPos;
        [ReadOnly]  public float               DespawnDistSq;
        [WriteOnly] public NativeArray<bool>   PendingDespawn;

        public void Execute(int i)
        {
            if (!Active[i]) { PendingDespawn[i] = false; return; }
            PendingDespawn[i] = math.distancesq(Positions[i], PlayerPos) > DespawnDistSq;
        }
    }

    // ── Cleanup ─────────────────────────────────────────────

    void OnDestroy()
    {
        if (EnemyPositions.IsCreated)    EnemyPositions.Dispose();
        if (ReadOnlyPositions.IsCreated) ReadOnlyPositions.Dispose();
        if (EnemyActive.IsCreated)       EnemyActive.Dispose();
        if (EnemySpeeds.IsCreated)       EnemySpeeds.Dispose();
        if (EnemyRadii.IsCreated)        EnemyRadii.Dispose();
        if (PendingDespawn.IsCreated)    PendingDespawn.Dispose();
        if (FlipDirty.IsCreated)         FlipDirty.Dispose();
        if (FlipLeft.IsCreated)          FlipLeft.Dispose();
        if (SpatialGrid.IsCreated)       SpatialGrid.Dispose();
        if (ObstacleGrid.IsCreated)      ObstacleGrid.Dispose();
        if (EnemyTransforms.isCreated)   EnemyTransforms.Dispose();
    }
}

public struct ObstacleData
{
    public float2 Position;
    public float  Radius;
}