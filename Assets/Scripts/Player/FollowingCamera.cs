using UnityEngine;

public class FollowingCamera : MonoBehaviour
{
    public Transform Target;

    void Awake()
    {
        Target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void LateUpdate()
    {
        transform.position = new Vector3(Target.position.x, Target.position.y, -10);
    }
}
