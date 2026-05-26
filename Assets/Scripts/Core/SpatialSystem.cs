using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class SpatialSystem : MonoBehaviour
{
    public static SpatialSystem Instance;

    public const int MAX_ENEMIES = 5000;

    public float2 PlayerPosition;
    public NativeArray<float2> EnemyPositions;
    public NativeArray<float2> ReadOnlyPositions;
    public NativeArray<bool> EnemyActive;
    public NativeArray<float> EnemySpeeds;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        EnemyPositions = new NativeArray<float2>(MAX_ENEMIES, Allocator.Persistent);
        ReadOnlyPositions = new NativeArray<float2>(MAX_ENEMIES, Allocator.Persistent);
        EnemyActive = new NativeArray<bool>(MAX_ENEMIES, Allocator.Persistent);
        EnemySpeeds = new NativeArray<float>(MAX_ENEMIES, Allocator.Persistent);
    }

    public void UpdateReadOnlyPositions()
    {
        NativeArray<float2>.Copy(EnemyPositions, ReadOnlyPositions);
    }

    public int RegisterEnemy(float2 pos, float speed)
    {
        for (int i = 0; i < MAX_ENEMIES; i++)
        {
            if (!EnemyActive[i])
            {
                EnemyPositions[i] = pos;
                ReadOnlyPositions[i] = pos;
                EnemySpeeds[i] = speed;
                EnemyActive[i] = true;
                return i;
            }
        }
        return -1;
    }

    public void UnregisterEnemy(int index)
    {
        if (index >= 0 && index < MAX_ENEMIES)
            EnemyActive[index] = false;
    }

    void OnDestroy()
    {
        if (EnemyPositions.IsCreated) EnemyPositions.Dispose();
        if (ReadOnlyPositions.IsCreated) ReadOnlyPositions.Dispose();
        if (EnemyActive.IsCreated) EnemyActive.Dispose();
        if (EnemySpeeds.IsCreated) EnemySpeeds.Dispose();
    }
}
