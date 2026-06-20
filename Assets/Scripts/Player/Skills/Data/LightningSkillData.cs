using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LightningSkill", menuName = "Skills/Lightning")]
public class LightningSkillData : SkillData
{
    [Serializable]
    public struct LightningLevel
    {
        public float Damage;             // 번개 한 발당 공격력
        public int MinStrikeCount;       // 최소 번개 수
        public int MaxStrikeCount;       // 최대 번개 수
        public float StrikeDuration;     // 번개들이 모두 떨어지는 데 걸리는 총 지속 시간
        public float StrikeRadius;       // 번개의 공격 판정 반경
        public float SearchRadius;       // 적을 탐색할 플레이어 기준 반경
        public float Cooldown;           // 스킬 재사용 대기시간
        public BaseStatModifier[] StatModifiers;
        [TextArea] public string LevelDescription;
    }

    [Header("Lightning Prefab & Animation Settings")]
    public GameObject LightningEffectPrefab;  // 번개 이펙트 프리팹
    public bool UseDirectPlay = true;         // true이면 파라미터 대신 지정한 State 이름으로 직접 재생합니다.
    public string AnimParameterName = "AnimIndex"; // 애니메이터의 int형 파라미터명 (기본값: AnimIndex)
    public string AnimStatePrefix = "Lightning_";  // 직접 재생 시 사용할 상태 이름의 접두사 (예: Lightning_0)

    [Header("Lightning Settings")]
    public LightningLevel[] Levels;

    private float _cooldownTimer;

    public override int MaxLevel => Levels.Length;
    private LightningLevel CurrentLevelData => Levels[Mathf.Clamp(CurrentLevel - 1, 0, MaxLevel - 1)];

    public override void OnEquip(GameObject owner)
    {
        base.OnEquip(owner);
        _cooldownTimer = 0f;
        ApplyStatModifiers(CurrentLevelData.StatModifiers);
    }

    public override void OnUpdate(GameObject owner)
    {
        _cooldownTimer += Time.deltaTime;

        float actualCooldown = GetModifiedCooldown(CurrentLevelData.Cooldown);
        if (_cooldownTimer >= actualCooldown)
        {
            _cooldownTimer = 0f;
            TriggerLightningStorm(owner);
        }
    }

    public override void OnLevelUp(GameObject owner)
    {
        base.OnLevelUp(owner);
        ApplyStatModifiers(CurrentLevelData.StatModifiers);
    }

    public override string GetLevelUpDescription()
    {
        if (IsMaxLevel) return "MAX LEVEL";
        return Levels[CurrentLevel].LevelDescription;
    }

    private void TriggerLightningStorm(GameObject owner)
    {
        // PlayerSkillManager의 Coroutine을 빌려서 번개 폭풍 실행
        PlayerSkillManager.Instance.StartCoroutine(SpawnLightningStorm(owner));
    }

    private IEnumerator SpawnLightningStorm(GameObject owner)
    {
        LightningLevel data = CurrentLevelData;
        int count = UnityEngine.Random.Range(data.MinStrikeCount, data.MaxStrikeCount + 1);

        // 1. 플레이어 사정거리 내 가장 가까운 적 N마리 수집 (거리 순 정렬됨)
        List<EnemyEntity> targetEnemies = GetNearestEnemies(owner.transform.position, data.SearchRadius, count);

        for (int i = 0; i < count; i++)
        {
            Vector2 targetPos;

            // 2. 가장 가까운 타겟 적이 살아있다면 해당 적 위치, 없으면 주변 맨땅 무작위 좌표 설정
            if (targetEnemies.Count > 0 && i < targetEnemies.Count)
            {
                var target = targetEnemies[i];
                if (target != null && target.IsActive)
                {
                    targetPos = target.transform.position;
                }
                else
                {
                    // 수집 후 사망 등의 예외 대비
                    Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * data.SearchRadius;
                    targetPos = (Vector2)owner.transform.position + randomOffset;
                }
            }
            else
            {
                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * data.SearchRadius;
                targetPos = (Vector2)owner.transform.position + randomOffset;
            }

            // 3. 개별 번개 즉시 생성 및 판정
            DropSingleLightning(targetPos, data);

            // 4. 번개 폭풍 총 지속 시간에 맞게 랜덤한 딜레이 분할
            float delay = data.StrikeDuration / count;
            yield return new WaitForSeconds(UnityEngine.Random.Range(delay * 0.5f, delay * 1.5f));
        }
    }

    private void DropSingleLightning(Vector2 targetPos, LightningLevel data)
    {
        // 1. 번개 비주얼 이펙트 생성
        if (LightningEffectPrefab != null)
        {
            GameObject fx = Instantiate(LightningEffectPrefab, targetPos, Quaternion.identity);
            
            // 2. Animator 제어 (0~3 중 하나)
            Animator animator = fx.GetComponent<Animator>();
            if (animator != null)
            {
                int randomAnimIndex = UnityEngine.Random.Range(0, 4); // 0, 1, 2, 3
                
                if (UseDirectPlay)
                {
                    // (방안 A) 트랜지션 없이 상태 이름을 직접 재생 (예: Lightning_0, Lightning_1...)
                    string stateName = AnimStatePrefix + randomAnimIndex;
                    animator.Play(stateName, 0, 0f);
                }
                else if (!string.IsNullOrEmpty(AnimParameterName))
                {
                    // (방안 B) 파라미터 기반 트랜지션 재생 (파라미터 변경 후 강제 업데이트)
                    animator.SetInteger(AnimParameterName, randomAnimIndex);
                }
                
                // 생성된 즉시 상태 전이가 평가되어 애니메이션이 1프레임부터 즉시 재생되도록 강제 업데이트
                animator.Update(0f);
            }
        }

        // 3. 데미지 판정 (SpatialSystem 최적화 격자 활용)
        ApplyAreaDamage(targetPos, data);
    }

