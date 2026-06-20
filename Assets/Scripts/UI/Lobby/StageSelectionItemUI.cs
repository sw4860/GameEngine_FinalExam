using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageSelectionItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI statusText; // "선택됨", "선택"
    [SerializeField] private Button selectButton;

    private StageData stageData;
    private System.Action onSelectionChanged;

    public void Setup(StageData data, System.Action onSelect)
    {
        stageData = data;
        onSelectionChanged = onSelect;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelectClick);

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (stageData == null || GameDataManager.Instance == null) return;

        bool isSelected = IsStageSelected(stageData);

        if (nameText != null) nameText.text = stageData.StageName;
        if (descText != null)
        {
            descText.text = $"{stageData.Description}\n(플레이 타임: {stageData.EndTime}초)";
        }

        if (thumbnailImage != null && stageData.Thumbnail != null)
        {
            thumbnailImage.sprite = stageData.Thumbnail;
            thumbnailImage.gameObject.SetActive(true);
        }

        // 선택 상태 바인딩
        if (isSelected)
        {
            if (statusText != null) statusText.text = "선택됨";
            if (selectButton != null) selectButton.interactable = false;
        }
        else
        {
            if (statusText != null) statusText.text = "선택";
            if (selectButton != null) selectButton.interactable = true;
        }
    }

    private bool IsStageSelected(StageData data)
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentData == null) return false;
        return GameDataManager.Instance.CurrentData.SelectedStageName == data.name;
    }

    private void OnSelectClick()
    {
        if (stageData == null || GameDataManager.Instance == null) return;

        // 선택한 스테이지 저장
        var currentData = GameDataManager.Instance.CurrentData;
        currentData.SelectedStageName = stageData.name;
        GameDataManager.Instance.SaveGame();

        onSelectionChanged?.Invoke();
    }
}
