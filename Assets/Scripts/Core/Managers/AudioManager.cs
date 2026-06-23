using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public const string MasterVolKey = "Master";
    public const string BGMVolKey = "BGM";
    public const string SFXVolKey = "SFX";

    private static AudioManager _instance;
    public static AudioManager Instance => _instance;

    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private GameObject _sfxSourcePrefab;
    [SerializeField] private int _initialPoolSize = 10;

    private readonly Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
    private readonly List<AudioSource> _activeSfxSources = new List<AudioSource>();

    private void Awake()
    {
        _instance = this;
        InitializePool();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private System.Collections.IEnumerator Start()
    {
        yield return null;
        LoadAndApplyVolumes();
    }

    private void InitializePool()
    {
        for (int i = 0; i < _initialPoolSize; i++)
        {
            AudioSource source = CreateNewSfxSource();
            _sfxPool.Enqueue(source);
        }
    }

    private AudioSource CreateNewSfxSource()
    {
        GameObject obj;
        if (_sfxSourcePrefab != null)
        {
            obj = Instantiate(_sfxSourcePrefab, transform);
            obj.name = "SFXSource";
        }
        else
        {
            obj = new GameObject("SFXSource", typeof(AudioSource));
            obj.transform.SetParent(transform);
        }

        AudioSource source = obj.GetComponent<AudioSource>();
        if (source == null)
        {
            source = obj.AddComponent<AudioSource>();
        }
        source.playOnAwake = false;

        if (_audioMixer != null)
        {
            AudioMixerGroup[] groups = _audioMixer.FindMatchingGroups("SFX");
            if (groups.Length > 0)
            {
                source.outputAudioMixerGroup = groups[0];
            }
        }

        return source;
    }

    public void LoadAndApplyVolumes()
    {
        float master = PlayerPrefs.GetFloat(MasterVolKey, 0.75f);
        float bgm = PlayerPrefs.GetFloat(BGMVolKey, 0.75f);
        float sfx = PlayerPrefs.GetFloat(SFXVolKey, 0.75f);

        SetVolume(MasterVolKey, master, false);
        SetVolume(BGMVolKey, bgm, false);
        SetVolume(SFXVolKey, sfx, false);
    }

    public void SetVolume(string parameterName, float linearValue)
    {
        SetVolume(parameterName, linearValue, true);
    }

    public void SetVolume(string parameterName, float value, bool saveImmediately)
    {
        if (saveImmediately)
        {
            PlayerPrefs.SetFloat(parameterName, value);
            PlayerPrefs.Save();
        }

        if (_audioMixer == null) return;

        float clampedValue = Mathf.Clamp(value, 0.0001f, 1f);
        float decibel = Mathf.Log10(clampedValue) * 20f;
        _audioMixer.SetFloat(parameterName, decibel);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (_bgmSource == null || clip == null) return;

        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    public void StopBGM()
    {
        if (_bgmSource != null)
        {
            _bgmSource.Stop();
        }
    }

    public void PlaySFX(AudioClip clip, Vector3 position = default)
    {
        if (clip == null) return;

        AudioSource source = GetPooledSfxSource();
        if (source == null) return;

        source.gameObject.transform.position = position;
        source.spatialBlend = position == default ? 0f : 1f;
        source.clip = clip;
        source.Play();

        _activeSfxSources.Add(source);
        StartCoroutine(ReturnToPoolAfterFinished(source));
    }

    private AudioSource GetPooledSfxSource()
    {
        if (_sfxPool.Count > 0)
        {
            AudioSource source = _sfxPool.Dequeue();
            source.gameObject.SetActive(true);
            return source;
        }

        AudioSource newSource = CreateNewSfxSource();
        return newSource;
    }

    private System.Collections.IEnumerator ReturnToPoolAfterFinished(AudioSource source)
    {
        yield return new WaitForSeconds(source.clip.length);
        
        if (source != null)
        {
            source.Stop();
            source.gameObject.SetActive(false);
            _activeSfxSources.Remove(source);
            _sfxPool.Enqueue(source);
        }
    }
}
