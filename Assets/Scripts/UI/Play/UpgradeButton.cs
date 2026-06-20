using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UpgradeButton : MonoBehaviour
{
    public Image Icon;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI DescriptionText;
    public TextMeshProUGUI LevelText;
    public Button Button;

    public void SetSkill(SkillData skill, Action onClick)
    {
        Icon.sprite = skill.Icon;
        NameText.text = string.IsNullOrEmpty(skill.SkillName) ? skill.name : skill.SkillName;
        
        var existingSkill = PlayerSkillManager.Instance.GetActiveSkillInstance(skill);
        if (existingSkill != null)
        {
            LevelText.text = $"Lv. {existingSkill.CurrentLevel}";
            DescriptionText.text = $"<color=yellow>강화:</color>\n{existingSkill.GetLevelUpDescription()}";
        }
        else
        {
            LevelText.text = "New";
            DescriptionText.text = $"<color=green>신규:</color>\n{skill.GetLevelUpDescription()}";
        }

        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() => onClick?.Invoke());
    }
}
