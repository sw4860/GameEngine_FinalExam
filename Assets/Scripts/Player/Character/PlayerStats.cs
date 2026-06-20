using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    [Header("Data")]
    public CharacterData CharacterData;
    public PlayerStatData StatData;

    [Header("Leveling")]
    public int Level = 1;
    public int CurrentExp = 0;

    public int RequiredExp => (int)Math.Pow(Level, 2) * 10;
    
    [Header("Health")]
    public float MaxHp = 100f;
    public float CurrentHp;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeStats();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Heal(float amount)
    {
        CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
        EventManager.OnPlayerHpChanged?.Invoke();
    }

    public void TakeDamage(float damage)
    {
        CurrentHp -= damage;
        EventManager.OnPlayerHpChanged?.Invoke();
        if (CurrentHp <= 0)
        {
            EventManager.OnPlayerDeath?.Invoke();
        }
    }

    private void InitializeStats()
    {
        if (CharacterManager.Instance != null && CharacterManager.Instance.SelectedCharacter != null)
        {
            CharacterData = CharacterManager.Instance.SelectedCharacter;
        }

        if (CharacterData != null)
        {
            if (CharacterData.BaseStats != null)
            {
                StatData = CharacterData.BaseStats.Clone();
            }

            if (CharacterData.BaseSkill != null)
            {
                PlayerSkillManager.Instance.AddSkill(CharacterData.BaseSkill);
            }

            PlayerSkillManager.Instance.MaxSkillSlots = CharacterData.MaxSkillSlots;
            MaxHp = CharacterData.BaseStats.BaseMaxHp;

            // --- Apply Lobby Permanent Upgrades ---
            if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentData != null)
            {
                LobbyUpgradeData[] lobbyUpgrades = Resources.LoadAll<LobbyUpgradeData>("SO/LobbyUpgrades");
                foreach (var upgrade in lobbyUpgrades)
                {
                    int currentLevel = upgrade.GetCurrentLevel(GameDataManager.Instance.CurrentData);
                    float bonusValue = upgrade.GetBonusForLevel(currentLevel);
                    ApplyLobbyUpgradeBonus(upgrade.TargetStat, bonusValue);
                }
            }

            CurrentHp = MaxHp;

            ApplyCharacterVisuals();
        }
        
        if (StatData == null)
        {
            StatData = ScriptableObject.CreateInstance<PlayerStatData>();
        }
    }

    private void ApplyLobbyUpgradeBonus(StatType type, float bonusValue)
    {
        if (StatData == null) return;

        switch (type)
        {
            case StatType.MaxHp:
                MaxHp += bonusValue;
                break;
            case StatType.Damage:
                StatData.AdditionalDamage += bonusValue;
                break;
            case StatType.MoveSpeed:
                StatData.AdditionalMoveSpeed += bonusValue;
                break;
            case StatType.CooldownReduction:
                StatData.CooldownReduction += bonusValue;
                break;
            case StatType.MagnetRadius:
                // 1f 기준의 절대 배율 값(예: 1.1f)을 상대 증가치(+0.1f)로 환산하여 누적 합산
                StatData.MagnetRadiusBonus += (bonusValue - 1f);
                break;
            case StatType.ExpMultiplier:
                // 1f 기준의 절대 배율 값(예: 1.1f)을 상대 증가치(+0.1f)로 환산하여 누적 합산
                StatData.ExpMultiplier += (bonusValue - 1f);
                break;
        }
    }

    private void ApplyCharacterVisuals()
    {
        if (CharacterData == null) return;

        Animator animator = GetComponent<Animator>();
        if (animator != null && CharacterData.AnimatorController != null)
        {
            animator.runtimeAnimatorController = CharacterData.AnimatorController;
        }
    }


    public void AddExp(int amount)
    {
        CurrentExp += (int)(amount * StatData.CurrentExpMultiplier);
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        while (CurrentExp >= RequiredExp)
        {
            CurrentExp -= RequiredExp;
            Level++;
            EventManager.OnLevelUp?.Invoke(Level);
        }
    }
}