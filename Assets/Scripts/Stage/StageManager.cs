using UnityEngine;

public class StageMangaer : MonoBehaviour
{
    public StageData StageData;
    private int currentPhase = 0;
    private float elapsedTime;

    void Start()
    {
        elapsedTime = 0.0f;
    }

    void Update()
    {        
        elapsedTime += Time.deltaTime;

        if (currentPhase + 1 < StageData.phaseDatas.Length && elapsedTime >= StageData.phaseDatas[currentPhase].RequiredTime)
        {
            currentPhase++;
        }
    }
}