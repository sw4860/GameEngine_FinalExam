using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour, IDamageText
{
    [SerializeField]
    private TMP_Text _textMesh;

    [SerializeField]
    private float _moveSpeed = 1.5f;

    [SerializeField]
    private float _lifeTime = 0.8f;

    private Vector3 _startPosition;
    private float _elapsedTime;
    private Color _initialColor;

    private void Awake()
    {
        _initialColor = _textMesh.color;
    }

    public void Initialize(Vector3 position, float damage)
    {
        transform.position = position;
        _startPosition = position;
        _elapsedTime = 0f;
        
        _textMesh.SetText("{0:1}", damage);
        
        _textMesh.color = _initialColor;
        gameObject.SetActive(true);
    }

    public bool UpdateTick(float deltaTime)
    {
        _elapsedTime += deltaTime;
        if (_elapsedTime >= _lifeTime)
        {
            return false;
        }

        float progress = _elapsedTime / _lifeTime;
        
        transform.position = _startPosition + Vector3.up * (_moveSpeed * _elapsedTime);

        Color targetColor = _initialColor;
        targetColor.a = Mathf.Lerp(_initialColor.a, 0f, progress);
        _textMesh.color = targetColor;

        return true;
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }
}
