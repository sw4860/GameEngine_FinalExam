using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class LobbyUpgradeUIManager : MonoBehaviour
{
    [Header("UI Transition References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelRect;

    [Header("Main References")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private GameObject upgradeItemPrefab;
    [SerializeField] private Button closeButton;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float scaleDuration = 0.3f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    private List<LobbyUpgradeItemUI> spawnedItems = new();
    private LobbyUpgradeData[] loadedUpgrades;

    private void Awake()
    {
        // 처음에는 안 보이게 처리
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
    }

    private void OnEnable()
    {
        EventManager.OnGameDataReloaded += HandleDataReloaded;
    }

    private void OnDisable()
    {
        EventManager.OnGameDataReloaded -= HandleDataReloaded;
    }

    private void HandleDataReloaded()
    {
        if (gameObject.activeSelf)
        {
            UpdateGoldText();
            RefreshItems();
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        UpdateGoldText();
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

    private void UpdateGoldText()
    {
        if (goldText != null && GameDataManager.Instance != null)
        {
            goldText.text = $"{GameDataManager.Instance.CurrentData.Gold} G";
        }
    }

    private void RefreshItems()
    {
        if (loadedUpgrades == null)
        {
            loadedUpgrades = Resources.LoadAll<LobbyUpgradeData>("SO/LobbyUpgrades");
        }

        if (itemsContainer == null || upgradeItemPrefab == null || loadedUpgrades == null) return;

        // 기존 생성된 슬롯 삭제
        foreach (var item in spawnedItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        spawnedItems.Clear();

        // 새로 생성 및 초기화
        foreach (var upgrade in loadedUpgrades)
        {
            GameObject itemObj = Instantiate(upgradeItemPrefab, itemsContainer);
            LobbyUpgradeItemUI itemUI = itemObj.GetComponent<LobbyUpgradeItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(upgrade, OnUpgradeCompleted);
                spawnedItems.Add(itemUI);
            }
        }
    }

    private void OnUpgradeCompleted()
    {
        UpdateGoldText();
        
        // 골드 차감 및 레벨 증가 후 모든 UI 항목들의 구매 가능 여부를 갱신
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                item.UpdateUI();
            }
        }
    }
}
