using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct EnemyVisualSyncJob : IJobParallelForTransform
{
    [ReadOnly] public NativeArray<float2> EnemyPositions;
    [ReadOnly] public NativeArray<float2> PrevPositions;   // ReadOnlyPositions (이전 프레임)
    [ReadOnly] public NativeArray<bool>   EnemyActive;

    // 출력 — 메인스레드에서 transform 접근 없이 flip 처리
    [WriteOnly] public NativeArray<bool> FlipDirty;
    [WriteOnly] public NativeArray<bool> FlipLeft;

    public void Execute(int index, TransformAccess transform)
    {
        if (!EnemyActive[index])
        {
            FlipDirty[index] = false;
            return;
        }

        float2 curr = EnemyPositions[index];
        float2 prev = PrevPositions[index];

        // 위치 동기화 (기존과 동일)
        transform.position = new Vector3(curr.x, curr.y, 0f);

        // flip 판단을 Job 안에서 처리
        float diffX = curr.x - prev.x;
        bool dirty = math.abs(diffX) > 0.01f;
        FlipDirty[index] = dirty;
        FlipLeft[index]  = diffX < 0f;
    }
}