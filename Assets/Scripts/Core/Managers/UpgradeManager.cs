using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;
    public List<SkillData> AllSkills;
    public UpgradeUIManager UpgradeUI;
    
    [Header("Fallback Rewards")]
    public SkillData HealReward;
    public SkillData GoldReward;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    
        AllSkills = new List<SkillData>(Resources.LoadAll<SkillData>("SO/Skill"));
    }
    
    private void OnEnable()
    {
        EventManager.OnLevelUp += OnLevelUp;
    }
    
    private void OnDisable()
    {
        EventManager.OnLevelUp -= OnLevelUp;
    }
    
    private Queue<int> _levelUpQueue = new Queue<int>();
    private bool _isShowingUI = false;

    private void OnLevelUp(int newLevel)
    {
        _levelUpQueue.Enqueue(newLevel);
        if (!_isShowingUI)
        {
            ProcessNextLevelUp();
        }
    }

    private void ProcessNextLevelUp()
    {
        if (_levelUpQueue.Count == 0)
        {
            Time.timeScale = 1f;
            _isShowingUI = false;
            UpgradeUI.Hide();
            return;
        }

        _isShowingUI = true;
        _levelUpQueue.Dequeue();
        Time.timeScale = 0f;
        List<SkillData> choices = GetRandomChoices(3);
        UpgradeUI.Show(this, choices);
    }
    
    private List<SkillData> GetRandomChoices(int count)
    {
        List<SkillData> available = new List<SkillData>();
    
        foreach (var skill in AllSkills)
        {
            if (skill is FallbackRewardSkillData) continue;
    
            var activeInstance = PlayerSkillManager.Instance.GetActiveSkillInstance(skill);
    
            if (activeInstance != null)
            {
                if (!activeInstance.IsMaxLevel)
                {
                    available.Add(skill);
                }
            }
            else
            {
                if (PlayerSkillManager.Instance.ActiveSkills.Count < PlayerSkillManager.Instance.MaxSkillSlots)
                {
                    available.Add(skill);
                }
            }
        }
    
        List<SkillData> result = new List<SkillData>();
        while (result.Count < count && available.Count > 0)
        {
            int index = Random.Range(0, available.Count);
            result.Add(available[index]);
            available.RemoveAt(index);
        }
    
        if (result.Count < count && HealReward != null && !result.Contains(HealReward))
        {
            result.Add(HealReward);
        }

        if (result.Count < count && GoldReward != null && !result.Contains(GoldReward))
        {
            result.Add(GoldReward);
        }
    
        return result;
    }

    public void SelectUpgrade(SkillData skillSO)
    {
        PlayerSkillManager.Instance.AddOrLevelUpSkill(skillSO);
        ProcessNextLevelUp();
    }
}
