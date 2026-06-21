using UnityEngine;
using UnityEngine.InputSystem;

public class AchievementTestDebugger : MonoBehaviour
{
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            AchievementManager.Instance.UpdateProgress(AchievementType.TotalKill, 10f);
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            AchievementManager.Instance.UpdateProgress(AchievementType.TotalMoney, 50f);
        }

        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            AchievementManager.Instance.UpdateProgress(AchievementType.SurviveTime, 600f);
        }

        if (keyboard.digit4Key.wasPressedThisFrame)
        {
            AchievementManager.Instance.TriggerAllPendingUnlocks();
        }

        if (keyboard.digit5Key.wasPressedThisFrame)
        {
            AchievementManager.Instance.DebugPrintAllStatus();
        }
    }
}

