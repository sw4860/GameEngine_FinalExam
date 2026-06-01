using UnityEngine;

public class ExpEntity : MonoBehaviour
{
    public int SpatialIndex = -1;
    public bool IsActive = false;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Init(int index, Sprite sprite)
    {
        SpatialIndex = index;
        IsActive = true;
        UpdateSprite(sprite);
    }

    public void UpdateSprite(Sprite sprite)
    {
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }
}
