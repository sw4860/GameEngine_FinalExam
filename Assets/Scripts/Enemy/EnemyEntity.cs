using Unity.Mathematics;
using UnityEngine;
using DG.Tweening;

public class EnemyEntity : MonoBehaviour
{
    public EnemyData EnemyData;
    public int PoolIndex = -1; 
    public float CurrentHp;
    [HideInInspector] public bool IsActive;
    [HideInInspector] public bool isDying; 
    public float DeathTimer; 

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private MaterialPropertyBlock propBlock;
    private Tween flashTween;
    private static readonly int ColorAnimHash = Shader.PropertyToID("_Color");

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

        if (flashTween != null) flashTween.Kill();
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

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            PlayFlashEffect();

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

    private void PlayFlashEffect()
    {
        if (flashTween != null) flashTween.Kill();

        float flashAmount = 3.0f;
        
        flashTween = DOTween.To(() => flashAmount, x => {
            flashAmount = x;
            if (spriteRenderer != null)
            {
                spriteRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor(ColorAnimHash, new Color(x, x, x, 1));
                spriteRenderer.SetPropertyBlock(propBlock);
            }
        }, 1.0f, 0.1f).SetEase(Ease.OutQuad);
    }

    private void ResetFlash()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor(ColorAnimHash, Color.white);
        spriteRenderer.SetPropertyBlock(propBlock);
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

        IsActive = false;
        isDying = false; 
        if (SpatialSystem.Instance != null && SpatialSystem.Instance.FlipDying.IsCreated)
        {
            SpatialSystem.Instance.FlipDying[PoolIndex] = false;
        }

        if (flashTween != null) flashTween.Kill();
        SpatialSystem.Instance.DeactivateEnemy(PoolIndex);
        gameObject.SetActive(false); 
    }

    void OnDisable()
    {
        if (flashTween != null) flashTween.Kill();
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
