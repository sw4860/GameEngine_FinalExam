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
            CurrentHp = MaxHp;
            ApplyCharacterVisuals();
        }
        
        if (StatData == null)
        {
            StatData = ScriptableObject.CreateInstance<PlayerStatData>();
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
            Debug.Log($"레벨업! 현재 레벨: {Level}");
        }
    }
}