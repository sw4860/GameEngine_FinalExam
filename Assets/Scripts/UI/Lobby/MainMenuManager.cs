using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;

public class MainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button GameStartButton;
    [SerializeField] private Button UpgradeButton;
    [SerializeField] private Button CharacterSelectButton;
    [SerializeField] private Button MapSelectButton;
    [SerializeField] private Button AchievementButton;
    [SerializeField] private Button SettingsButton;
    [SerializeField] private Button QuitButton;

    [Header("Lobby UI Panels")]
    [SerializeField] private LobbyUpgradeUIManager UpgradeUI;
    [SerializeField] private CharacterSelectionUIManager CharacterSelectUI;
    [SerializeField] private StageSelectionUIManager MapSelectUI;
    [SerializeField] private AchievementListUIManager AchievementUI;
    [SerializeField] private SettingsUIManager SettingsUI;

    [Header("Press Any Key Screen")]
    [SerializeField] private GameObject pressAnyKeyPanel;
    [SerializeField] private CanvasGroup pressAnyKeyCanvasGroup;
    [SerializeField] private RectTransform pressAnyKeyText;
    [SerializeField] private float pulseDuration = 0.8f;
    [SerializeField] private float pulseScale = 1.05f;

    [Header("Main Menu Screen")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private CanvasGroup mainMenuCanvasGroup;
    [SerializeField] private RectTransform[] menuElements;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float slideOffset = 80f;
    [SerializeField] private float staggerDelay = 0.12f;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    private Vector2[] originalPositions;
    private bool isKeyPressed = false;
    private Tween pulseTween;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (GameStartButton != null)
        {
            GameStartButton.onClick.AddListener(OnGameStart);
        }
        if (UpgradeButton != null && UpgradeUI != null)
        {
            UpgradeButton.onClick.AddListener(ToggleUpgradeUI);
        }
        if (CharacterSelectButton != null && CharacterSelectUI != null)
        {
            CharacterSelectButton.onClick.AddListener(ToggleCharacterSelectUI);
        }
        if (MapSelectButton != null && MapSelectUI != null)
        {
            MapSelectButton.onClick.AddListener(ToggleMapSelectUI);
        }
        if (AchievementButton != null && AchievementUI != null)
        {
            AchievementButton.onClick.AddListener(ToggleAchievementUI);
        }
        if (SettingsButton != null && SettingsUI != null)
        {
            SettingsButton.onClick.AddListener(ToggleSettingsUI);
        }
        if (QuitButton != null)
        {
            QuitButton.onClick.AddListener(OnQuitGame);
        }
        InitUIState();
    }

    private void Start()
    {
        StartPulseAnimation();

        // 첫 토글 클릭 시 정상 작동하도록 게임 시작 시 모든 패널을 비활성화 처리 (Start 단계에서 실행하여 데이터 로딩 유도)
        if (UpgradeUI != null) UpgradeUI.gameObject.SetActive(false);
        if (CharacterSelectUI != null) CharacterSelectUI.gameObject.SetActive(false);
        if (MapSelectUI != null) MapSelectUI.gameObject.SetActive(false);
        if (AchievementUI != null) AchievementUI.gameObject.SetActive(false);
        if (SettingsUI != null) SettingsUI.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isKeyPressed && CheckAnyKeyPress())
        {
            isKeyPressed = true;
            TransitionToMainMenu();
        }
    }

    private void InitUIState()
    {
        // Initialize "Press Any Key" panel
        if (pressAnyKeyPanel != null)
        {
            pressAnyKeyPanel.SetActive(true);
        }
        if (pressAnyKeyCanvasGroup != null)
        {
            pressAnyKeyCanvasGroup.alpha = 1f;
        }

        // Initialize "Main Menu" panel and elements
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.alpha = 0f;
        }

        // Save original positions and hide menu elements
        if (menuElements != null && menuElements.Length > 0)
        {
            originalPositions = new Vector2[menuElements.Length];
            for (int i = 0; i < menuElements.Length; i++)
            {
                if (menuElements[i] != null)
                {
                    originalPositions[i] = menuElements[i].anchoredPosition;
                    menuElements[i].localScale = Vector3.zero;
                    menuElements[i].anchoredPosition = originalPositions[i] - new Vector2(0, slideOffset);

                    CanvasGroup cg = menuElements[i].GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.alpha = 0f;
                    }
                }
            }
        }
    }

    private void StartPulseAnimation()
    {
        if (pressAnyKeyText != null)
        {
            pulseTween = pressAnyKeyText.DOScale(pulseScale, pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    private bool CheckAnyKeyPress()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            return true;
            
        return false;
    }

    private void TransitionToMainMenu()
    {
        // Stop pulse animation
        if (pulseTween != null)
        {
            pulseTween.Kill();
        }

        // Quick pop-up scale animation for "Press Any Key" text confirmation
        if (pressAnyKeyText != null)
        {
            pressAnyKeyText.DOScale(pulseScale * 1.1f, 0.15f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // Fade out
                    if (pressAnyKeyCanvasGroup != null)
                    {
                        pressAnyKeyCanvasGroup.DOFade(0f, fadeDuration)
                            .OnComplete(() =>
                            {
                                if (pressAnyKeyPanel != null) pressAnyKeyPanel.SetActive(false);
                                ShowMainMenu();
                            });
                    }
                    else
                    {
                        if (pressAnyKeyPanel != null) pressAnyKeyPanel.SetActive(false);
                        ShowMainMenu();
                    }
                });
        }
        else
        {
            if (pressAnyKeyCanvasGroup != null)
            {
                pressAnyKeyCanvasGroup.DOFade(0f, fadeDuration)
                    .OnComplete(() =>
                    {
                        if (pressAnyKeyPanel != null) pressAnyKeyPanel.SetActive(false);
                        ShowMainMenu();
                    });
            }
            else
            {
                if (pressAnyKeyPanel != null) pressAnyKeyPanel.SetActive(false);
                ShowMainMenu();
            }
        }
    }

    private void ShowMainMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.DOFade(1f, fadeDuration);
        }

        if (menuElements != null && menuElements.Length > 0)
        {
            for (int i = 0; i < menuElements.Length; i++)
            {
                RectTransform element = menuElements[i];
                if (element == null) continue;

                element.DOKill();

                // Slide back to its original position
                element.DOAnchorPos(originalPositions[i], animationDuration)
                    .SetEase(easeType)
                    .SetDelay(i * staggerDelay);

                // Scale up to 1
                element.DOScale(Vector3.one, animationDuration)
                    .SetEase(easeType)
                    .SetDelay(i * staggerDelay);

                // Fade in if CanvasGroup is attached
                CanvasGroup cg = element.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.DOFade(1f, animationDuration)
                        .SetEase(Ease.OutQuad)
                        .SetDelay(i * staggerDelay);
                }
            }
        }
    }

    private void OnGameStart()
    {
        SceneTransitionManager.Instance.LoadScene("GameScene");
    }

    private void OnDestroy()
    {
        if (GameStartButton != null)
        {
            GameStartButton.onClick.RemoveListener(OnGameStart);
        }
        if (UpgradeButton != null && UpgradeUI != null)
        {
            UpgradeButton.onClick.RemoveListener(ToggleUpgradeUI);
        }
        if (CharacterSelectButton != null && CharacterSelectUI != null)
        {
            CharacterSelectButton.onClick.RemoveListener(ToggleCharacterSelectUI);
        }
        if (MapSelectButton != null && MapSelectUI != null)
        {
            MapSelectButton.onClick.RemoveListener(ToggleMapSelectUI);
        }
        if (AchievementButton != null && AchievementUI != null)
        {
            AchievementButton.onClick.RemoveListener(ToggleAchievementUI);
        }
        if (SettingsButton != null && SettingsUI != null)
        {
            SettingsButton.onClick.RemoveListener(ToggleSettingsUI);
        }
    }

    private void ToggleUpgradeUI()
    {
        if (UpgradeUI == null) return;
        bool isOpen = UpgradeUI.gameObject.activeSelf;
        CloseAllPanels();
        if (!isOpen)
        {
            UpgradeUI.Show();
        }
    }

    private void ToggleCharacterSelectUI()
    {
        if (CharacterSelectUI == null) return;
        bool isOpen = CharacterSelectUI.gameObject.activeSelf;
        CloseAllPanels();
        if (!isOpen)
        {
            CharacterSelectUI.Show();
        }
    }

    private void ToggleMapSelectUI()
    {
        if (MapSelectUI == null) return;
        bool isOpen = MapSelectUI.gameObject.activeSelf;
        CloseAllPanels();
        if (!isOpen)
        {
            MapSelectUI.Show();
        }
    }

    private void ToggleAchievementUI()
    {
        if (AchievementUI == null) return;
        bool isOpen = AchievementUI.gameObject.activeSelf;
        CloseAllPanels();
        if (!isOpen)
        {
            AchievementUI.Show();
        }
    }

    private void ToggleSettingsUI()
    {
        if (SettingsUI == null) return;
        bool isOpen = SettingsUI.gameObject.activeSelf;
        CloseAllPanels();
        if (!isOpen)
        {
            SettingsUI.Show();
        }
    }

    private void OnQuitGame()
    {
        GameDataManager.Instance.SaveGame();
        Application.Quit();
    }

    private void CloseAllPanels()
    {
        if (UpgradeUI != null && UpgradeUI.gameObject.activeSelf)
        {
            UpgradeUI.Hide();
        }
        if (CharacterSelectUI != null && CharacterSelectUI.gameObject.activeSelf)
        {
            CharacterSelectUI.Hide();
        }
        if (MapSelectUI != null && MapSelectUI.gameObject.activeSelf)
        {
            MapSelectUI.Hide();
        }
        if (AchievementUI != null && AchievementUI.gameObject.activeSelf)
        {
            AchievementUI.Hide();
        }
        if (SettingsUI != null && SettingsUI.gameObject.activeSelf)
        {
            SettingsUI.Hide();
        }
    }
}
