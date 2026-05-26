using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct EnemyMoveJob : IJobParallelFor
{
    [ReadOnly] public float2 PlayerPosition;
    [ReadOnly] public float DeltaTime;
    [ReadOnly] public float MoveSpeed;

    public NativeArray<float2> MonsterPositions;

    public void Execute(int index)
    {
        float2 currentPos = MonsterPositions[index];
        
        float2 direction = PlayerPosition - currentPos;
        
        float distance = math.length(direction);
        if (distance > 0.01f)
        {
            direction /= distance; 
            
            currentPos += direction * MoveSpeed * DeltaTime;
            
            MonsterPositions[index] = currentPos;
        }
    }
}