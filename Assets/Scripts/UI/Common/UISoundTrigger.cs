using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UISoundTrigger : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private AudioClip _hoverSound;
    [SerializeField] private AudioClip _clickSound;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_button != null && !_button.interactable) return;

        if (_hoverSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(_hoverSound);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_button != null && !_button.interactable) return;

        if (_clickSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(_clickSound);
        }
    }
}
