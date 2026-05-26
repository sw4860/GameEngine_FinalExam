using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;
    public GameObject EnemyPrefab;
    private Transform playerTransform;

    public Queue<EnemyEntity> enemyPool = new Queue<EnemyEntity>();
    public Dictionary<int, EnemyEntity> activeEnemies = new Dictionary<int, EnemyEntity>();
    private List<int> deadIndices = new List<int>();
    private float _spawnTimer;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        PreSpawnPool();
    }

    void PreSpawnPool()
    {
        for (int i = 0; i < SpatialSystem.MAX_ENEMIES; i++)
        {
            GameObject go = Instantiate(EnemyPrefab, transform);
            EnemyEntity entity = go.GetComponent<EnemyEntity>();
            go.SetActive(false);
            enemyPool.Enqueue(entity);
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        HandleSpawning();

        SpatialSystem.Instance.PlayerPosition = new float2(playerTransform.position.x, playerTransform.position.y);
        
        SpatialSystem.Instance.UpdateReadOnlyPositions();

        EnemyMoveJob job = new EnemyMoveJob
        {
            PlayerPos = SpatialSystem.Instance.PlayerPosition,
            DeltaTime = Time.deltaTime,

            EnemyPositions = SpatialSystem.Instance.EnemyPositions,
            ReadOnlyEnemyPositions = SpatialSystem.Instance.ReadOnlyPositions,
            EnemyActive = SpatialSystem.Instance.EnemyActive,
            EnemySpeeds = SpatialSystem.Instance.EnemySpeeds
        };

        JobHandle handle = job.Schedule(SpatialSystem.MAX_ENEMIES, 64);
        handle.Complete();

        SyncVisuals();
    }

    void HandleSpawning()
    {
        if (StageManager.Instance == null) return;

        int currentPhaseIdx = StageManager.Instance.currentPhase;
        PhaseData phase = StageManager.Instance.StageData.phaseDatas[currentPhaseIdx];
        
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= phase.SpawnInterval)
        {
            int deficit = phase.maxEnemyCount - activeEnemies.Count;
            if (deficit > 0)
            {
                int minSpawn = Mathf.Max(1, deficit / 15);
                int maxSpawn = Mathf.Max(2, deficit / 8);
                int spawnCount = UnityEngine.Random.Range(minSpawn, maxSpawn + 1);
                
                spawnCount = Mathf.Min(spawnCount, 500); 

                for (int i = 0; i < spawnCount; i++)
                {
                    EnemyData data = phase.enemyDatas[UnityEngine.Random.Range(0, phase.enemyDatas.Length)];
                    SpawnEnemy(data);
                }
            }
            _spawnTimer = 0f;
        }
    }

    void SpawnEnemy(EnemyData data)
    {
        if (enemyPool.Count == 0) return;

        EnemyEntity entity = enemyPool.Dequeue();
        Vector2 spawnPos = (Vector2)playerTransform.position + UnityEngine.Random.insideUnitCircle.normalized * 15f;
        entity.transform.position = spawnPos;
        entity.gameObject.SetActive(true);
        entity.Init(data);

        if (GameManager.Instance != null)
            GameManager.Instance.AddSpawn();

        if (entity.SpatialIndex != -1)
        {
            activeEnemies[entity.SpatialIndex] = entity;
        }
    }

    void SyncVisuals()
    {
        deadIndices.Clear();

        foreach (var pair in activeEnemies)
        {
            int index = pair.Key;
            EnemyEntity entity = pair.Value;

            if (!entity.gameObject.activeSelf)
            {
                deadIndices.Add(index);
            }
            else
            {
                float2 pos = SpatialSystem.Instance.EnemyPositions[index];
                entity.SetPosition(new Vector2(pos.x, pos.y));
            }
        }

        foreach (int index in deadIndices)
        {
            EnemyEntity entity = activeEnemies[index];
            activeEnemies.Remove(index);
            enemyPool.Enqueue(entity);
        }
    }
}
