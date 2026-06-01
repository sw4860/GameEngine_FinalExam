using UnityEngine;

public class Prop : MonoBehaviour
{
    public float Radius = 0.5f;

    void OnEnable()
    {
        if (ObstacleManager.Instance != null && !ObstacleManager.Instance.ActiveProps.Contains(this))
        {
            ObstacleManager.Instance.ActiveProps.Add(this);
        }
    }

    void OnDisable()
    {
        if (ObstacleManager.Instance != null)
        {
            ObstacleManager.Instance.ActiveProps.Remove(this);
        }
    }
}
