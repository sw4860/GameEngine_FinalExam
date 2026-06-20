using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class ButtonImageController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Sprites")]
    [SerializeField] private Sprite originalSprite;
    [SerializeField] private Sprite onPressedSprite;

    [Header("DOTween Animation Settings")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float duration = 0.15f;
    [SerializeField] private Ease easeType = Ease.OutQuad;

    private Button button;
    private Image image;
    private bool isHovered = false;

    void Start()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();
        Init();
    }

    void Init()
    {
        if (image != null && originalSprite != null)
        {
            image.sprite = originalSprite;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        isHovered = true;

        transform.DOKill();
        transform.DOScale(hoverScale, duration)
            .SetEase(easeType)
            .SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        isHovered = false;

        transform.DOKill();
        transform.DOScale(1f, duration)
            .SetEase(easeType)
            .SetUpdate(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;

        if (image != null && onPressedSprite != null)
        {
            image.sprite = onPressedSprite;
        }

        transform.DOKill();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;

        if (image != null && originalSprite != null)
        {
            image.sprite = originalSprite;
        }

        transform.DOKill();
        float targetScale = isHovered ? hoverScale : 1f;
        transform.DOScale(targetScale, duration)
            .SetEase(easeType)
            .SetUpdate(true);
    }
}
