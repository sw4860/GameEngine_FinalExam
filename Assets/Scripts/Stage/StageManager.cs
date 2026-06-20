using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;
    public StageData StageData;
    public float ElapsedTime;
    public int currentPhase = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            // 로비에서 선택한 스테이지 정보를 동적으로 로드
            if (GameDataManager.Instance != null && !string.IsNullOrEmpty(GameDataManager.Instance.CurrentData.SelectedStageName))
            {
                StageData selectedStage = Resources.Load<StageData>("SO/StageDatas/" + GameDataManager.Instance.CurrentData.SelectedStageName);
                if (selectedStage != null)
                {
                    StageData = selectedStage;
                }
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ElapsedTime = 0.0f;
    }

    void Update()
    {        
        ElapsedTime += Time.deltaTime;

        // 다음 페이즈가 존재하고, 현재 시간이 다음 페이즈의 시작 시간에 도달했을 때
        if (currentPhase + 1 < StageData.phaseDatas.Length)
        {
            if (ElapsedTime >= StageData.phaseDatas[currentPhase + 1].RequiredTime)
            {
                currentPhase++;
                EventManager.OnPhaseChanged?.Invoke();
            }
        }
        else if (ElapsedTime >= StageData.EndTime)
        {
            ElapsedTime = StageData.EndTime;
            EventManager.OnGameClear?.Invoke();
        }
    }
}