using UnityEngine;
using Unity.Mathematics;

public class AuraSkill : MonoBehaviour
{
    private AuraSkillData _data;
    private float _timer;
    private SpriteRenderer _auraRenderer;

    public void Init(AuraSkillData data)
    {
        _data = data;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (_auraRenderer == null)
        {
            GameObject auraObj = new GameObject("AuraVisual");
            auraObj.transform.SetParent(transform);
            auraObj.transform.localPosition = Vector3.zero;
            _auraRenderer = auraObj.AddComponent<SpriteRenderer>();
        }

        _auraRenderer.sprite = _data.AuraSprite;
        _auraRenderer.color = _data.AuraColor;
        // Assuming sprite is 1x1 unit. Adjust scale to match radius.
        _auraRenderer.transform.localScale = new Vector3(_data.Radius * 2, _data.Radius * 2, 1);
        _auraRenderer.sortingOrder = 5;
    }

    void LateUpdate()
    {
        if (_data == null) return;

        _timer += Time.deltaTime;
        if (_timer >= _data.TickInterval)
        {
            _timer = 0f;
            ApplyDamage();
        }
    }
private void ApplyDamage()
{
    if (SpatialSystem.Instance == null || EnemyManager.Instance == null) return;

    Vector2 myPos = transform.position;
    float radiusSq = _data.Radius * _data.Radius;

    // Efficiently find enemies using SpatialGrid
    int startX = (int)math.floor((myPos.x - _data.Radius) / SpatialSystem.CELL_SIZE);
    int endX = (int)math.floor((myPos.x + _data.Radius) / SpatialSystem.CELL_SIZE);
    int startY = (int)math.floor((myPos.y - _data.Radius) / SpatialSystem.CELL_SIZE);
    int endY = (int)math.floor((myPos.y + _data.Radius) / SpatialSystem.CELL_SIZE);

    var grid = SpatialSystem.Instance.SpatialGrid;
    var activeSlots = EnemyManager.Instance._activeSlots;
    var enemyPositions = SpatialSystem.Instance.EnemyPositions;

    for (int x = startX; x <= endX; x++)
    {
        for (int y = startY; y <= endY; y++)
        {
            int hash = (x * 73856093) ^ (y * 19349663);
            if (grid.TryGetFirstValue(hash, out int enemyIndex, out var it))
            {
                do
                {
                    EnemyEntity enemy = activeSlots[enemyIndex];
                    if (enemy != null && enemy.IsActive)
                    {
                        float2 enemyPos = enemyPositions[enemyIndex];
                        float distSq = math.distancesq(new float2(myPos.x, myPos.y), enemyPos);
                        if (distSq <= radiusSq)
                        {
                            enemy.TakeDamage(_data.Damage);
                        }
                    }
                } while (grid.TryGetNextValue(out enemyIndex, ref it));
            }
        }
    }
}
}
