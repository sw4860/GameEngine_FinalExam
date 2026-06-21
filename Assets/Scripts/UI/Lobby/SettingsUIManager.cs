using UnityEngine;
using DG.Tweening;

public class SettingsUIManager : MonoBehaviour
{
    [Header("UI Transition")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelRect;

    [Header("Animations")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float scaleDuration = 0.3f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    private void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        if (panelRect != null)
        {
            panelRect.localScale = Vector3.zero;
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);

        canvasGroup.DOKill();
        panelRect.DOKill();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
        }
        if (panelRect != null)
        {
            panelRect.DOScale(Vector3.one, scaleDuration).SetEase(easeType).SetUpdate(true);
        }
    }

    public void Hide()
    {
        canvasGroup.DOKill();
        panelRect.DOKill();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.DOFade(0f, fadeDuration).SetUpdate(true);
        }
        if (panelRect != null)
        {
            panelRect.DOScale(Vector3.zero, scaleDuration).SetEase(Ease.InQuad).SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}
