using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct EnemyVisualSyncJob : IJobParallelForTransform
{
    [ReadOnly] public NativeArray<float2> EnemyPositions;
    [ReadOnly] public NativeArray<bool>   EnemyActive;
    [ReadOnly] public NativeArray<bool>   EnemyDying;
    [ReadOnly] public float2              PlayerPos;

    public NativeArray<bool> FlipLeft;

    public void Execute(int index, TransformAccess transform)
    {
        bool process = EnemyActive[index] || EnemyDying[index];
        if (!process) return;

        float2 curr = EnemyPositions[index];
        
        transform.position = new Vector3(curr.x, curr.y, curr.y * 0.001f);

        FlipLeft[index] = curr.x > PlayerPos.x;
    }
}
