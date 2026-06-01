using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float MoveSpeed;

    [HideInInspector] public Vector2 input;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private string currentAnim;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    public void OnMove(InputValue value)
    {
        input = value.Get<Vector2>();
    }

    void Update()
    {
        UpdateAnimation();
        
        if (SpatialSystem.Instance != null)
        {
            SpatialSystem.Instance.PlayerPosition = new Unity.Mathematics.float2(transform.position.x, transform.position.y);
        }
    }

    private void FixedUpdate()
    {
        float speed = 5f; // 기본값
        if (PlayerStats.Instance != null && PlayerStats.Instance.StatData != null)
        {
            speed = PlayerStats.Instance.StatData.CurrentMoveSpeed;
        }
        rb.linearVelocity = input.normalized * speed;
    }


    private void UpdateAnimation()
    {
        if (animator == null) return;

        string animName = "Idle";
        bool shouldFlip = spriteRenderer.flipX;

        if (input.sqrMagnitude > 0.01f)
        {
            int xDir = input.x > 0.1f ? 1 : (input.x < -0.1f ? -1 : 0);
            int yDir = input.y > 0.1f ? 1 : (input.y < -0.1f ? -1 : 0);

            if (xDir == 0 && yDir == 1) animName = "U_Run";
            else if (xDir == 0 && yDir == -1) animName = "D_Run";
            else if (xDir != 0 && yDir == 0) animName = "R_Run";
            else if (xDir != 0 && yDir == 1) animName = "RU_Run";
            else if (xDir != 0 && yDir == -1) animName = "RD_Run";

            if (xDir != 0) shouldFlip = (xDir == -1);
        }

        spriteRenderer.flipX = shouldFlip;

        if (currentAnim != animName)
        {
            animator.Play(animName);
            currentAnim = animName;
        }
    }
}
