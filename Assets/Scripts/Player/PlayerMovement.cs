using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float MoveSpeed;

    private Rigidbody2D rb;
    [HideInInspector] public Vector2 input;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void OnMove(InputValue value)
    {
        input = value.Get<Vector2>().normalized;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = input * MoveSpeed;
    }
}
