using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private TMP_InputField _masterInputField;
    [SerializeField] private TMP_InputField _bgmInputField;
    [SerializeField] private TMP_InputField _sfxInputField;
    [SerializeField] private Button _closeButton;

    private AudioSettingsUIManager _uiManager;

    private void Awake()
    {
        _uiManager = GetComponent<AudioSettingsUIManager>();
    }

    private void OnEnable()
    {
        InitializeSliders();
        BindEvents();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    private void InitializeSliders()
    {
        float master = PlayerPrefs.GetFloat(AudioManager.MasterVolKey, 0.75f);
        float bgm = PlayerPrefs.GetFloat(AudioManager.BGMVolKey, 0.75f);
        float sfx = PlayerPrefs.GetFloat(AudioManager.SFXVolKey, 0.75f);

        if (_masterSlider != null)
        {
            _masterSlider.value = master;
            UpdateInputField(_masterInputField, master);
        }
        if (_bgmSlider != null)
        {
            _bgmSlider.value = bgm;
            UpdateInputField(_bgmInputField, bgm);
        }
        if (_sfxSlider != null)
        {
            _sfxSlider.value = sfx;
            UpdateInputField(_sfxInputField, sfx);
        }
    }

    private void BindEvents()
    {
        if (_masterSlider != null)
        {
            _masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        }

        if (_bgmSlider != null)
        {
            _bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }

        if (_masterInputField != null)
        {
            _masterInputField.onEndEdit.AddListener(OnMasterInputFieldEndEdit);
        }

        if (_bgmInputField != null)
        {
            _bgmInputField.onEndEdit.AddListener(OnBgmInputFieldEndEdit);
        }

        if (_sfxInputField != null)
        {
            _sfxInputField.onEndEdit.AddListener(OnSfxInputFieldEndEdit);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void UnbindEvents()
    {
        if (_masterSlider != null)
        {
            _masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
        }

        if (_bgmSlider != null)
        {
            _bgmSlider.onValueChanged.RemoveListener(OnBgmSliderChanged);
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
        }

        if (_masterInputField != null)
        {
            _masterInputField.onEndEdit.RemoveListener(OnMasterInputFieldEndEdit);
        }

        if (_bgmInputField != null)
        {
            _bgmInputField.onEndEdit.RemoveListener(OnBgmInputFieldEndEdit);
        }

        if (_sfxInputField != null)
        {
            _sfxInputField.onEndEdit.RemoveListener(OnSfxInputFieldEndEdit);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(ClosePanel);
        }
    }

    private void UpdateInputField(TMP_InputField inputField, float value)
    {
        if (inputField != null)
        {
            inputField.text = Mathf.RoundToInt(value * 100f).ToString();
        }
    }

    private void OnInputFieldEndEdit(string text, Slider slider, string volumeParameter, TMP_InputField inputField)
    {
        if (float.TryParse(text, out float percentage))
        {
            float linearValue = Mathf.Clamp(percentage / 100f, 0.0001f, 1f);
            if (slider != null)
            {
                slider.value = linearValue;
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetVolume(volumeParameter, linearValue);
            }
            UpdateInputField(inputField, linearValue);
        }
        else
        {
            if (slider != null)
            {
                UpdateInputField(inputField, slider.value);
            }
        }
    }

    private void OnMasterSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolume(AudioManager.MasterVolKey, value);
        }
        UpdateInputField(_masterInputField, value);
    }

    private void OnBgmSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolume(AudioManager.BGMVolKey, value);
        }
        UpdateInputField(_bgmInputField, value);
    }

    private void OnSfxSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolume(AudioManager.SFXVolKey, value);
        }
        UpdateInputField(_sfxInputField, value);
    }

    private void OnMasterInputFieldEndEdit(string text)
    {
        OnInputFieldEndEdit(text, _masterSlider, AudioManager.MasterVolKey, _masterInputField);
    }

    private void OnBgmInputFieldEndEdit(string text)
    {
        OnInputFieldEndEdit(text, _bgmSlider, AudioManager.BGMVolKey, _bgmInputField);
    }

    private void OnSfxInputFieldEndEdit(string text)
    {
        OnInputFieldEndEdit(text, _sfxSlider, AudioManager.SFXVolKey, _sfxInputField);
    }

    private void ClosePanel()
    {
        if (_uiManager != null)
        {
            _uiManager.Hide();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
