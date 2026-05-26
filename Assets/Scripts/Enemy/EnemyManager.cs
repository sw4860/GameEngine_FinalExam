using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    private Transform playerTransform;
    public GameObject MonsterPrefab;
    public int SpawnCount = 1000;
    public float MonsterSpeed = 2.0f;
    
    private List<Transform> monsterTransforms = new List<Transform>();
    
    private NativeArray<float2> monsterPositions;

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        monsterPositions = new NativeArray<float2>(SpawnCount, Allocator.Persistent);

        for (int i = 0; i < SpawnCount; i++)
        {
            Vector2 randomPos = UnityEngine.Random.insideUnitCircle * 100f;
            GameObject go = Instantiate(MonsterPrefab, randomPos, Quaternion.identity);
            
            monsterTransforms.Add(go.transform);
            monsterPositions[i] = new float2(randomPos.x, randomPos.y);
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        EnemyMoveJob moveJob = new EnemyMoveJob
        {
            PlayerPosition = new float2(playerTransform.position.x, playerTransform.position.y),
            DeltaTime = Time.deltaTime,
            MoveSpeed = MonsterSpeed,
            MonsterPositions = monsterPositions
        };

        JobHandle jobHandle = moveJob.Schedule(SpawnCount, 64);

        jobHandle.Complete();

        for (int i = 0; i < SpawnCount; i++)
        {
            float2 pos = monsterPositions[i];
            monsterTransforms[i].position = new Vector3(pos.x, pos.y, 0);
        }
    }

    void OnDestroy()
    {
        if (monsterPositions.IsCreated)
        {
            monsterPositions.Dispose();
        }
    }
}