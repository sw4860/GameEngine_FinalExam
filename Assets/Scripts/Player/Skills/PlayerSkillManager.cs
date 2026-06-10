using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(0)]
public class PlayerSkillManager : MonoBehaviour
{
    public static PlayerSkillManager Instance;
    public int MaxSkillSlots = 6;

    private List<SkillData> _activeSkills = new List<SkillData>();
    public List<SkillData> ActiveSkills => _activeSkills;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void LateUpdate()
    {
        for (int i = 0; i < _activeSkills.Count; i++)
        {
            _activeSkills[i].OnUpdate(gameObject);
        }
    }

    public void AddOrLevelUpSkill(SkillData skillSO)
    {
        var existing = GetActiveSkillInstance(skillSO);
        if (existing != null)
        {
            existing.OnLevelUp(gameObject);
        }
        else
        {
            AddSkill(skillSO);
        }
    }

    public void AddSkill(SkillData skillSO)
    {
        if (skillSO == null) return;

        SkillData skillInstance = Instantiate(skillSO);
        skillInstance.OnEquip(gameObject);
        _activeSkills.Add(skillInstance);
    }

    public SkillData GetActiveSkillInstance(SkillData skillSO)
    {
        if (skillSO == null) return null;
        
        string targetName = string.IsNullOrEmpty(skillSO.SkillName) ? skillSO.name : skillSO.SkillName;
        return _activeSkills.Find(s => {
            string currentName = string.IsNullOrEmpty(s.SkillName) ? s.name : s.SkillName;
            return currentName == targetName;
        });
    }
}
