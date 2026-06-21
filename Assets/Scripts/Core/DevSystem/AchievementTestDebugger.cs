using UnityEngine;
using UnityEngine.InputSystem;

public class AchievementTestDebugger : MonoBehaviour
{
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (AchievementPresenter.Instance == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            AchievementData rawNormal = ScriptableObject.CreateInstance<AchievementData>();
            rawNormal.InitForDebug(991, "일반 업적 해금!", AchievementGrade.Normal);
            AchievementPresenter.Instance.PlayPresentation(rawNormal);
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            AchievementData rawChallenge = ScriptableObject.CreateInstance<AchievementData>();
            rawChallenge.InitForDebug(992, "챌린지 업적 해금!", AchievementGrade.Challenge);
            AchievementPresenter.Instance.PlayPresentation(rawChallenge);
        }
    }
}

