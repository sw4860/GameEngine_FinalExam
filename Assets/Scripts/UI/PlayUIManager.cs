using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PlayUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI TimeText;
    public TextMeshProUGUI NextPhaseInfoText;
    public TextMeshProUGUI KillCountText;
    public TextMeshProUGUI SpawnCountText;
    public Image PhaseGauge;

    [Header("EXP UI References")]
    public Image ExpGauge;
    public TextMeshProUGUI LevelText;

    private StageData stageData;
    private PhaseData[] phaseDatas;
    private int currentPhase;
    private int nextPhase;
    private float elapsedTime;
    private float currentPhaseStartTime;

    void Awake()
    {
        EventManager.OnPhaseChanged += UpdatePhaseData;
        EventManager.OnLevelUp += UpdateLevelUI;
    }

    void Start()
    {
        UpdatePhaseData();
        UpdateExpUI(true);
    }


    void Update()
    {
        if (StageManager.Instance == null) return;

        elapsedTime = StageManager.Instance.ElapsedTime;
        
        if (TimeText != null)
            TimeText.text = $"{elapsedTime:N2}";

        UpdateInfoUI();
        UpdateExpUI(false);
    }

    private void UpdateInfoUI()
    {
        if (stageData == null || phaseDatas == null) return;

        bool isLastPhase = currentPhase >= phaseDatas.Length - 1;
        
        float startTime = phaseDatas[currentPhase].RequiredTime;
        float endTime = isLastPhase ? stageData.EndTime : phaseDatas[currentPhase + 1].RequiredTime;
        float remaining = Mathf.Max(0, endTime - elapsedTime);

        if (NextPhaseInfoText != null)
        {
            NextPhaseInfoText.text = isLastPhase 
                ? $"종료까지 {remaining:N2}초" 
                : $"다음 페이즈까지 {remaining:N2}초";
        }

        if (PhaseGauge != null)
        {
            float duration = endTime - startTime;
            PhaseGauge.fillAmount = duration > 0 ? Mathf.Clamp01((elapsedTime - startTime) / duration) : 1f;
        }

        if (GameManager.Instance != null)
        {
            if (KillCountText != null)
                KillCountText.text = $"Kills: {GameManager.Instance.SessionKillCount}";
            
            if (SpawnCountText != null && EnemyManager.Instance != null)
            {
                SpawnCountText.text = $"Active Enemies: {EnemyManager.Instance.activeCount}";
            }
        }
    }

    private void UpdateExpUI(bool immediate)
    {
        if (PlayerStats.Instance == null) return;

        float targetFill = (float)PlayerStats.Instance.CurrentExp / PlayerStats.Instance.RequiredExp;

        if (ExpGauge != null)
        {
            if (immediate)
            {
                ExpGauge.fillAmount = targetFill;
            }
            else
            {
                ExpGauge.DOKill();
                ExpGauge.DOFillAmount(targetFill, 0.2f).SetUpdate(true);
            }
        }

        if (LevelText != null)
        {
            LevelText.text = $"Lv. {PlayerStats.Instance.Level}";
        }
    }

    private void UpdateLevelUI(int currentLevel)
    {
        UpdateExpUI(true);
    }

    private void UpdatePhaseData()
    {
        if (StageManager.Instance == null || StageManager.Instance.StageData == null) return;

        stageData = StageManager.Instance.StageData;
        phaseDatas = stageData.phaseDatas;
        currentPhase = StageManager.Instance.currentPhase;
        nextPhase = StageManager.Instance.NextPhase;
    }

    void OnDestroy()
    {
        EventManager.OnPhaseChanged -= UpdatePhaseData;
        EventManager.OnLevelUp -= UpdateLevelUI;
    }
}
