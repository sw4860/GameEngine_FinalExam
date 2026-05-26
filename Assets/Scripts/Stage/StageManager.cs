using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;
    public StageData StageData;
    public float ElapsedTime;
    public int NextPhase = 0;
    public int currentPhase = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void Start()
    {
        ElapsedTime = 0.0f;
        NextPhase = 0;
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
                NextPhase = currentPhase + 1;
                EventManager.OnPhaseChanged?.Invoke();
            }
        }
    }
}