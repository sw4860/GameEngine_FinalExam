using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CursedContractCardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _returnText;
    [SerializeField] private TextMeshProUGUI _riskText;
    [SerializeField] private Button _selectButton;

    public void Setup(string title, string returnDesc, string riskDesc, System.Action onClickAction)
    {
        if (_titleText != null) _titleText.text = title;
        if (_returnText != null) _returnText.text = returnDesc;
        if (_riskText != null) _riskText.text = riskDesc;

        if (_selectButton != null)
        {
            _selectButton.onClick.RemoveAllListeners();
            _selectButton.onClick.AddListener(() => onClickAction?.Invoke());
        }
    }
}
