using UnityEngine;

public enum RewardType
{
    Heal,
    Gold
}

[CreateAssetMenu(fileName = "FallbackRewardSkillData", menuName = "Skills/FallbackRewardSkillData")]
public class FallbackRewardSkillData : SkillData
{
    public RewardType RewardType;
    public float Amount = 20f;

    public override int MaxLevel => 999;

    public override void OnEquip(GameObject owner)
    {
        ApplyReward();
    }

    public override void OnLevelUp(GameObject owner)
    {
        ApplyReward();
    }

    private void ApplyReward()
    {
        switch (RewardType)
        {
            case RewardType.Heal:
                PlayerStats.Instance.Heal(Amount);
                break;
            case RewardType.Gold:
                GameDataManager.Instance.AddGold((int)Amount);
                break;
        }
    }

    public override string GetLevelUpDescription()
    {
        return RewardType switch
        {
            RewardType.Heal => $"체력을 {Amount} 회복합니다.",
            RewardType.Gold => $"골드를 {Amount} 획득합니다.",
            _ => ""
        };
    }
}
