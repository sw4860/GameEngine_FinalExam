using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementUIItem : MonoBehaviour
{
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [SerializeField] private Sprite _particleSprite;
    [SerializeField] private int _particleCount = 20;
    [SerializeField] private float _particleMinSpeed = 150f;
    [SerializeField] private float _particleMaxSpeed = 350f;
    [SerializeField] private float _particleGravity = 300f;
    [SerializeField] private float _particleFadeDuration = 2f;

    [SerializeField] private float _moveDuration = 0.4f;
    [SerializeField] private float _displayDuration = 3f;

    private Vector2 _normalStartPosition;
    private Vector2 _normalTargetPosition;
    private Coroutine _animationCoroutine;
    private Vector3 _originalScale;
    private bool _isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        if (_rectTransform != null)
        {
            _originalScale = _rectTransform.localScale;
            if (_originalScale.sqrMagnitude < 0.001f)
            {
                _originalScale = Vector3.one;
            }
        }
        else
        {
            _originalScale = Vector3.one;
        }

        _isInitialized = true;
    }

    private void CalculateNormalPositions()
    {
        if (_rectTransform == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        float canvasHeight = 1080f;
        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasHeight = canvasRect.rect.height;
            }
        }

        _rectTransform.anchorMin = new Vector2(1f, 1f);
        _rectTransform.anchorMax = new Vector2(1f, 1f);
        _rectTransform.pivot = new Vector2(1f, 1f);

        float width = _rectTransform.rect.width;
        float height = _rectTransform.rect.height;

        _normalStartPosition = new Vector2(width + 50f, -20f);
        _normalTargetPosition = new Vector2(-20f, -20f);
    }

    public void Setup(AchievementData data)
    {
        Initialize();

        if (_iconImage != null)
        {
            _iconImage.sprite = data.Icon;
        }

        if (_titleText != null)
        {
            _titleText.text = data.Title;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = data.Description;
        }

        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }

        if (data.Grade == AchievementGrade.Normal)
        {
            CalculateNormalPositions();
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = _normalStartPosition;
                _rectTransform.localScale = _originalScale;
            }
            _animationCoroutine = StartCoroutine(AnimateNormalPopup());
        }
        else if (data.Grade == AchievementGrade.Challenge)
        {
            if (_rectTransform != null)
            {
                _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _rectTransform.pivot = new Vector2(0.5f, 0.5f);

                _rectTransform.anchoredPosition = Vector2.zero;
                _rectTransform.localScale = Vector3.zero;
            }
            _animationCoroutine = StartCoroutine(AnimateChallengePopup());
            
            if (_particleSprite != null)
            {
                TriggerChallengeParticles();
            }
        }
    }

    public void Hide()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
        Destroy(gameObject);
    }

    private IEnumerator AnimateNormalPopup()
    {
        _rectTransform.localScale = _originalScale;
        _rectTransform.anchoredPosition = _normalStartPosition;
        float elapsed = 0f;

        while (elapsed < _moveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / _moveDuration;
            t = Mathf.SmoothStep(0f, 1f, t);
            _rectTransform.anchoredPosition = Vector2.Lerp(_normalStartPosition, _normalTargetPosition, t);
            yield return null;
        }

        _rectTransform.anchoredPosition = _normalTargetPosition;
        yield return new WaitForSecondsRealtime(_displayDuration);

        elapsed = 0f;
        while (elapsed < _moveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / _moveDuration;
            t = Mathf.SmoothStep(0f, 1f, t);
            _rectTransform.anchoredPosition = Vector2.Lerp(_normalTargetPosition, _normalStartPosition, t);
            yield return null;
        }

        _rectTransform.anchoredPosition = _normalStartPosition;
        Hide();
    }

    private IEnumerator AnimateChallengePopup()
    {
        float elapsed = 0f;
        float scaleDuration = 0.3f;
        float peakScale = _originalScale.x * 1.15f;

        while (elapsed < scaleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / scaleDuration;
            float scale = Mathf.Lerp(0f, peakScale, t);
            _rectTransform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        elapsed = 0f;
        float bounceDuration = 0.15f;
        while (elapsed < bounceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / bounceDuration;
            float scale = Mathf.Lerp(peakScale, _originalScale.x, t);
            _rectTransform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        _rectTransform.localScale = _originalScale;
        yield return new WaitForSecondsRealtime(_displayDuration);

        elapsed = 0f;
        float fadeOutDuration = 0.3f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeOutDuration;
            float scale = Mathf.Lerp(_originalScale.x, 0f, t);
            _rectTransform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        _rectTransform.localScale = Vector3.zero;
        Hide();
    }

    private void TriggerChallengeParticles()
    {
        for (int i = 0; i < _particleCount; i++)
        {
            GameObject particleObj = new GameObject("UIParticle", typeof(RectTransform), typeof(Image));
            particleObj.transform.SetParent(transform, false);
            particleObj.transform.SetAsFirstSibling();

            RectTransform rect = particleObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(25f, 25f);
            rect.anchoredPosition = Vector2.zero;

            Image img = particleObj.GetComponent<Image>();
            img.sprite = _particleSprite;
            img.color = new Color(Random.value, Random.value, Random.value, 1f);

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(_particleMinSpeed, _particleMaxSpeed);
            Vector2 velocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);

            float startScale = Random.Range(0.5f, 1.5f);
            float randomFadeDuration = Random.Range(_particleFadeDuration * 0.6f, _particleFadeDuration * 1.4f);

            StartCoroutine(AnimateParticle(rect, img, velocity, startScale, randomFadeDuration));
        }
    }

    private IEnumerator AnimateParticle(RectTransform rect, Image img, Vector2 velocity, float startScale, float fadeDuration)
    {
        float elapsed = 0f;
        Color startColor = img.color;
        Vector2 position = rect.anchoredPosition;
        Vector3 initialScale = new Vector3(startScale, startScale, 1f);

        while (elapsed < fadeDuration)
        {
            if (rect == null || img == null) yield break;

            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeDuration;

            velocity.y -= _particleGravity * Time.unscaledDeltaTime;
            position += velocity * Time.unscaledDeltaTime;
            rect.anchoredPosition = position;

            img.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), t);
            rect.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);

            yield return null;
        }

        if (rect != null)
        {
            Destroy(rect.gameObject);
        }
    }
}





