using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private TMP_InputField _masterInputField;
    [SerializeField] private TMP_InputField _bgmInputField;
    [SerializeField] private TMP_InputField _sfxInputField;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Toggle _damageTextToggle;
    [SerializeField] private Image _toggleTargetImage;
    [SerializeField] private TextMeshProUGUI _toggleLabel;
    [SerializeField] private Sprite _toggleOnSprite;
    [SerializeField] private Sprite _toggleOffSprite;
    [SerializeField] private Button _resetDataButton;

    private SettingsUIManager _uiManager;

    private void Awake()
    {
        _uiManager = GetComponent<SettingsUIManager>();
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

        if (_damageTextToggle != null && GameDataManager.Instance != null && GameDataManager.Instance.CurrentData != null)
        {
            _damageTextToggle.isOn = GameDataManager.Instance.CurrentData.IsDamageTextEnabled;
            UpdateToggleVisual(_damageTextToggle.isOn);
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

        if (_damageTextToggle != null)
        {
            _damageTextToggle.onValueChanged.AddListener(OnDamageTextToggleChanged);
        }

        if (_resetDataButton != null)
        {
            _resetDataButton.onClick.AddListener(ResetData);
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

        if (_damageTextToggle != null)
        {
            _damageTextToggle.onValueChanged.RemoveListener(OnDamageTextToggleChanged);
        }

        if (_resetDataButton != null)
        {
            _resetDataButton.onClick.RemoveListener(ResetData);
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

    private void OnDamageTextToggleChanged(bool value)
    {
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentData != null)
        {
            GameDataManager.Instance.CurrentData.IsDamageTextEnabled = value;
            GameDataManager.Instance.SaveGame();
        }
        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.ApplySettings(value);
        }
        UpdateToggleVisual(value);
    }

    private void UpdateToggleVisual(bool value)
    {
        if (_toggleTargetImage != null)
        {
            _toggleTargetImage.sprite = value ? _toggleOnSprite : _toggleOffSprite;
        }

        if (_toggleLabel != null)
        {
            _toggleLabel.text = value ? "ON" : "OFF";
        }
    }

    private void ResetData()
    {
        SaveManager.DeleteSave();
        PlayerPrefs.DeleteAll();

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.LoadGame();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.LoadAndApplyVolumes();
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene("MainScene");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
        }
    }
}
