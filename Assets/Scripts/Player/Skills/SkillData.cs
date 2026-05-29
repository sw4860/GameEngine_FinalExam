using UnityEngine;

public abstract class SkillData : ScriptableObject
{
    public string SkillName;
    [TextArea] public string Description;
    public Sprite Icon;
    
    public abstract void OnEquip(GameObject player);
}
