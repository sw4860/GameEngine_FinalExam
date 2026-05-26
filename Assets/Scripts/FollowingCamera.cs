using UnityEngine;

public class FollowingCamera : MonoBehaviour
{
    public Transform Target;

    void LateUpdate()
    {
        transform.position = new Vector3(Target.position.x, Target.position.y, -10);
    }
}
