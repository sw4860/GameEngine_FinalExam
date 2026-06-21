using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUpgradeItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button upgradeButton;

    private LobbyUpgradeData upgradeData;
    private System.Action onUpgradeSuccess;

    public void Setup(LobbyUpgradeData data, System.Action onUpgrade)
    {
        upgradeData = data;
        onUpgradeSuccess = onUpgrade;

        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(OnUpgradeClick);

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (upgradeData == null || GameDataManager.Instance == null) return;

        var currentData = GameDataManager.Instance.CurrentData;
        int currentLevel = upgradeData.GetCurrentLevel(currentData);
        int maxLevel = upgradeData.MaxLevel;

        // 기본 정보 바인딩
        if (nameText != null) nameText.text = upgradeData.UpgradeName;
        if (iconImage != null)
        {
            iconImage.sprite = upgradeData.Icon;
            iconImage.gameObject.SetActive(upgradeData.Icon != null);
        }

        // 레벨 텍스트 바인딩
        if (levelText != null) levelText.text = $"Lv. {currentLevel} / {maxLevel}";

        // 능력치 변화 설명 텍스트
        if (descText != null)
        {
            float currentBonus = upgradeData.GetBonusForLevel(currentLevel);
            string currentBonusStr = FormatValue(currentBonus, upgradeData.IsPercentage);

            if (currentLevel < maxLevel)
            {
                float nextBonus = upgradeData.GetBonusForLevel(currentLevel + 1);
                string nextBonusStr = FormatValue(nextBonus, upgradeData.IsPercentage);
                descText.text = $"{currentBonusStr} → {nextBonusStr}";
            }
            else
            {
                descText.text = $"{currentBonusStr} (MAX)";
            }
        }

        // 비용 및 구매 버튼 상태 바인딩
        if (currentLevel < maxLevel)
        {
            int cost = upgradeData.GetCostForNextLevel(currentLevel);
            if (costText != null) costText.text = $"{cost} G";

            // 골드 부족 시 버튼 비활성화
            if (upgradeButton != null)
            {
                upgradeButton.interactable = currentData.Gold >= cost;
            }
        }
        else
        {
            if (costText != null) costText.text = "MAX";
            if (upgradeButton != null)
            {
                upgradeButton.interactable = false;
            }
        }
    }

    private void OnUpgradeClick()
    {
        if (upgradeData == null || GameDataManager.Instance == null) return;

        var currentData = GameDataManager.Instance.CurrentData;
        int currentLevel = upgradeData.GetCurrentLevel(currentData);

        if (currentLevel >= upgradeData.MaxLevel) return;

        int cost = upgradeData.GetCostForNextLevel(currentLevel);
        if (currentData.Gold >= cost)
        {
            GameDataManager.Instance.ConsumeGold(cost);
            upgradeData.SetCurrentLevel(currentData, currentLevel + 1);
            
            GameDataManager.Instance.SaveGame();

            // 효과음 재생 등을 위해 콜백 실행
            onUpgradeSuccess?.Invoke();
        }
    }

    private string FormatValue(float value, bool isPercentage)
    {
        if (isPercentage)
        {
            // 예: 0.1f -> "+10%"
            return $"+{value * 100:0}%";
        }
        else
        {
            // 예: 10f -> "+10"
            return $"+{value:0}";
        }
    }
}
