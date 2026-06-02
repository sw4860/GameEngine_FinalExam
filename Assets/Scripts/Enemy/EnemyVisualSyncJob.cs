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
        
        float z = math.max(0f, curr.y * 0.001f);
        transform.position = new Vector3(curr.x, curr.y, z);

        FlipLeft[index] = curr.x > PlayerPos.x;
    }
}
