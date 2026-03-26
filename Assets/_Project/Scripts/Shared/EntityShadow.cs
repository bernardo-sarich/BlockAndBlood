using UnityEngine;

/// <summary>
/// Adds a shadow child beneath any entity (hero, enemy, tower).
/// Two modes:
///   - Ellipse (default): generates a soft circle texture at runtime, squished into an ellipse.
///   - Sprite: uses the entity's actual sprite, tinted black, squashed and flipped — syncs
///     each frame so it follows directional animations.
///
/// Shadow spec (from CLAUDE.md):
///   Alpha: 0.3, Scale: 0.7×0.35, Layer: below entity via sortingOrder
/// </summary>
public class EntityShadow : MonoBehaviour
{
    private static readonly Color EllipseShadowColor = new Color(0f, 0f, 0f, 0.5f);
    private static readonly Color SpriteShadowColor  = new Color(0f, 0f, 0f, 0.35f);
    private const int ShadowSortingOrder = -5000; // Above ground tiles but below all entities

    /// <summary>
    /// Optional override: use this renderer as the shadow source instead of the
    /// root SpriteRenderer. Useful when the root SR is disabled (e.g. layered hero).
    /// </summary>
    [SerializeField] public SpriteRenderer sourceRenderer;

    /// <summary>Shadow width and height in local space (relative to parent).</summary>
    [SerializeField] private Vector2 _scale = new Vector2(1.0f, 0.5f);

    /// <summary>Y offset in local space. Negative = below entity center.</summary>
    [SerializeField] private float _yOffset = -0.55f;

    /// <summary>
    /// When true, uses the entity's actual sprite (tinted black, squashed, flipped 180°)
    /// instead of the procedural ellipse. The shadow sprite syncs each frame.
    /// </summary>
    [SerializeField] private bool _useSpriteAsShadow;

    private static Sprite _ellipseSprite;

    // Cached references for sprite-shadow mode
    private SpriteRenderer _parentSr;
    private SpriteRenderer _shadowSr;

    private void Start()
    {
        // Skip if a Shadow child already exists (e.g. manually placed in prefab/scene)
        if (transform.Find("Shadow") != null) return;

        _parentSr = sourceRenderer != null ? sourceRenderer : GetComponent<SpriteRenderer>();
        if (_parentSr == null) return;

        if (_useSpriteAsShadow)
            CreateSpriteShadow();
        else
            CreateEllipseShadow();
    }

    private void LateUpdate()
    {
        // Only sprite-shadow mode needs per-frame sync
        if (!_useSpriteAsShadow || _shadowSr == null || _parentSr == null) return;

        _shadowSr.sprite = _parentSr.sprite;
        _shadowSr.flipX  = _parentSr.flipX;
    }

    private void CreateSpriteShadow()
    {
        var shadowGo = new GameObject("Shadow");
        shadowGo.transform.SetParent(transform);
        shadowGo.transform.localPosition = new Vector3(0f, _yOffset, 0f);
        shadowGo.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        shadowGo.transform.localScale    = new Vector3(_scale.x, _scale.y, 1f);

        _shadowSr                  = shadowGo.AddComponent<SpriteRenderer>();
        _shadowSr.sprite           = _parentSr.sprite;
        _shadowSr.color            = SpriteShadowColor;
        _shadowSr.flipX            = _parentSr.flipX;
        _shadowSr.sortingLayerName = _parentSr.sortingLayerName;
        _shadowSr.sortingOrder     = ShadowSortingOrder;
    }

    private void CreateEllipseShadow()
    {
        if (_ellipseSprite == null)
            _ellipseSprite = CreateEllipseSprite(64);

        var shadowGo = new GameObject("Shadow");
        shadowGo.transform.SetParent(transform);
        shadowGo.transform.localPosition = new Vector3(0f, _yOffset, 0f);
        shadowGo.transform.localRotation = Quaternion.identity;
        shadowGo.transform.localScale    = new Vector3(_scale.x, _scale.y, 1f);

        _shadowSr                  = shadowGo.AddComponent<SpriteRenderer>();
        _shadowSr.sprite           = _ellipseSprite;
        _shadowSr.color            = EllipseShadowColor;
        _shadowSr.sortingLayerName = _parentSr.sortingLayerName;
        _shadowSr.sortingOrder     = ShadowSortingOrder;
    }

    private static Sprite CreateEllipseSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float center = size * 0.5f;
        float radius = center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / radius;

                // Soft circle: fully opaque at center, smooth fade at edge
                float alpha = Mathf.Clamp01(1f - dist * dist); // smooth falloff
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
