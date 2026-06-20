using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class UpgradeUIManager : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup CanvasGroup;
    public RectTransform Panel;
    public Transform UpgradeButtonsContainer;
    public GameObject UpgradeButtonPrefab;

    private UpgradeManager _manager;
    private bool _isSelecting = false;

    void Awake()
    {
        CanvasGroup.alpha = 0f;
        CanvasGroup.blocksRaycasts = false;
        Panel.localScale = Vector3.zero;
    }

    public void Show(UpgradeManager manager, List<SkillData> choices)
    {
        _manager = manager;
        _isSelecting = false;

        Panel.DOKill();
        CanvasGroup.DOKill();
        
        foreach (Transform child in UpgradeButtonsContainer)
        {
            Destroy(child.gameObject);
        }
        
        for (int i = 0; i < choices.Count; i++)
        {
            var skill = choices[i];
            GameObject buttonObj = Instantiate(UpgradeButtonPrefab, UpgradeButtonsContainer);
            buttonObj.SetActive(true);
            var button = buttonObj.GetComponent<UpgradeButton>();
            button.SetSkill(skill, () => OnSelectSkill(skill));
        }

        CanvasGroup.blocksRaycasts = true;
        CanvasGroup.DOFade(1f, 0.5f).SetUpdate(true);
        Panel.DOScale(1f, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void Hide()
    {
        Panel.DOKill();
        CanvasGroup.DOKill();

        CanvasGroup.blocksRaycasts = false;
        Panel.DOScale(0f, 0.3f).SetEase(Ease.InQuad).SetUpdate(true);
        CanvasGroup.DOFade(0f, 0.2f).SetUpdate(true);
    }

    private void OnSelectSkill(SkillData skill)
    {
        if (_isSelecting) return;
        _isSelecting = true;

        CanvasGroup.blocksRaycasts = false;
        _manager.SelectUpgrade(skill);
    }
}
