using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class StageSelectionUIManager : MonoBehaviour
{
    [Header("UI Transition References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelRect;

    [Header("Main References")]
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private GameObject stageItemPrefab;
    [SerializeField] private Button closeButton;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float scaleDuration = 0.3f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    private List<StageSelectionItemUI> spawnedItems = new();
    private StageData[] loadedStages;

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
    }

    private void Start()
    {
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
        if (loadedStages == null)
        {
            loadedStages = Resources.LoadAll<StageData>("SO/StageDatas");
        }

        if (itemsContainer == null || stageItemPrefab == null || loadedStages == null) return;

        // 기존 생성된 슬롯 삭제
        foreach (var item in spawnedItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        spawnedItems.Clear();

        // 스테이지 슬롯 생성 및 초기화
        foreach (var stage in loadedStages)
        {
            GameObject itemObj = Instantiate(stageItemPrefab, itemsContainer);
            StageSelectionItemUI itemUI = itemObj.GetComponent<StageSelectionItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(stage, OnSelectionChanged);
                spawnedItems.Add(itemUI);
            }
        }
    }

    private void OnSelectionChanged()
    {
        // 선택 변경 시 모든 슬롯의 선택 상태 갱신
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                item.UpdateUI();
            }
        }
    }
}