    private List<EnemyEntity> GetNearestEnemies(Vector2 playerPos, float searchRadius, int maxCount)
    {
        if (SpatialSystem.Instance == null || EnemyManager.Instance == null) return new List<EnemyEntity>();

        // SpatialGrid 읽기 전 비동기 Job 완료 대기
        EnemyManager.Instance.CompleteLateJob();

        List<(EnemyEntity enemy, float distSq)> candidates = new List<(EnemyEntity, float)>();

        int startX = (int)Unity.Mathematics.math.floor((playerPos.x - searchRadius) / SpatialSystem.CELL_SIZE);
        int endX = (int)Unity.Mathematics.math.floor((playerPos.x + searchRadius) / SpatialSystem.CELL_SIZE);
        int startY = (int)Unity.Mathematics.math.floor((playerPos.y - searchRadius) / SpatialSystem.CELL_SIZE);
        int endY = (int)Unity.Mathematics.math.floor((playerPos.y + searchRadius) / SpatialSystem.CELL_SIZE);

        var grid = SpatialSystem.Instance.SpatialGrid;
        var activeSlots = EnemyManager.Instance._activeSlots;
        var enemyPositions = SpatialSystem.Instance.EnemyPositions;
        float radiusSq = searchRadius * searchRadius;

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                int hash = SpatialSystem.GetCellHash(new Unity.Mathematics.int2(x, y));
                if (grid.TryGetFirstValue(hash, out int enemyIndex, out var it))
                {
                    do
                    {
                        EnemyEntity enemy = activeSlots[enemyIndex];
                        if (enemy != null && enemy.IsActive)
                        {
                            Unity.Mathematics.float2 enemyPos = enemyPositions[enemyIndex];
                            float distSq = Unity.Mathematics.math.distancesq(new Unity.Mathematics.float2(playerPos.x, playerPos.y), enemyPos);
                            if (distSq <= radiusSq)
                            {
                                candidates.Add((enemy, distSq));
                            }
                        }
                    } while (grid.TryGetNextValue(out enemyIndex, ref it));
                }
            }
        }

        // 플레이어 기준 거리순(오름차순)으로 정렬
        candidates.Sort((a, b) => a.distSq.CompareTo(b.distSq));

        List<EnemyEntity> found = new List<EnemyEntity>();
        int limit = Mathf.Min(candidates.Count, maxCount);
        for (int i = 0; i < limit; i++)
        {
            found.Add(candidates[i].enemy);
        }

        return found;
    }

    private void ApplyAreaDamage(Vector2 center, LightningLevel data)
    {
        if (SpatialSystem.Instance == null || EnemyManager.Instance == null) return;

        // SpatialGrid 읽기 전 비동기 Job 완료 대기
        EnemyManager.Instance.CompleteLateJob();

        float radius = data.StrikeRadius;
        float radiusSq = radius * radius;
        float finalDamage = data.Damage;

        if (PlayerStats.Instance != null && PlayerStats.Instance.StatData != null)
        {
            finalDamage += PlayerStats.Instance.StatData.CurrentDamage;
        }

        int startX = (int)Unity.Mathematics.math.floor((center.x - radius) / SpatialSystem.CELL_SIZE);
        int endX = (int)Unity.Mathematics.math.floor((center.x + radius) / SpatialSystem.CELL_SIZE);
        int startY = (int)Unity.Mathematics.math.floor((center.y - radius) / SpatialSystem.CELL_SIZE);
        int endY = (int)Unity.Mathematics.math.floor((center.y + radius) / SpatialSystem.CELL_SIZE);

        var grid = SpatialSystem.Instance.SpatialGrid;
        var activeSlots = EnemyManager.Instance._activeSlots;
        var enemyPositions = SpatialSystem.Instance.EnemyPositions;

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                int hash = SpatialSystem.GetCellHash(new Unity.Mathematics.int2(x, y));
                if (grid.TryGetFirstValue(hash, out int enemyIndex, out var it))
                {
                    do
                    {
                        EnemyEntity enemy = activeSlots[enemyIndex];
                        if (enemy != null && enemy.IsActive)
                        {
                            Unity.Mathematics.float2 enemyPos = enemyPositions[enemyIndex];
                            float distSq = Unity.Mathematics.math.distancesq(new Unity.Mathematics.float2(center.x, center.y), enemyPos);
                            if (distSq <= radiusSq)
                            {
                                enemy.TakeDamage(finalDamage);
                            }
                        }
                    } while (grid.TryGetNextValue(out enemyIndex, ref it));
                }
            }
        }
    }
}
