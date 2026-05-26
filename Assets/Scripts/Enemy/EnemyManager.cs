using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;
    public GameObject EnemyPrefab; // Assign in Inspector
    private Transform playerTransform;

    private List<EnemyEntity> enemyPool = new List<EnemyEntity>();
    private Dictionary<int, EnemyEntity> activeEnemies = new Dictionary<int, EnemyEntity>();
    private List<int> deadIndices = new List<int>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (playerTransform == null) return;

        HandleSpawning();

        SpatialSystem.Instance.PlayerPosition = new float2(playerTransform.position.x, playerTransform.position.y);

        EnemyMoveJob job = new EnemyMoveJob
        {
            PlayerPos = SpatialSystem.Instance.PlayerPosition,
            DeltaTime = Time.deltaTime,
            MoveSpeed = 2.0f, 

            EnemyPositions = SpatialSystem.Instance.EnemyPositions,
            EnemyActive = SpatialSystem.Instance.EnemyActive
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
        
        if (activeEnemies.Count < phase.maxEnemyCount)
        {
            SpawnEnemy(phase.enemyDatas[UnityEngine.Random.Range(0, phase.enemyDatas.Length)]);
        }
    }

    void SpawnEnemy(EnemyData data)
    {
        EnemyEntity entity = GetFromPool();
        Vector2 spawnPos = (Vector2)playerTransform.position + UnityEngine.Random.insideUnitCircle.normalized * 15f;
        entity.transform.position = spawnPos;
        entity.gameObject.SetActive(true);
        entity.Init(data);

        if (entity.SpatialIndex != -1)
        {
            activeEnemies[entity.SpatialIndex] = entity;
        }
    }

    EnemyEntity GetFromPool()
    {
        foreach (var e in enemyPool)
        {
            if (!e.gameObject.activeSelf) return e;
        }

        GameObject go = Instantiate(EnemyPrefab, transform);
        EnemyEntity entity = go.GetComponent<EnemyEntity>();
        enemyPool.Add(entity);
        return entity;
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
                entity.transform.position = new Vector3(pos.x, pos.y, 0);
            }
        }

        foreach (int index in deadIndices)
        {
            activeEnemies.Remove(index);
        }
    }
}
