using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PlayUIManager : MonoBehaviour
{
    [Header("Info UI")]
    public TextMeshProUGUI TimeText;
    public TextMeshProUGUI NextPhaseInfoText;
    public TextMeshProUGUI KillCountText;
    public TextMeshProUGUI SpawnCountText;
    public Image PhaseGauge;
    public Image HpGauge;

    [Header("EXP UI")]
    public Image ExpGauge;
    public TextMeshProUGUI LevelText;

    [Header("Clear UI")]
    public GameObject ClearPanel;

    [Header("GameOver UI")]
    public GameObject GameOverPanel;

    [Header("Clear UI Reference")]
    [SerializeField] private TextMeshProUGUI _clearKillText;
    [SerializeField] private TextMeshProUGUI _clearTimeText;
    [SerializeField] private Button _clearLobbyButton;
    [SerializeField] private Button _clearRestartButton;

    [Header("GameOver UI Reference")]
    [SerializeField] private TextMeshProUGUI _gameOverKillText;
    [SerializeField] private TextMeshProUGUI _gameOverTimeText;
    [SerializeField] private Button _gameOverLobbyButton;
    [SerializeField] private Button _gameOverRestartButton;

    [Header("Pause UI")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private Button _pauseLobbyButton;
    [SerializeField] private Button _pauseResumeButton;
    [SerializeField] private Button _pauseRestartButton;

    private StageData stageData;
    private PhaseData[] phaseDatas;
    private int currentPhase;
    private float elapsedTime;
    private bool _isPaused;

    void Awake()
    {
        EventManager.OnPhaseChanged += UpdatePhaseData;
        EventManager.OnLevelUp += UpdateLevelUI;
        EventManager.OnPlayerHpChanged += UpdatePlayerHpUI;
        EventManager.OnGameClear += OnGameClear;
        EventManager.OnPlayerDeath += OnGameOver;
        Time.timeScale = 1f;

        BindButtonEvents();
    }

    void Start()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SessionKillCount = 0;
            GameDataManager.Instance.AddPlayCount();
        }

        if (_pausePanel != null)
        {
            _pausePanel.SetActive(false);
        }

        UpdatePhaseData();
        UpdateExpUI(true);
        UpdatePlayerHpUI();
    }

    void Update()
    {
        HandleKeyboardInput();

        if (StageManager.Instance == null) return;

        elapsedTime = StageManager.Instance.ElapsedTime;
        
        if (TimeText != null)
            TimeText.text = $"{elapsedTime:N2}";

        UpdateInfoUI();
        UpdateExpUI(false);
    }

    private void HandleKeyboardInput()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void BindButtonEvents()
    {
        if (_clearLobbyButton != null)
        {
            _clearLobbyButton.onClick.AddListener(ReturnToLobby);
        }
        if (_clearRestartButton != null)
        {
            _clearRestartButton.onClick.AddListener(RestartGame);
        }
        if (_gameOverLobbyButton != null)
        {
            _gameOverLobbyButton.onClick.AddListener(ReturnToLobby);
        }
        if (_gameOverRestartButton != null)
        {
            _gameOverRestartButton.onClick.AddListener(RestartGame);
        }
        if (_pauseLobbyButton != null)
        {
            _pauseLobbyButton.onClick.AddListener(ReturnToLobby);
        }
        if (_pauseResumeButton != null)
        {
            _pauseResumeButton.onClick.AddListener(TogglePause);
        }
        if (_pauseRestartButton != null)
        {
            _pauseRestartButton.onClick.AddListener(RestartGame);
        }
    }

    private void UnbindButtonEvents()
    {
        if (_clearLobbyButton != null)
        {
            _clearLobbyButton.onClick.RemoveListener(ReturnToLobby);
        }
        if (_clearRestartButton != null)
        {
            _clearRestartButton.onClick.RemoveListener(RestartGame);
        }
        if (_gameOverLobbyButton != null)
        {
            _gameOverLobbyButton.onClick.RemoveListener(ReturnToLobby);
        }
        if (_gameOverRestartButton != null)
        {
            _gameOverRestartButton.onClick.RemoveListener(RestartGame);
        }
        if (_pauseLobbyButton != null)
        {
            _pauseLobbyButton.onClick.RemoveListener(ReturnToLobby);
        }
        if (_pauseResumeButton != null)
        {
            _pauseResumeButton.onClick.RemoveListener(TogglePause);
        }
        if (_pauseRestartButton != null)
        {
            _pauseRestartButton.onClick.RemoveListener(RestartGame);
        }
    }

    private void TogglePause()
    {
        if (ClearPanel.activeSelf || GameOverPanel.activeSelf) return;

        _isPaused = !_isPaused;
        if (_pausePanel != null)
        {
            _pausePanel.SetActive(_isPaused);
        }
        Time.timeScale = _isPaused ? 0f : 1f;
    }

    private void ReturnToLobby()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SaveGame();
        }
        Time.timeScale = 1f;
        SceneTransitionManager.Instance.LoadScene("MainScene");
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneTransitionManager.Instance.LoadScene("GameScene");
    }

    private void UpdateInfoUI()
    {
        if (stageData == null || phaseDatas == null) return;

        bool isLastPhase = currentPhase >= phaseDatas.Length - 1;
        
        float startTime = phaseDatas[currentPhase].RequiredTime;
        float endTime = isLastPhase ? stageData.EndTime : phaseDatas[currentPhase + 1].RequiredTime;
        float remaining = Mathf.Max(0, endTime - elapsedTime);

        if (NextPhaseInfoText != null)
        {
            NextPhaseInfoText.text = isLastPhase 
                ? $"종료까지 {remaining:N2}초" 
                : $"다음 페이즈까지 {remaining:N2}초";
        }

        if (PhaseGauge != null)
        {
            float duration = endTime - startTime;
            PhaseGauge.fillAmount = duration > 0 ? Mathf.Clamp01((elapsedTime - startTime) / duration) : 1f;
        }

        if (GameDataManager.Instance != null)
        {
            if (KillCountText != null)
                KillCountText.text = $"Kills: {GameDataManager.Instance.SessionKillCount}";
            
            if (SpawnCountText != null && EnemyManager.Instance != null)
            {
                SpawnCountText.text = $"Active Enemies: {EnemyManager.Instance.activeCount}";
            }
        }
    }

    private void UpdateExpUI(bool immediate)
    {
        if (PlayerStats.Instance == null) return;

        float targetFill = (float)PlayerStats.Instance.CurrentExp / PlayerStats.Instance.RequiredExp;

        if (ExpGauge != null)
        {
            if (immediate)
            {
                ExpGauge.fillAmount = targetFill;
            }
            else
            {
                ExpGauge.DOKill();
                ExpGauge.DOFillAmount(targetFill, 0.2f).SetUpdate(true);
            }
        }

        if (LevelText != null)
        {
            LevelText.text = $"Lv. {PlayerStats.Instance.Level}";
        }
    }

    private void UpdateLevelUI(int currentLevel)
    {
        UpdateExpUI(true);
    }

    private void UpdatePhaseData()
    {
        if (StageManager.Instance == null || StageManager.Instance.StageData == null) return;

        stageData = StageManager.Instance.StageData;
        phaseDatas = stageData.phaseDatas;
        currentPhase = StageManager.Instance.currentPhase;
    }

    private void UpdatePlayerHpUI()
    {
        HpGauge.fillAmount = PlayerStats.Instance.CurrentHp / PlayerStats.Instance.MaxHp;
    }

    private void OnGameClear()
    {
        Time.timeScale = 0f;
        ClearPanel.SetActive(true);

        if (GameDataManager.Instance != null)
        {
            if (_clearKillText != null)
            {
                _clearKillText.text = $"Total Kills: {GameDataManager.Instance.SessionKillCount}";
            }
            GameDataManager.Instance.SaveGame();
        }
        if (_clearTimeText != null)
        {
            _clearTimeText.text = $"Survived Time: {elapsedTime:F2}s";
        }
    }

    private void OnGameOver()
    {
        Time.timeScale = 0f;
        GameOverPanel.SetActive(true);

        if (GameDataManager.Instance != null)
        {
            if (_gameOverKillText != null)
            {
                _gameOverKillText.text = $"Total Kills: {GameDataManager.Instance.SessionKillCount}";
            }
            GameDataManager.Instance.SaveGame();
        }
        if (_gameOverTimeText != null)
        {
            _gameOverTimeText.text = $"Survived Time: {elapsedTime:F2}s";
        }
    }

    void OnDestroy()
    {
        EventManager.OnPhaseChanged -= UpdatePhaseData;
        EventManager.OnLevelUp -= UpdateLevelUI;
        EventManager.OnPlayerHpChanged -= UpdatePlayerHpUI;
        EventManager.OnGameClear -= OnGameClear;
        EventManager.OnPlayerDeath -= OnGameOver;

        UnbindButtonEvents();
    }
}
