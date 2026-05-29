using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillManager : MonoBehaviour
{
    public List<SkillData> StartingSkills;
    private List<SkillData> _activeSkills = new List<SkillData>();

    void Start()
    {
        if (StartingSkills != null)
        {
            foreach (var skill in StartingSkills)
            {
                AddSkill(skill);
            }
        }
    }

    public void AddSkill(SkillData skillData)
    {
        if (skillData == null) return;
        
        skillData.OnEquip(gameObject);
        _activeSkills.Add(skillData);
    }
}
