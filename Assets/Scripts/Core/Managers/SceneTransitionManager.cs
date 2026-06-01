using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;
    [SerializeField] Image image;
    [SerializeField] Color imageColor;
    [SerializeField] float transitionDuration = 0.5f;

    private bool isTransition = false;

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(Instance);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName)
    {
        if (isTransition) return;
        //GameObject Canvas = GameObject.FindGameObjectWithTag("Canvas");
        //if (Canvas != null) Canvas.SetActive(false);
        StartCoroutine(SceneTransitionCoroutine(sceneName));
    }

    IEnumerator SceneTransitionCoroutine(string sceneName)
    {
        isTransition = true;

        yield return StartCoroutine(Fade(1f));
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return StartCoroutine(Fade(0f));

        isTransition = false;
    }

    IEnumerator Fade(float targetAlpha)
    {
        float timer = 0f;
        Color startColor = image.color;
        Color targetColor = new Color(imageColor.r, imageColor.g, imageColor.b, targetAlpha);

        while (timer < transitionDuration)
        {
            timer += Time.unscaledDeltaTime;
            image.color = Color.Lerp(startColor, targetColor, timer / transitionDuration);
            yield return null;
        }
        image.color = targetColor;
    }
}