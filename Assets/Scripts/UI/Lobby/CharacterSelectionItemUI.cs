using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectionItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image characterIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI skillText;
    [SerializeField] private TextMeshProUGUI statusText; // "선택됨", "선택", "구매 (1000G)", "업적 잠금"
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private Button actionButton;

    private CharacterData characterData;
    private System.Action onSelectionChanged;

    public void Setup(CharacterData data, System.Action onSelect)
    {
        characterData = data;
        onSelectionChanged = onSelect;

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnActionClick);

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (characterData == null || GameDataManager.Instance == null) return;

        var currentData = GameDataManager.Instance.CurrentData;
        bool isUnlocked = IsCharacterUnlocked(characterData, currentData);
        bool isSelected = IsCharacterSelected(characterData);

        if (nameText != null) nameText.text = characterData.CharacterName;
        if (characterIcon != null && characterData.CharacterIcon != null)
        {
            characterIcon.sprite = characterData.CharacterIcon;
            characterIcon.gameObject.SetActive(true);
        }

        // 시작 스킬 표시
        if (skillText != null)
        {
            if (characterData.BaseSkill != null)
            {
                skillText.text = $"시작 스킬: {characterData.BaseSkill.SkillName}";
            }
            else
            {
                skillText.text = "시작 스킬: 없음";
            }
        }

        // 잠금 오버레이 활성화 여부
        if (lockOverlay != null)
        {
            lockOverlay.SetActive(!isUnlocked);
        }

        // 현재 상태 분기 처리
        if (isSelected)
        {
            if (statusText != null) statusText.text = "선택됨";
            if (actionButton != null) actionButton.interactable = false;
        }
        else if (isUnlocked)
        {
            if (statusText != null) statusText.text = "선택";
            if (actionButton != null) actionButton.interactable = true;
        }
        else
        {
            // 잠금 상태 분기
            if (!string.IsNullOrEmpty(characterData.UnlockAchievementId))
            {
                // 업적 해금 캐릭터
                if (statusText != null) statusText.text = $"업적 해금 필요\n({characterData.UnlockAchievementId})";
                if (actionButton != null) actionButton.interactable = false;
            }
            else
            {
                // 골드 구매 캐릭터
                if (statusText != null) statusText.text = $"구매 ({characterData.UnlockGoldCost} G)";
                if (actionButton != null)
                {
                    actionButton.interactable = currentData.Gold >= characterData.UnlockGoldCost;
                }
            }
        }
    }

    private bool IsCharacterUnlocked(CharacterData data, GameData save)
    {
        if (data.IsDefaultUnlocked) return true;

        // 1. 업적으로 해금되었는지 검사
        if (!string.IsNullOrEmpty(data.UnlockAchievementId))
        {
            return save.UnlockedAchievements.Contains(data.UnlockAchievementId);
        }

        // 2. 골드로 구매하여 해금되었는지 검사
        return save.UnlockedCharacters.Contains(data.name);
    }

    private bool IsCharacterSelected(CharacterData data)
    {
        if (CharacterManager.Instance == null || CharacterManager.Instance.SelectedCharacter == null) return false;
        return CharacterManager.Instance.SelectedCharacter.name == data.name;
    }

    private void OnActionClick()
    {
        if (characterData == null || GameDataManager.Instance == null) return;

        var currentData = GameDataManager.Instance.CurrentData;
        bool isUnlocked = IsCharacterUnlocked(characterData, currentData);

        if (isUnlocked)
        {
            // 캐릭터 선택 처리
            if (CharacterManager.Instance != null)
            {
                CharacterManager.Instance.SelectCharacter(characterData);
                onSelectionChanged?.Invoke();
            }
        }
        else
        {
            // 골드로 캐릭터 구매 처리
            if (string.IsNullOrEmpty(characterData.UnlockAchievementId) && characterData.UnlockGoldCost > 0)
            {
                if (currentData.Gold >= characterData.UnlockGoldCost)
                {
                    currentData.Gold -= characterData.UnlockGoldCost;
                    currentData.UnlockedCharacters.Add(characterData.name);
                    
                    GameDataManager.Instance.SaveGame();

                    // 구매 완료 후 자동 선택
                    if (CharacterManager.Instance != null)
                    {
                        CharacterManager.Instance.SelectCharacter(characterData);
                    }

                    onSelectionChanged?.Invoke();
                }
            }
        }
    }
}
