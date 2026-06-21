using UnityEngine;

public interface IDamageText
{
    void Initialize(Vector3 position, float damage);
    bool UpdateTick(float deltaTime);
    void Deactivate();
}
