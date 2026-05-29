using Unity.Mathematics;
using UnityEngine;

public class EnemyEntity : MonoBehaviour
{
    public EnemyData EnemyData;
    public int SpatialIndex = -1;
    public float CurrentHp;
    [HideInInspector] public bool IsActive;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        gameObject.layer = 7;
    }

    public void Init(EnemyData data)
    {
        IsActive = true;

        if (SpatialIndex != -1)
        {
            SpatialSystem.Instance.UnregisterEnemy(SpatialIndex);
            SpatialIndex = -1;
        }

        this.EnemyData = data;
        this.CurrentHp = data.MaxHp;

        SpatialIndex = SpatialSystem.Instance.RegisterEnemy(
            transform,
            new float2(transform.position.x, transform.position.y),
            data.MoveSpeed,
            data.ColliderRadius > 0 ? data.ColliderRadius : 0.4f
        );

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            if (data.animOverride != null)
                anim.runtimeAnimatorController = data.animOverride;
        }
    }

    // flip만 담당 — transform 접근 없음
    public void ApplyVisuals(bool flipX)
    {
        spriteRenderer.flipX = flipX;
    }

    public void SetSortingOrder(int order)
    {
        spriteRenderer.sortingOrder = order;
    }

    public void TakeDamage(float damage)
    {
        if (!IsActive) return;
        CurrentHp -= damage;
        if (CurrentHp <= 0) Die();
    }

    private void Die()
    {
        if (!IsActive) return;
        EventManager.OnEnemyDeath?.Invoke();
        // gameObject.SetActive는 EnemyManager.HandleCleanup에서 일괄 처리
        // 여기서 호출하면 OnDisable → UnregisterEnemy 타이밍 꼬임
        IsActive = false;
    }

    void OnDisable()
    {
        if (SpatialIndex != -1)
        {
            SpatialSystem.Instance.UnregisterEnemy(SpatialIndex);
            SpatialIndex = -1;
        }
    }
}
