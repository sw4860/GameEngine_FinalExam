using UnityEngine;

public class CursedContractManager : MonoBehaviour
{
    private static CursedContractManager _instance;
    public static CursedContractManager Instance => _instance;

    [SerializeField] private GameObject _cursedChestPrefab;
    [SerializeField] private AudioClip _cursePurgedSound;
    [SerializeField] private AudioClip _contractAcceptSound;

    private CursedContractData _activeContract;
    private float _remainingTime;
    private int _currentKills;
    private int _targetKills;
    private float _goldMultiplier = 1f;

    public CursedContractData ActiveContract => _activeContract;
    public float GoldMultiplier => _goldMultiplier;
    public float RemainingTime => _remainingTime;
    public int CurrentKills => _currentKills;
    public int TargetKills => _targetKills;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        EventManager.OnEnemyDeath += HandleEnemyDeath;
    }

    private void OnDisable()
    {
        EventManager.OnEnemyDeath -= HandleEnemyDeath;
    }

    private void Update()
    {
        if (_activeContract == null) return;

        if (_activeContract.RiskHpDrainPerSecond > 0f && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.TakeDamage(_activeContract.RiskHpDrainPerSecond * Time.deltaTime);
        }

        if (_activeContract.QuestType == ContractQuestType.Timer)
        {
            _remainingTime -= Time.deltaTime;
            if (_remainingTime <= 0f)
            {
                PurgeCurse();
            }
        }
    }

    public void SpawnCursedChest(Vector3 position)
    {
        if (_cursedChestPrefab != null)
        {
            Instantiate(_cursedChestPrefab, position, Quaternion.identity);
        }
    }

    public void ActivateContract(CursedContractData contract)
    {
        if (contract == null || PlayerStats.Instance == null || PlayerStats.Instance.StatData == null) return;

        ForceRemoveCurse();

        _activeContract = contract;
        _goldMultiplier = contract.ReturnGoldMultiplier;

        var stat = PlayerStats.Instance.StatData;
        stat.DamageMultiplier *= (contract.ReturnDamageMultiplier * contract.RiskDamageMultiplier);
        stat.MoveSpeedMultiplier *= (contract.ReturnMoveSpeedMultiplier * contract.RiskMoveSpeedMultiplier);
        stat.CooldownReduction += contract.ReturnCooldownReductionBonus;

        if (contract.QuestType == ContractQuestType.Timer)
        {
            _remainingTime = contract.TargetDuration;
        }
        else if (contract.QuestType == ContractQuestType.KillCount)
        {
            _currentKills = 0;
            _targetKills = contract.TargetKillCount;
        }

        if (_contractAcceptSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(_contractAcceptSound);
        }
    }

    public void PurgeCurse()
    {
        if (_activeContract == null || PlayerStats.Instance == null || PlayerStats.Instance.StatData == null) return;

        var stat = PlayerStats.Instance.StatData;
        if (_activeContract.RiskDamageMultiplier != 0f)
        {
            stat.DamageMultiplier /= _activeContract.RiskDamageMultiplier;
        }
        if (_activeContract.RiskMoveSpeedMultiplier != 0f)
        {
            stat.MoveSpeedMultiplier /= _activeContract.RiskMoveSpeedMultiplier;
        }

        if (_cursePurgedSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(_cursePurgedSound);
        }

        _activeContract = null;
        _goldMultiplier = 1f;
    }

    private void HandleEnemyDeath()
    {
        if (_activeContract == null) return;

        if (_activeContract.QuestType == ContractQuestType.KillCount)
        {
            _currentKills++;
            if (_currentKills >= _targetKills)
            {
                PurgeCurse();
            }
        }
    }

    public void ForceRemoveCurse()
    {
        if (_activeContract == null || PlayerStats.Instance == null || PlayerStats.Instance.StatData == null) return;

        var stat = PlayerStats.Instance.StatData;
        
        float finalDmgMult = _activeContract.ReturnDamageMultiplier * _activeContract.RiskDamageMultiplier;
        if (finalDmgMult != 0f)
        {
            stat.DamageMultiplier /= finalDmgMult;
        }

        float finalSpeedMult = _activeContract.ReturnMoveSpeedMultiplier * _activeContract.RiskMoveSpeedMultiplier;
        if (finalSpeedMult != 0f)
        {
            stat.MoveSpeedMultiplier /= finalSpeedMult;
        }

        stat.CooldownReduction -= _activeContract.ReturnCooldownReductionBonus;

        _activeContract = null;
        _goldMultiplier = 1f;
    }

    private void OnDestroy()
    {
        ForceRemoveCurse();
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
