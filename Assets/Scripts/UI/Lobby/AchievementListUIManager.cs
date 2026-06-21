using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class AchievementListUIManager : MonoBehaviour
{
    [Header("UI Transition")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelRect;

    [Header("Main Grid")]
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private GameObject achievementItemPrefab;
    [SerializeField] private Button closeButton;

    [Header("Tooltip Panel")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipTitleText;
    [SerializeField] private TextMeshProUGUI tooltipGradeText;
    [SerializeField] private TextMeshProUGUI tooltipDescText;
    [SerializeField] private TextMeshProUGUI tooltipStatusText;

    [Header("Animations")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float scaleDuration = 0.3f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    private readonly List<AchievementListItemUI> spawnedItems = new();
    private AchievementData[] loadedAchievements;

    private void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        if (panelRect != null)
        {
            panelRect.localScale = Vector3.zero;
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }

        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        RefreshItems();

        canvasGroup.DOKill();
        panelRect.DOKill();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
        }
        if (panelRect != null)
        {
            panelRect.DOScale(Vector3.one, scaleDuration).SetEase(easeType).SetUpdate(true);
        }
    }

    public void Hide()
    {
        HideTooltip();

        canvasGroup.DOKill();
        panelRect.DOKill();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.DOFade(0f, fadeDuration).SetUpdate(true);
        }
        if (panelRect != null)
        {
            panelRect.DOScale(Vector3.zero, scaleDuration).SetEase(Ease.InQuad).SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }

    private void RefreshItems()
    {
        if (loadedAchievements == null)
        {
            loadedAchievements = Resources.LoadAll<AchievementData>("SO/AchievementDatas");
        }

        if (itemsContainer == null || achievementItemPrefab == null || loadedAchievements == null) return;

        List<AchievementData> sortedList = new List<AchievementData>(loadedAchievements);
        sortedList.Sort((a, b) => a.Id.CompareTo(b.Id));

        foreach (var item in spawnedItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        spawnedItems.Clear();

        HashSet<string> unlockedSet = new HashSet<string>();
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentData != null)
        {
            foreach (var key in GameDataManager.Instance.CurrentData.UnlockedAchievements)
            {
                unlockedSet.Add(key);
            }
        }

        foreach (var ach in sortedList)
        {
            GameObject itemObj = Instantiate(achievementItemPrefab, itemsContainer);
            AchievementListItemUI itemUI = itemObj.GetComponent<AchievementListItemUI>();
            if (itemUI != null)
            {
                string key = ach.Id > 0 ? ach.Id.ToString() : ach.name;
                bool unlocked = unlockedSet.Contains(key);
                itemUI.Setup(ach, unlocked, ShowTooltip, HideTooltip);
                spawnedItems.Add(itemUI);
            }
        }
    }

    private void ShowTooltip(AchievementData data, bool unlocked, RectTransform itemRect)
    {
        if (tooltipPanel == null) return;

        if (tooltipTitleText != null)
        {
            tooltipTitleText.text = data.Title;
        }

        if (tooltipGradeText != null)
        {
            tooltipGradeText.text = data.Grade == AchievementGrade.Challenge ? "<color=#FF4500>[Challenge]</color>" : "<color=#FFD700>[Normal]</color>";
        }

        if (tooltipDescText != null)
        {
            tooltipDescText.text = data.Description;
        }

        if (tooltipStatusText != null)
        {
            tooltipStatusText.text = unlocked ? "<color=#00FF00>Unlocked</color>" : "<color=#FF3333>Locked</color>";
        }

        tooltipPanel.SetActive(true);

        Vector3[] corners = new Vector3[4];
        itemRect.GetWorldCorners(corners);
        Vector3 rightPosition = (corners[1] + corners[2]) * 0.5f;

        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        if (tooltipRect != null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, 
                    RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rightPosition), 
                    canvas.worldCamera, 
                    out Vector2 localPoint
                );
                
                tooltipRect.anchoredPosition = localPoint + new Vector2(25f, 0f);
            }
        }
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}
