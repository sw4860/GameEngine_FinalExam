using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct EnemyMoveJob : IJobParallelFor
{
    [ReadOnly] public float2 PlayerPos;
    [ReadOnly] public float DeltaTime;

    public NativeArray<float2> EnemyPositions;
    [ReadOnly] public NativeArray<float2> ReadOnlyEnemyPositions;
    [ReadOnly] public NativeArray<bool> EnemyActive;
    [ReadOnly] public NativeArray<float> EnemySpeeds;

    public void Execute(int index)
    {
        if (!EnemyActive[index]) return;

        float2 pos = EnemyPositions[index];
        float moveSpeed = EnemySpeeds[index];
        
        float2 toPlayer = PlayerPos - pos;
        float distSq = math.lengthsq(toPlayer);
        float2 moveDir = float2.zero;

        if (distSq > 0.01f)
        {
            moveDir = toPlayer * math.rsqrt(distSq);
        }

        float2 separation = float2.zero;
        const float separationRadius = 0.4f;
        const float separationRadiusSq = separationRadius * separationRadius;

        // 적끼리의 거리 유지를 위한 단순 루프 (Burst가 최대한 최적화하도록 유도)
        for (int i = 0; i < ReadOnlyEnemyPositions.Length; i++)
        {
            if (i == index || !EnemyActive[i]) continue;

            float2 otherPos = ReadOnlyEnemyPositions[i];
            float2 diff = pos - otherPos;
            float dSq = math.lengthsq(diff);

            if (dSq < separationRadiusSq && dSq > 0.0001f)
            {
                float d = math.sqrt(dSq);
                separation += (diff / d) * (separationRadius - d);
            }
        }

        float2 finalVelocity = moveDir + separation * 5.0f;
        float finalLenSq = math.lengthsq(finalVelocity);
        
        if (finalLenSq > 0.001f)
        {
            pos += (finalVelocity * math.rsqrt(finalLenSq)) * moveSpeed * DeltaTime;
        }

        EnemyPositions[index] = pos;
    }
}
