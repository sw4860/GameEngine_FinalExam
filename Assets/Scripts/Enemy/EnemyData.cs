using UnityEngine;

public enum EnemyType { Melee, Ranged }

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "EnemyData")]
public class EnemyData : ScriptableObject
{
    public EnemyType EnemyType;
    public float MoveSpeed;
    public float MaxHp;
    public float AttackRange;
    public float ColliderRadius;
}