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

        if (NextPhase + 1 < StageData.phaseDatas.Length && ElapsedTime >= StageData.phaseDatas[NextPhase].RequiredTime)
        {
            NextPhase++;
            currentPhase = NextPhase - 1;
            EventManager.OnPhaseChanged?.Invoke();
        }
    }
}