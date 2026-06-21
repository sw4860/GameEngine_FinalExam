using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AchievementListItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite normalBackground;
    [SerializeField] private Sprite challengeBackground;
    [SerializeField] private GameObject lockOverlay;

    private AchievementData achievementData;
    private bool isUnlocked;
    private System.Action<AchievementData, bool, RectTransform> onHoverEnter;
    private System.Action onHoverExit;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(AchievementData data, bool unlocked, System.Action<AchievementData, bool, RectTransform> hoverEnter, System.Action hoverExit)
    {
        achievementData = data;
        isUnlocked = unlocked;
        onHoverEnter = hoverEnter;
        onHoverExit = hoverExit;

        if (backgroundImage != null)
        {
            backgroundImage.sprite = data.Grade == AchievementGrade.Challenge ? challengeBackground : normalBackground;
        }

        if (iconImage != null)
        {
            iconImage.sprite = data.Icon;
            iconImage.color = unlocked ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1f);
        }

        if (lockOverlay != null)
        {
            lockOverlay.SetActive(!unlocked);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onHoverEnter?.Invoke(achievementData, isUnlocked, rectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onHoverExit?.Invoke();
    }
}
