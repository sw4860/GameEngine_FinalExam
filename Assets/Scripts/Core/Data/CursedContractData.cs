using UnityEngine;

public enum ContractQuestType
{
    Timer,
    KillCount
}

[CreateAssetMenu(fileName = "CursedContract", menuName = "Cursed/Contract")]
public class CursedContractData : ScriptableObject
{
    public string Title;
    [TextArea] public string ReturnDescription;
    [TextArea] public string RiskDescription;

    public float ReturnDamageMultiplier = 1f;
    public float ReturnMoveSpeedMultiplier = 1f;
    public float ReturnCooldownReductionBonus = 0f;
    public float ReturnGoldMultiplier = 1f;

    public float RiskDamageMultiplier = 1f;
    public float RiskMoveSpeedMultiplier = 1f;
    public float RiskHpDrainPerSecond = 0f;
    public float EnemySpeedMultiplier = 1f;

    public ContractQuestType QuestType;
    public float TargetDuration = 0f;
    public int TargetKillCount = 0;
}
