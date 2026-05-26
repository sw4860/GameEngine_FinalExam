using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI TimeText;
    public TextMeshProUGUI NextPhaseInfoText;
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

        bool isToEnding = nextPhase >= phaseDatas.Length || elapsedTime >= phaseDatas[nextPhase].RequiredTime;

        float startTime = isToEnding ? (phaseDatas.Length > 0 ? phaseDatas[phaseDatas.Length - 1].RequiredTime : 0) : currentPhaseStartTime;
        float endTime   = isToEnding ? stageData.EndTime : phaseDatas[nextPhase].RequiredTime;
        float remaining = Mathf.Max(0, endTime - elapsedTime);

        if (NextPhaseInfoText != null)
        {
            NextPhaseInfoText.text = isToEnding 
                ? $"종료까지 {remaining:N2}초" 
                : $"{nextPhase + 1}페이즈까지 {remaining:N2}초";
        }

        if (PhaseGauge != null)
        {
            float duration = endTime - startTime;
            PhaseGauge.fillAmount = duration > 0 ? Mathf.Clamp01((elapsedTime - startTime) / duration) : 1f;
        }
    }

    private void UpdatePhaseData()
    {
        if (StageManager.Instance == null || StageManager.Instance.StageData == null) return;

        stageData = StageManager.Instance.StageData;
        phaseDatas = stageData.phaseDatas;
        currentPhase = StageManager.Instance.currentPhase;
        nextPhase = StageManager.Instance.NextPhase;
    
        currentPhaseStartTime = phaseDatas[currentPhase].RequiredTime;
    }

    void OnDestroy()
    {
        EventManager.OnPhaseChanged -= UpdatePhaseData;
    }
}
