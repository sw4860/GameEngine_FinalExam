using Unity.Mathematics;
using UnityEngine;

public class EnemyEntity : MonoBehaviour
{
    public EnemyData EnemyData;
    public int SpatialIndex = -1;
    public float CurrentHp;

    public void Init(EnemyData data)
    {
        this.EnemyData = data;
        this.CurrentHp = data.MaxHp;
        
        // Register for movement job
        SpatialIndex = SpatialSystem.Instance.RegisterEnemy(
            new float2(transform.position.x, transform.position.y)
        );

        // Visuals
        //SpriteRenderer sr = GetComponent<SpriteRenderer>();
        //if (sr != null && data.EnemySprite != null)
        //    sr.sprite = data.EnemySprite;

        Animator anim = GetComponent<Animator>();
        if (anim != null && data.animOverride != null)
            anim.runtimeAnimatorController = data.animOverride;
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
