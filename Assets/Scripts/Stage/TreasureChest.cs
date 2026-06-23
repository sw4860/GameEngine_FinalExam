using UnityEngine;

public class TreasureChest : MonoBehaviour
{
    [SerializeField] private float _magneticSpeed = 8f;
    [SerializeField] private float _magnetRadius = 4f;

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
        if (dist <= _magnetRadius)
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
            if (CursedChestUIManager.Instance != null)
            {
                CursedChestUIManager.Instance.ShowCursedChestPanel();
            }
            Destroy(gameObject);
        }
    }
}
