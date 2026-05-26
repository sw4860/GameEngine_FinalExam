using UnityEngine;

public class EnemyEntity : MonoBehaviour
{
    public EnemyData EnemyData;
    private float currentHp;
    private int managerIndex;
    private EnemyManager manager;

    public void Init(EnemyData data, int index, EnemyManager manager)
    {
        this.EnemyData = data;
        this.currentHp = data.MaxHp;
        this.managerIndex = index;
        this.manager = manager;
    }

    public void TakeDamage(float damage)
    {
        currentHp -= damage;
        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
