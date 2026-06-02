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
    [ReadOnly] public NativeArray<float> EnemyRadii;

    [ReadOnly] public NativeParallelMultiHashMap<int, int> SpatialGrid;
    [ReadOnly] public NativeArray<ObstacleData> Obstacles;
    [ReadOnly] public int ObstacleCount;

    public float SeparationWeight;
    public float ObstacleWeight;

    public void Execute(int index)
    {
        if (!EnemyActive[index]) return;

        float2 pos = EnemyPositions[index];
        float moveSpeed = EnemySpeeds[index];
        float radius = EnemyRadii[index];
        
        float2 toPlayer = PlayerPos - pos;
        float distSq = math.lengthsq(toPlayer);
        float2 moveDir = float2.zero;

        if (distSq > 0.0001f)
        {
            moveDir = toPlayer * math.rsqrt(distSq);
        }

        float2 separation = float2.zero;
        float2 obstacleAvoidance = float2.zero;

        int2 cell = (int2)math.floor(pos / SpatialSystem.CELL_SIZE);

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int hash = SpatialSystem.GetCellHash(new int2(cell.x + x, cell.y + y));
                
                if (SpatialGrid.TryGetFirstValue(hash, out int otherIndex, out var itEnemy))
                {
                    do
                    {
                        if (otherIndex == index) continue;

                        float2 otherPos = ReadOnlyEnemyPositions[otherIndex];
                        float otherRadius = EnemyRadii[otherIndex];
                        float2 diff = pos - otherPos;
                        float dSq = math.lengthsq(diff);
                        
                        float minSepDist = radius + otherRadius;
                        float minSepDistSq = minSepDist * minSepDist;

                        if (dSq < minSepDistSq && dSq > 0.0001f)
                        {
                            float d = math.sqrt(dSq);
                            float overlap = (minSepDist - d) / minSepDist;
                            separation += (diff / d) * (overlap * overlap);
                        }
                    } while (SpatialGrid.TryGetNextValue(out otherIndex, ref itEnemy));
                }
            }
        }

        for (int i = 0; i < ObstacleCount; i++)
        {
            float2 obsPos = Obstacles[i].Position;
            float obsRadius = Obstacles[i].Radius;
            
            float2 diff = pos - obsPos;
            float dSq = math.lengthsq(diff);
            float minSafeDist = radius + obsRadius + 0.2f;
            float minSafeDistSq = minSafeDist * minSafeDist;

            if (dSq < minSafeDistSq && dSq > 0.0001f)
            {
                float d = math.sqrt(dSq);
                float overlap = (minSafeDist - d) / minSafeDist;
                obstacleAvoidance += (diff / d) * (overlap * overlap);
            }
        }

        float sepLen = math.length(separation);
        if (sepLen > 1.0f) separation /= sepLen;

        float avdLen = math.length(obstacleAvoidance);
        if (avdLen > 1.0f) obstacleAvoidance /= avdLen;

        float2 steering = moveDir + separation * SeparationWeight + obstacleAvoidance * ObstacleWeight;
        float steerLen = math.length(steering);
        
        if (steerLen > 0.001f)
        {
            float currentSpeed = math.min(steerLen, 1.0f) * moveSpeed;
            pos += (steering / steerLen) * currentSpeed * DeltaTime;
        }

        EnemyPositions[index] = pos;
    }
}
