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
    [ReadOnly] public NativeParallelMultiHashMap<int, int> ObstacleGrid;
    [ReadOnly] public NativeArray<ObstacleData> Obstacles;

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

        if (distSq > 0.01f)
        {
            moveDir = toPlayer * math.rsqrt(distSq);
        }

        float2 separation = float2.zero;
        float2 obstacleAvoidance = float2.zero;

        // Spatial Grid - Enemy Separation & Obstacle Avoidance
        int2 cell = (int2)math.floor(pos / SpatialSystem.CELL_SIZE);

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int hash = ( (cell.x + x) * 73856093) ^ ((cell.y + y) * 19349663);
                
                // Enemy Separation
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
                            separation += (diff / d) * (minSepDist - d);
                        }
                    } while (SpatialGrid.TryGetNextValue(out otherIndex, ref itEnemy));
                }

                // Obstacle Avoidance
                if (ObstacleGrid.TryGetFirstValue(hash, out int obsIndex, out var itObs))
                {
                    do
                    {
                        float2 obsPos = Obstacles[obsIndex].Position;
                        float obsRadius = Obstacles[obsIndex].Radius;
                        
                        float2 diff = pos - obsPos;
                        float dist = math.length(diff);
                        float minSafeDist = radius + obsRadius + 0.2f;

                        if (dist < minSafeDist && dist > 0.0001f)
                        {
                            obstacleAvoidance += (diff / dist) * (minSafeDist - dist);
                        }
                    } while (ObstacleGrid.TryGetNextValue(out obsIndex, ref itObs));
                }
            }
        }

        // Normalize forces slightly to prevent explosive movement
        float sepLen = math.length(separation);
        if (sepLen > 1.0f) separation /= sepLen;

        float avdLen = math.length(obstacleAvoidance);
        if (avdLen > 1.0f) obstacleAvoidance /= avdLen;

        float2 finalVelocity = moveDir + separation * SeparationWeight + obstacleAvoidance * ObstacleWeight;
        float finalLenSq = math.lengthsq(finalVelocity);
        
        if (finalLenSq > 0.001f)
        {
            float finalLen = math.sqrt(finalLenSq);
            pos += (finalVelocity / finalLen) * moveSpeed * DeltaTime;
        }

        EnemyPositions[index] = pos;
    }
}
