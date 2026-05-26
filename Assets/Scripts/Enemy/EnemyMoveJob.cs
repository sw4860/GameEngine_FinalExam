using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct EnemyMoveJob : IJobParallelFor
{
    [ReadOnly] public float2 PlayerPos;
    [ReadOnly] public float DeltaTime;
    [ReadOnly] public float MoveSpeed;

    public NativeArray<float2> EnemyPositions;
    [ReadOnly] public NativeArray<bool> EnemyActive;

    public void Execute(int index)
    {
        if (!EnemyActive[index]) return;

        float2 pos = EnemyPositions[index];
        
        // Simple move towards player
        float2 toPlayer = PlayerPos - pos;
        float dist = math.length(toPlayer);

        if (dist > 0.1f)
        {
            float2 dir = toPlayer / dist;
            pos += dir * MoveSpeed * DeltaTime;
            EnemyPositions[index] = pos;
        }
    }
}
