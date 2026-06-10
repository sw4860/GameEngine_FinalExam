using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private Button GameStartButton;

    private void Awake()
    {
        GameStartButton.onClick.AddListener(OnGameStart);
    }

    private void OnGameStart()
    {
        SceneTransitionManager.Instance.LoadScene("GameScene");
    }

    private void OnDestroy()
    {
        GameStartButton.onClick.RemoveListener(OnGameStart);
    }
}
