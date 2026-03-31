using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the game cursor:
///   - Default: custom pointer texture (assigned in Inspector)
///   - Placement mode: hides hardware cursor, shows tower sprite preview
///     snapped to grid cells, tinted green (valid) or red (invalid)
/// </summary>
public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [Header("Default Cursor")]
    [SerializeField] private Texture2D _cursorTexture;
    [SerializeField] private Texture2D _cursorDownTexture;
    [SerializeField] private Vector2   _cursorHotspot = Vector2.zero;

    [Header("Placement Preview")]
    [SerializeField] private Color _validColor   = new Color(0.3f, 1f, 0.3f, 0.5f);
    [SerializeField] private Color _invalidColor = new Color(1f, 0.3f, 0.3f, 0.5f);

    private Camera         _cam;
    private GameObject     _previewObj;
    private SpriteRenderer _previewRenderer;
    private SpriteRenderer _cursorSpriteRenderer;
    private bool           _inPlacementMode;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _cam = Camera.main;
        ApplyDefaultCursor();
    }

    private void OnEnable()
    {
        TowerPlacementManager.OnTowerSelected      += OnTowerSelected;
        TowerPlacementManager.OnPlacementCancelled += OnPlacementCancelled;
    }

    private void OnDisable()
    {
        TowerPlacementManager.OnTowerSelected      -= OnTowerSelected;
        TowerPlacementManager.OnPlacementCancelled -= OnPlacementCancelled;
    }

    private void Update()
    {
        if (CardSystem.IsPickerActive)
        {
            Cursor.visible = true;
            return; // suspend cursor/preview logic while card picker is shown
        }

        if (_inPlacementMode && _previewRenderer != null)
        {
            Cursor.visible = false;
            UpdatePreview();
            return;
        }

        HandleCursorClick();
    }

    private void HandleCursorClick()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame && _cursorDownTexture != null)
            Cursor.SetCursor(_cursorDownTexture, _cursorHotspot, CursorMode.Auto);
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
            ApplyDefaultCursor();
    }

    private void ApplyDefaultCursor()
    {
        if (_cursorTexture != null)
            Cursor.SetCursor(_cursorTexture, _cursorHotspot, CursorMode.Auto);
    }

    // ── Event handlers ──────────────────────────────────────────────────────

    private void OnTowerSelected(TowerData data)
    {
        _inPlacementMode = true;
        Cursor.visible = false;
        CreatePreview(data);
    }

    private void OnPlacementCancelled()
    {
        _inPlacementMode = false;
        Cursor.visible = true;
        ApplyDefaultCursor();
        DestroyPreview();
    }

    // ── Preview ─────────────────────────────────────────────────────────────

    private void CreatePreview(TowerData data)
    {
        DestroyPreview();

        Sprite sprite = data.Icon;
        Vector3 scale = Vector3.one;

        if (data.TowerPrefab != null)
        {
            if (sprite == null)
            {
                var sr = data.TowerPrefab.GetComponent<SpriteRenderer>();
                if (sr != null) sprite = sr.sprite;
            }
            scale = data.TowerPrefab.transform.localScale;
        }

        if (sprite == null)
        {
            Debug.LogWarning("[CursorManager] No sprite found on TowerPrefab for preview.");
            return;
        }

        _previewObj = new GameObject("TowerCursorPreview");
        _previewRenderer                  = _previewObj.AddComponent<SpriteRenderer>();
        _previewRenderer.sprite           = sprite;
        _previewRenderer.sortingLayerName = "Effects";
        _previewRenderer.sortingOrder     = 100;
        _previewRenderer.color            = _validColor;
        _previewObj.transform.localScale  = scale;

        // Cursor sprite at bottom-right corner of the preview
        CreateCursorChild(sprite, scale);
    }

    private void CreateCursorChild(Sprite towerSprite, Vector3 parentScale)
    {
        if (_cursorTexture == null) return;

        // Convert cursor Texture2D to a Sprite
        var cursorSprite = Sprite.Create(
            _cursorTexture,
            new Rect(0, 0, _cursorTexture.width, _cursorTexture.height),
            new Vector2(0f, 1f),  // pivot top-left so it hangs from the anchor point
            _cursorTexture.width); // PPU = texture width → 1 world unit wide

        var cursorGO = new GameObject("CursorIcon");
        cursorGO.transform.SetParent(_previewObj.transform, false);

        _cursorSpriteRenderer                  = cursorGO.AddComponent<SpriteRenderer>();
        _cursorSpriteRenderer.sprite           = cursorSprite;
        _cursorSpriteRenderer.sortingLayerName = "Effects";
        _cursorSpriteRenderer.sortingOrder     = 101;

        // Size: ~0.625 cells, compensate for parent scale
        float desiredSize = 0.625f * GridManager.CellSize;
        float sx = parentScale.x != 0f ? desiredSize / parentScale.x : desiredSize;
        float sy = parentScale.y != 0f ? desiredSize / parentScale.y : desiredSize;
        cursorGO.transform.localScale = new Vector3(sx, sy, 1f);

        // Position at bottom-right of the tower sprite bounds
        float halfW = towerSprite.rect.width  / towerSprite.pixelsPerUnit * 0.5f;
        float halfH = towerSprite.rect.height / towerSprite.pixelsPerUnit * 0.5f;
        cursorGO.transform.localPosition = new Vector3(halfW, -halfH, 0f);
    }

    private void DestroyPreview()
    {
        if (_previewObj != null) Destroy(_previewObj);
        _previewObj      = null;
        _previewRenderer = null;
    }

    private void UpdatePreview()
    {
        if (_cam == null || Mouse.current == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Ray ray = _cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
        float t = (ray.direction.z != 0f) ? -ray.origin.z / ray.direction.z : 0f;
        Vector3 world = ray.origin + ray.direction * t;
        world.z = 0f;

        var gm = GridManager.Instance;
        Vector2Int cell = gm.WorldToCell(world);

        if (gm.IsInBounds(cell))
        {
            // Snap to cell center
            _previewObj.transform.position = gm.CellToWorld(cell);
            bool valid = CanPlaceQuick(cell);
            _previewRenderer.color = valid ? _validColor : _invalidColor;
        }
        else
        {
            // Follow mouse freely outside the grid
            _previewObj.transform.position = world;
            _previewRenderer.color = _invalidColor;
        }
    }

    /// <summary>
    /// Lightweight placement check for visual feedback only — skips the expensive
    /// A* pathfinding validation. The full CanPlaceTower() runs on click.
    /// </summary>
    private bool CanPlaceQuick(Vector2Int cell)
    {
        var gm = GridManager.Instance;
        if (!gm.IsInBounds(cell)) return false;
        if (gm.GetCellState(cell) != GridManager.CellState.Libre) return false;
        if (cell == gm.SpawnCell || cell == gm.GoalCell) return false;
        if (gm.IsRestricted(cell)) return false;
        if (cell.x < GridVisualizer.PathColMin || cell.x > GridVisualizer.PathColMax) return false;
        return true;
    }
}
