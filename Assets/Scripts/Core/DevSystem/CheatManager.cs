using UnityEngine;
using UnityEngine.InputSystem;

public class CheatManager : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.ElapsedTime += 10f;
                Debug.Log($"[Cheat] Time added: 10s.");
            }
        }

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            EnemyEntity[] enemies = FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy.gameObject.activeSelf)
                {
                    enemy.TakeDamage(999999f);
                }
            }
            Debug.Log($"[Cheat] All active enemies killed.");
        }
    }
#endif
}
