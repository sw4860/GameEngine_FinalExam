using System.Collections.Generic;
using UnityEngine;

public class DamageTextManager : MonoBehaviour
{
    private static DamageTextManager _instance;

    [SerializeField]
    private GameObject _damageTextPrefab;

    [SerializeField]
    private int _initialPoolSize = 50;

    private readonly List<IDamageText> _activeTexts = new List<IDamageText>();
    private readonly Queue<IDamageText> _textPool = new Queue<IDamageText>();
    private bool _isEnabled = true;

    public static DamageTextManager Instance
    {
        get
        {
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        InitializePool();
    }

    private void Start()
    {
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentData != null)
        {
            _isEnabled = GameDataManager.Instance.CurrentData.IsDamageTextEnabled;
        }
    }

    private void InitializePool()
    {
        for (int i = 0; i < _initialPoolSize; i++)
        {
            GameObject obj = Instantiate(_damageTextPrefab, transform);
            if (obj.TryGetComponent(out IDamageText textComponent))
            {
                textComponent.Deactivate();
                _textPool.Enqueue(textComponent);
            }
        }
    }

    public void ApplySettings(bool isEnabled)
    {
        _isEnabled = isEnabled;
        if (!_isEnabled)
        {
            ClearActiveTexts();
        }
    }

    private void ClearActiveTexts()
    {
        int count = _activeTexts.Count;
        for (int i = 0; i < count; i++)
        {
            IDamageText damageText = _activeTexts[i];
            damageText.Deactivate();
            _textPool.Enqueue(damageText);
        }
        _activeTexts.Clear();
    }

    public void SpawnDamageText(Vector3 position, float damage)
    {
        if (!_isEnabled)
        {
            return;
        }

        IDamageText damageText;

        if (_textPool.Count > 0)
        {
            damageText = _textPool.Dequeue();
        }
        else
        {
            GameObject obj = Instantiate(_damageTextPrefab, transform);
            if (obj.TryGetComponent(out damageText))
            {
                damageText.Deactivate();
            }
            else
            {
                return;
            }
        }

        damageText.Initialize(position, damage);
        _activeTexts.Add(damageText);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        int activeCount = _activeTexts.Count;

        for (int i = activeCount - 1; i >= 0; i--)
        {
            IDamageText damageText = _activeTexts[i];
            if (!damageText.UpdateTick(deltaTime))
            {
                damageText.Deactivate();
                _activeTexts.RemoveAt(i);
                _textPool.Enqueue(damageText);
            }
        }
    }
}
