using Unity.Mathematics;
using UnityEngine;
using DG.Tweening;

public class EnemyEntity : MonoBehaviour
{
    public EnemyData EnemyData;
    public AudioClip HitSound;
    public int PoolIndex = -1; 
    public float CurrentHp;
    [HideInInspector] public bool IsActive;
    [HideInInspector] public bool isDying; 
    public float DeathTimer; 

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private MaterialPropertyBlock propBlock;
    private static readonly int FlashHash = Shader.PropertyToID("_Flash");
    private static float _lastGlobalHitSoundTime = -1f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        propBlock = new MaterialPropertyBlock();
        gameObject.layer = 7;
    }

    public void Init(EnemyData data)
    {
        IsActive = true;
        isDying = false;
        DeathTimer = 0f;

        this.EnemyData = data;
        this.CurrentHp = data.MaxHp;

        ResetFlash();

        SpatialSystem.Instance.ActivateEnemy(
            PoolIndex,
            new float2(transform.position.x, transform.position.y),
            data.MoveSpeed,
            data.ColliderRadius > 0 ? data.ColliderRadius : 0.4f
        );

        if (animator != null)
        {
            if (data.animOverride != null)
                animator.runtimeAnimatorController = data.animOverride;

            if (animator.runtimeAnimatorController != null)
            {
                animator.SetBool("Death", false);
            }
        }
    }

    public void ApplyVisuals(bool flipX)
    {
        if (spriteRenderer.flipX != flipX)
        {
            spriteRenderer.flipX = flipX;
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsActive || isDying) return; 
        CurrentHp -= damage;

        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.SpawnDamageText(transform.position, damage);
        }

        if (HitSound != null && Time.time - _lastGlobalHitSoundTime > 0.03f)
        {
            AudioManager.Instance.PlaySFX(HitSound);
            _lastGlobalHitSoundTime = Time.time;
        }

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.TriggerFlash(PoolIndex);
            }

            if (CurrentHp <= 0)
            {
                if (EnemyManager.Instance != null) EnemyManager.Instance.CompleteLateJob();

                isDying = true;
                IsActive = true; 
                DeathTimer = 0.5f; 
                animator.SetBool("Death", true);

                SpatialSystem.Instance.DeactivateEnemy(PoolIndex);
                if (SpatialSystem.Instance.FlipDying.IsCreated)
                {
                    SpatialSystem.Instance.FlipDying[PoolIndex] = true;
                }

                EventManager.OnEnemyDeath?.Invoke();
                EnemyManager.Instance.RegisterDyingEnemy(PoolIndex);
            }
        }
        else if (CurrentHp <= 0)
        {
            Die();
        }
    }

    public void SetFlash(float value)
    {
        if (spriteRenderer == null) return;
        propBlock.SetFloat(FlashHash, value);
        spriteRenderer.SetPropertyBlock(propBlock);
    }

    public void ResetFlash()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.SetPropertyBlock(null);
    }

    public void Die()
    {
        if (!isDying) return; 

        if (EnemyManager.Instance != null) EnemyManager.Instance.CompleteLateJob();

        if (ExpManager.Instance != null)
        {
            int expValue = UnityEngine.Random.Range(1, 4);
            ExpManager.Instance.SpawnExp(new float2(transform.position.x, transform.position.y), expValue);
        }

        if (EnemyData != null && EnemyData.IsBoss)
        {
            if (CursedContractManager.Instance != null)
            {
                CursedContractManager.Instance.SpawnCursedChest(transform.position);
            }
        }

        IsActive = false;
        isDying = false; 
        if (SpatialSystem.Instance != null && SpatialSystem.Instance.FlipDying.IsCreated)
        {
            SpatialSystem.Instance.FlipDying[PoolIndex] = false;
        }

        SpatialSystem.Instance.DeactivateEnemy(PoolIndex);
        gameObject.SetActive(false); 
    }

    void OnDisable()
    {
        if (PoolIndex != -1 && SpatialSystem.Instance != null)
        {
            SpatialSystem.Instance.DeactivateEnemy(PoolIndex);
            if (SpatialSystem.Instance.FlipDying.IsCreated)
            {
                SpatialSystem.Instance.FlipDying[PoolIndex] = false;
            }
        }
    }
}
