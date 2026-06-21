using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AchievementPresenter : MonoBehaviour
{
    private static AchievementPresenter _instance;
    public static AchievementPresenter Instance => _instance;

    [SerializeField] private GameObject _normalPrefab;
    [SerializeField] private GameObject _challengePrefab;
    [SerializeField] private Transform _uiParent;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _normalSound;
    [SerializeField] private AudioClip _challengeSound;

    private readonly Queue<AchievementData> _unlockQueue = new Queue<AchievementData>();
    private Coroutine _presentationCoroutine;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        EventManager.OnAchievementUnlocked += QueueUnlockPresentation;
#if UNITY_EDITOR
        Debug.Log("[업적 프리젠터] OnEnable - 이벤트 구독 완료");
#endif
    }

    private void OnDisable()
    {
        EventManager.OnAchievementUnlocked -= QueueUnlockPresentation;
#if UNITY_EDITOR
        Debug.Log("[업적 프리젠터] OnDisable - 이벤트 구독 해제");
#endif
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        Debug.Log("[업적 프리젠터] OnDestroy - 프리젠터 오브젝트 파괴됨");
#endif
    }

    private void QueueUnlockPresentation(AchievementData data)
    {
        _unlockQueue.Enqueue(data);
#if UNITY_EDITOR
        Debug.Log($"[업적 연출 대기열] Enqueue 완료 - 현재 대기열 크기: {_unlockQueue.Count}, 대상: {data.Title}");
#endif
        if (_presentationCoroutine == null)
        {
            _presentationCoroutine = StartCoroutine(PresentationRoutine());
        }
    }

    private IEnumerator PresentationRoutine()
    {
        while (_unlockQueue.Count > 0)
        {
            AchievementData data = _unlockQueue.Dequeue();
#if UNITY_EDITOR
            Debug.Log($"[업적 연출 대기열] Dequeue 실행 - 남은 큐 크기: {_unlockQueue.Count}, 대상 업적: {data.Title} (등급: {data.Grade})");
#endif
            GameObject prefab = GetPrefab(data.Grade);

            if (prefab != null)
            {
                Transform parent = _uiParent;
                if (parent == null)
                {
                    Canvas canvas = Object.FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        parent = canvas.transform;
                    }
                }

                GameObject instance = Instantiate(prefab, parent);
                if (instance.TryGetComponent<AchievementUIItem>(out var uiItem))
                {
                    uiItem.Setup(data);
                }
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[업적 연출 경고] {data.Grade} 등급에 해당하는 UI 프리팹이 인스펙터에 등록되어 있지 않습니다!");
#endif
            }

            PlaySound(data.Grade);

            yield return new WaitForSecondsRealtime(4f);
        }
        _presentationCoroutine = null;
    }

    private GameObject GetPrefab(AchievementGrade grade)
    {
        return grade == AchievementGrade.Normal ? _normalPrefab : _challengePrefab;
    }

    private void PlaySound(AchievementGrade grade)
    {
        if (_audioSource == null) return;

        AudioClip clip = grade == AchievementGrade.Normal ? _normalSound : _challengeSound;
        if (clip != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }
}



