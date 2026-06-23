using UnityEngine;

public class BibleProjectile : MonoBehaviour
{
    private float _damage;

    public void Init(float damage)
    {
        _damage = damage;
    }
}
