using UnityEngine;
using UnityEngine.InputSystem;

public class CheatManager : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void Update()
    {
        // T 키를 누르면 현재 시간 +10초 (페이즈 테스트용)
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.ElapsedTime += 10f;
                Debug.Log($"[Cheat] Time added: 10s. Current Time: {StageManager.Instance.ElapsedTime}");
            }
        }

        // K 키를 누르면 현재 활성화된 모든 적 제거 (킬 수 테스트용)
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
