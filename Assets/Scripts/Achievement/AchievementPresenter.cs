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
    }

    private void OnDisable()
    {
        EventManager.OnAchievementUnlocked -= QueueUnlockPresentation;
    }

    private void OnDestroy()
    {
    }

    public void PlayPresentation(AchievementData data)
    {
        if (data == null) return;
        QueueUnlockPresentation(data);
    }

    private void QueueUnlockPresentation(AchievementData data)
    {
        _unlockQueue.Enqueue(data);
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



