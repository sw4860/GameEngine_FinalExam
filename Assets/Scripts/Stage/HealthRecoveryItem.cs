using UnityEngine;

public class HealthRecoveryItem : MonoBehaviour
{
    [Header("Heal Settings")]
    [Tooltip("Heals a flat amount of health.")]
    [SerializeField] private float _healAmount = 20f;
    [Tooltip("If true, heals a percentage of Max HP instead of a flat amount.")]
    [SerializeField] private bool _usePercentage = false;
    [Tooltip("Percentage of Max HP to heal (0.0 to 1.0).")]
    [Range(0f, 1f)]
    [SerializeField] private float _healPercentage = 0.2f;

    [Header("Movement Settings")]
    [SerializeField] private float _magneticSpeed = 10f;

    [Header("Audio")]
    [SerializeField] private AudioClip _healSound;

    private Transform _playerTransform;
    private bool _isCollected;

    private void Start()
    {
        if (PlayerStats.Instance != null)
        {
            _playerTransform = PlayerStats.Instance.transform;
        }
    }

    private void Update()
    {
        if (_isCollected || _playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        float currentMagnetRadius = 4f;

        if (PlayerStats.Instance != null && PlayerStats.Instance.StatData != null)
        {
            currentMagnetRadius = PlayerStats.Instance.StatData.CurrentMagnetRadius;
        }

        if (dist <= currentMagnetRadius)
        {
            transform.position = Vector3.MoveTowards(transform.position, _playerTransform.position, _magneticSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isCollected) return;

        if (collision.CompareTag("Player"))
        {
            _isCollected = true;

            // Apply Heal
            if (PlayerStats.Instance != null)
            {
                float healVal = _healAmount;
                if (_usePercentage)
                {
                    healVal = PlayerStats.Instance.MaxHp * _healPercentage;
                }
                PlayerStats.Instance.Heal(healVal);
            }

            // Play SFX
            if (_healSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(_healSound);
            }

            Destroy(gameObject);
        }
    }
}
