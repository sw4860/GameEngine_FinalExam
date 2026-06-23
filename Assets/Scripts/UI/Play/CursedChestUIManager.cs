using System.Collections.Generic;
using UnityEngine;

public class CursedChestUIManager : MonoBehaviour
{
    private static CursedChestUIManager _instance;
    public static CursedChestUIManager Instance => _instance;

    [SerializeField] private GameObject _panel;
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private Transform _cardsContainer;

    private CursedContractData[] _availableContracts;
    private readonly List<CursedContractCardUI> _spawnedCards = new List<CursedContractCardUI>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (_panel != null)
        {
            _panel.SetActive(false);
        }
    }

    public void ShowCursedChestPanel()
    {
        if (_availableContracts == null)
        {
            _availableContracts = Resources.LoadAll<CursedContractData>("SO/CursedContracts");
        }

        if (_availableContracts == null || _availableContracts.Length == 0)
        {
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.AddGold(100);
            }
            return;
        }

        ClearSpawnedCards();

        Time.timeScale = 0f;

        int firstIdx = Random.Range(0, _availableContracts.Length);
        int secondIdx = Random.Range(0, _availableContracts.Length);
        if (_availableContracts.Length > 1)
        {
            while (secondIdx == firstIdx)
            {
                secondIdx = Random.Range(0, _availableContracts.Length);
            }
        }

        CreateCard(_availableContracts[firstIdx], 0);
        CreateCard(_availableContracts[secondIdx], 1);
        CreateDeclineCard();

        if (_panel != null)
        {
            _panel.SetActive(true);
        }
    }

    private void CreateCard(CursedContractData data, int index)
    {
        if (_cardPrefab == null || _cardsContainer == null) return;

        GameObject cardObj = Instantiate(_cardPrefab, _cardsContainer);
        CursedContractCardUI cardUI = cardObj.GetComponent<CursedContractCardUI>();
        if (cardUI != null)
        {
            cardUI.Setup(data.Title, data.ReturnDescription, data.RiskDescription, () => OnCardSelected(index, data));
            _spawnedCards.Add(cardUI);
        }
    }

    private void CreateDeclineCard()
    {
        if (_cardPrefab == null || _cardsContainer == null) return;

        GameObject cardObj = Instantiate(_cardPrefab, _cardsContainer);
        CursedContractCardUI cardUI = cardObj.GetComponent<CursedContractCardUI>();
        if (cardUI != null)
        {
            cardUI.Setup("영혼의 거부", "+ 100 Gold 획득", "어떠한 저주도 받지 않고 거래를 파기합니다.", () => OnCardSelected(2, null));
            _spawnedCards.Add(cardUI);
        }
    }

    private void ClearSpawnedCards()
    {
        foreach (var card in _spawnedCards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
        _spawnedCards.Clear();
    }

    private void OnCardSelected(int index, CursedContractData data)
    {
        if (index == 2)
        {
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.AddGold(100);
            }
        }
        else if (data != null)
        {
            if (CursedContractManager.Instance != null)
            {
                CursedContractManager.Instance.ActivateContract(data);
            }
        }

        Time.timeScale = 1f;

        if (_panel != null)
        {
            _panel.SetActive(false);
        }
    }
}
