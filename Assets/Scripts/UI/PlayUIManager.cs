using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI TimeText;
    public TextMeshProUGUI NextPhaseInfoText;
    public TextMeshProUGUI KillCountText;
    public TextMeshProUGUI SpawnCountText;
    public Image PhaseGauge;

    private StageData stageData;
    private PhaseData[] phaseDatas;
    private int currentPhase;
    private int nextPhase;
    private float elapsedTime;
    private float currentPhaseStartTime;

    void Awake()
    {
        EventManager.OnPhaseChanged += UpdatePhaseData;
    }

    void Start()
    {
        UpdatePhaseData();
    }

    void Update()
    {
        if (StageManager.Instance == null) return;

        elapsedTime = StageManager.Instance.ElapsedTime;
        
        if (TimeText != null)
            TimeText.text = $"{elapsedTime:N2}";

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (stageData == null || phaseDatas == null) return;

        // 현재 페이즈가 마지막인지 확인
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
                SpawnCountText.text = $"Active Enemies: {EnemyManager.Instance.activeEnemies.Count}";
        }
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
    }
}
