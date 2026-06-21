using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CharacterSelectionUIManager : MonoBehaviour
{
    [Header("UI Transition References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelRect;

    [Header("Main References")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private GameObject characterItemPrefab;
    [SerializeField] private Button closeButton;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float scaleDuration = 0.3f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    private List<CharacterSelectionItemUI> spawnedItems = new();
    private CharacterData[] loadedCharacters;

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
        if (loadedCharacters == null)
        {
            loadedCharacters = Resources.LoadAll<CharacterData>("SO/Character");
        }

        if (itemsContainer == null || characterItemPrefab == null || loadedCharacters == null) return;

        // 기존 생성된 슬롯 삭제
        foreach (var item in spawnedItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        spawnedItems.Clear();

        // 캐릭터 슬롯 생성 및 초기화
        foreach (var character in loadedCharacters)
        {
            GameObject itemObj = Instantiate(characterItemPrefab, itemsContainer);
            CharacterSelectionItemUI itemUI = itemObj.GetComponent<CharacterSelectionItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(character, OnSelectionChanged);
                spawnedItems.Add(itemUI);
            }
        }
    }

    private void OnSelectionChanged()
    {
        UpdateGoldText();

        // 선택 변경 시 모든 슬롯의 선택/구매 상태 갱신
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                item.UpdateUI();
            }
        }
    }
}
