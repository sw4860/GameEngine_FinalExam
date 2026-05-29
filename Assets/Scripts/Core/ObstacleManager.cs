using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    public static ObstacleManager Instance;

    public List<Prop> ActiveProps = new List<Prop>();
    public NativeArray<ObstacleData> ObstacleDatas;
    private int _lastCount = -1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        // Pre-allocate a reasonable size
        ObstacleDatas = new NativeArray<ObstacleData>(500, Allocator.Persistent);
    }

    void OnDestroy()
    {
        if (ObstacleDatas.IsCreated) ObstacleDatas.Dispose();
    }

    public void UpdateObstacleData()
    {
        int count = 0;
        foreach (var prop in ActiveProps)
        {
            if (prop != null && prop.gameObject.activeInHierarchy) count++;
        }

        // Resize only if needed
        if (count > ObstacleDatas.Length)
        {
            if (ObstacleDatas.IsCreated) ObstacleDatas.Dispose();
            ObstacleDatas = new NativeArray<ObstacleData>(count + 100, Allocator.Persistent);
        }

        int index = 0;
        foreach (var prop in ActiveProps)
        {
            if (prop != null && prop.gameObject.activeInHierarchy)
            {
                ObstacleDatas[index] = new ObstacleData
                {
                    Position = new float2(prop.transform.position.x, prop.transform.position.y),
                    Radius = prop.Radius
                };
                index++;
                if (index >= ObstacleDatas.Length) break;
            }
        }
        _lastCount = count;
    }

    public int GetActiveCount() => _lastCount;
}
