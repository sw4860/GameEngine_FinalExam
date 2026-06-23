using UnityEngine;

public class KnifeProjectile : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    private float _damage;
    private float _lifeTime = 5f;
    private float _hitRadius = 0.3f;

    public void Init(Vector3 direction, float speed, float damage, float hitRadius = 0.3f)
    {
        _direction = direction.normalized;
        _speed = speed;
        _damage = damage;
        _hitRadius = hitRadius;
        _lifeTime = 5f;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;

        CheckCollisionWithGrid();

        _lifeTime -= Time.deltaTime;
        if (_lifeTime <= 0f)
        {
            KnifeSkillData.ReturnKnifeToPool(gameObject);
        }
    }

    private void CheckCollisionWithGrid()
    {
        if (SpatialSystem.Instance == null || EnemyManager.Instance == null) return;

        EnemyManager.Instance.CompleteLateJob();

        Vector2 center = transform.position;
        float radiusSq = _hitRadius * _hitRadius;
        float finalDamage = _damage;

        if (PlayerStats.Instance != null && PlayerStats.Instance.StatData != null)
        {
            finalDamage += PlayerStats.Instance.StatData.CurrentDamage;
        }

        var grid = SpatialSystem.Instance.SpatialGrid;
        var activeSlots = EnemyManager.Instance._activeSlots;
        var enemyPositions = SpatialSystem.Instance.EnemyPositions;

        int startX = (int)Unity.Mathematics.math.floor((center.x - _hitRadius) / SpatialSystem.CELL_SIZE);
        int endX = (int)Unity.Mathematics.math.floor((center.x + _hitRadius) / SpatialSystem.CELL_SIZE);
        int startY = (int)Unity.Mathematics.math.floor((center.y - _hitRadius) / SpatialSystem.CELL_SIZE);
        int endY = (int)Unity.Mathematics.math.floor((center.y + _hitRadius) / SpatialSystem.CELL_SIZE);

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
                                KnifeSkillData.ReturnKnifeToPool(gameObject);
                                return;
                            }
                        }
                    } while (grid.TryGetNextValue(out enemyIndex, ref it));
                }
            }
        }
    }
}
