using Unity.Mathematics;
using UnityEngine;

public class EnemyEntity : MonoBehaviour
{
    public EnemyData EnemyData;
    public int SpatialIndex = -1;
    public float CurrentHp;

    void Awake()
    {
        // Rigidbody 제거 및 레이어 설정만 유지
        gameObject.layer = 7; // Enemy Layer
    }

    public void Init(EnemyData data)
    {
        this.EnemyData = data;
        this.CurrentHp = data.MaxHp;
        
        // Register for movement job
        SpatialIndex = SpatialSystem.Instance.RegisterEnemy(
            new float2(transform.position.x, transform.position.y),
            data.MoveSpeed
        );

        Animator anim = GetComponent<Animator>();
        if (anim != null && data.animOverride != null)
            anim.runtimeAnimatorController = data.animOverride;
    }

    public void SetPosition(Vector2 pos)
    {
        // 성능을 위해 transform.position 직접 제어
        transform.position = pos;
    }

    public void TakeDamage(float damage)
    {
        CurrentHp -= damage;
        if (CurrentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AddKill();

        gameObject.SetActive(false); // Return to pool
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
