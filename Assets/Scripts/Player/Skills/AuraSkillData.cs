using UnityEngine;

[CreateAssetMenu(fileName = "NewAuraSkillData", menuName = "Skills/AuraSkill")]
public class AuraSkillData : SkillData
{
    public float Damage;
    public float Radius;
    public float TickInterval;
    public Sprite AuraSprite;
    public Color AuraColor = new Color(1, 1, 1, 0.3f);

    public override void OnEquip(GameObject player)
    {
        AuraSkill aura = player.GetComponent<AuraSkill>();
        if (aura == null)
        {
            aura = player.AddComponent<AuraSkill>();
        }
        aura.Init(this);
    }
}
