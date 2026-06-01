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

    public const int MAX_ENEMIES = 4000;
    public const float CELL_SIZE = 1.0f;

    public float2 PlayerPosition;

    public NativeArray<float2> EnemyPositions;
    public NativeArray<float2> ReadOnlyPositions;
    public NativeArray<bool>   EnemyActive;
    public NativeArray<float>  EnemySpeeds;
    public NativeArray<float>  EnemyRadii;
    public NativeArray<bool>   PendingDespawn;
    public NativeArray<bool>   FlipDirty;
    public NativeArray<bool>   FlipLeft;
    public NativeArray<bool>   FlipDying; // 사망 중 상태 추가
    public TransformAccessArray EnemyTransforms;

    public NativeParallelMultiHashMap<int, int> SpatialGrid;
    public NativeParallelMultiHashMap<int, int> ObstacleGrid;
    public NativeParallelMultiHashMap<int, int> ExpGrid;

    // ── Exp System Data ─────────────────────────────────────
    public const int MAX_EXPS = 5000;
    public NativeArray<float2> ExpPositions;
    public NativeArray<bool> ExpActive;
    public NativeArray<int> ExpValues;
    public NativeArray<bool> ExpCollected;
    public TransformAccessArray ExpTransforms;

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
        FlipDying         = new NativeArray<bool>  (MAX_ENEMIES, Allocator.Persistent);

        EnemyTransforms = new TransformAccessArray(MAX_ENEMIES);

        SpatialGrid  = new NativeParallelMultiHashMap<int, int>(MAX_ENEMIES * 2, Allocator.Persistent);
        ObstacleGrid = new NativeParallelMultiHashMap<int, int>(2000,            Allocator.Persistent);
        ExpGrid      = new NativeParallelMultiHashMap<int, int>(MAX_EXPS * 2,    Allocator.Persistent);

        // Exp Data Init
        ExpPositions  = new NativeArray<float2>(MAX_EXPS, Allocator.Persistent);
        ExpActive     = new NativeArray<bool>  (MAX_EXPS, Allocator.Persistent);
        ExpValues     = new NativeArray<int>   (MAX_EXPS, Allocator.Persistent);
        ExpCollected  = new NativeArray<bool>  (MAX_EXPS, Allocator.Persistent);
        ExpTransforms = new TransformAccessArray(MAX_EXPS);
        for (int i = 0; i < MAX_EXPS; i++)
        {
            ExpTransforms.Add(transform);
        }
    }

    public void PreRegisterEnemyTransform(Transform trans)
    {
        EnemyTransforms.Add(trans);
    }

    public void ActivateEnemy(int index, float2 pos, float speed, float radius)
    {
        if (index < 0 || index >= MAX_ENEMIES) return;

        EnemyPositions[index]    = pos;
        ReadOnlyPositions[index] = pos;
        EnemySpeeds[index]       = speed;
        EnemyRadii[index]        = radius;
        EnemyActive[index]       = true;
    }

    public void DeactivateEnemy(int index)
    {
        if (index < 0 || index >= MAX_ENEMIES) return;
        EnemyActive[index] = false;
    }

    public void ActivateExp(int index, float2 pos, int value)
    {
        if (index < 0 || index >= MAX_EXPS) return;

        ExpPositions[index]  = pos;
        ExpValues[index]     = value;
        ExpActive[index]     = true;
        ExpCollected[index]  = false;
    }

    public void DeactivateExp(int index)
    {
        if (index < 0 || index >= MAX_EXPS) return;
        ExpActive[index] = false;
    }

    public void SetExpTransform(int index, Transform trans)
    {
        if (index < 0 || index >= MAX_EXPS) return;
        ExpTransforms[index] = trans;
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
    public struct BuildExpGridJob : IJobParallelFor
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
        if (FlipDying.IsCreated)         FlipDying.Dispose();
        if (SpatialGrid.IsCreated)       SpatialGrid.Dispose();
        if (ObstacleGrid.IsCreated)      ObstacleGrid.Dispose();
        if (EnemyTransforms.isCreated)   EnemyTransforms.Dispose();

        if (ExpGrid.IsCreated)           ExpGrid.Dispose();

        if (ExpPositions.IsCreated)      ExpPositions.Dispose();
        if (ExpActive.IsCreated)         ExpActive.Dispose();
        if (ExpValues.IsCreated)         ExpValues.Dispose();
        if (ExpCollected.IsCreated)      ExpCollected.Dispose();
        if (ExpTransforms.isCreated)     ExpTransforms.Dispose();
    }
}

public struct ObstacleData
{
    public float2 Position;
    public float Radius;
}
