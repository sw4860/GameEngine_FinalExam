using UnityEngine;

public enum EnemyType { Melee, Ranged }

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "EnemyData")]
public class EnemyData : ScriptableObject
{
    public EnemyType EnemyType;
    public Sprite EnemySprite;
    public AnimatorOverrideController animOverride;
    public float MoveSpeed;
    public float MaxHp;
    public float AttackRange;
    public float Damage = 5f;
    public float AttackInterval = 1f;
    public float ColliderRadius;
    public bool CanNoclip = false;
    public bool IsBoss = false;
}