using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Tracks the currently selected entity (hero or tower) and renders a green
/// selection ellipse beneath it.  Drives HUD visibility via OnSelectionChanged.
///
/// Selection rules:
///   - Hero selected by default on game start.
///   - Left-click on active tower → select tower.
///   - Left-click on empty ground / ESC / right-click → reselect hero.
///   - Clicks are ignored during build-placement mode.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    /// <summary>(previous, current) — fired when the selected entity changes.</summary>
    public static event System.Action<ISelectable, ISelectable> OnSelectionChanged;

    public ISelectable Current { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Selection Indicator")]
    [SerializeField] private Color   _color  = new Color(0f, 1f, 0f, 0.7f);
    [SerializeField] private Vector2 _scale  = new Vector2(1.46f, 0.73f);
    [SerializeField] private float   _yOffset = -1.15f;

    // ── Internal state ───────────────────────────────────────────────────────
    private GameObject     _indicator;
    private SpriteRenderer _indicatorSr;
    private static Sprite  _ellipseSprite;
    private Camera         _cam;

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _cam = Camera.main;
        CreateIndicator();

        // Hero selected by default
        if (HeroBehaviour.Instance != null)
            Select(HeroBehaviour.Instance);
    }

    private void OnEnable()
    {
        TowerBehaviour.OnTowerClicked    += HandleTowerClicked;
        TowerBehaviour.OnTowerSold       += HandleTowerSold;
        GameManager.OnGameStateChanged   += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        TowerBehaviour.OnTowerClicked    -= HandleTowerClicked;
        TowerBehaviour.OnTowerSold       -= HandleTowerSold;
        GameManager.OnGameStateChanged   -= HandleGameStateChanged;
    }

    private void Update()
    {
        HandleClickInput();
        HandleCancelInput();
    }

    private void LateUpdate()
    {
        if (_indicator == null || Current == null) return;

        // Handle destroyed MonoBehaviour (e.g. tower sold between frames)
        if (Current is MonoBehaviour mb && mb == null)
        {
            SelectHero();
            return;
        }

        Vector3 pos = Current.SelectionTransform.position;
        pos.y += _yOffset;
        pos.z  = 0f;
        _indicator.transform.position = pos;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void Select(ISelectable target)
    {
        if (target == null || !target.IsSelectable) return;
        if (target == Current) return;

        ISelectable previous = Current;
        Current = target;

        _indicator?.SetActive(true);
        OnSelectionChanged?.Invoke(previous, Current);
    }

    public void SelectHero()
    {
        if (HeroBehaviour.Instance != null)
            Select(HeroBehaviour.Instance);
    }

    // ── Event handlers ───────────────────────────────────────────────────────

    private void HandleTowerClicked(TowerBehaviour tower)
    {
        // Don't select towers during build-placement mode
        if (TowerPlacementManager.Instance != null &&
            TowerPlacementManager.Instance.SelectedTower != null) return;

        if (tower is ISelectable selectable)
            Select(selectable);
    }

    private void HandleTowerSold(TowerBehaviour tower, int refund)
    {
        if (Current is TowerBehaviour tb && tb == tower)
            SelectHero();
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Paused && Current is TowerBehaviour)
            SelectHero();
    }

    // ── Input ────────────────────────────────────────────────────────────────

    private void HandleClickInput()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (_cam == null) return;

        // Don't interfere with build-placement mode
        if (TowerPlacementManager.Instance != null &&
            TowerPlacementManager.Instance.SelectedTower != null) return;

        // Don't interfere with UI clicks
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // Convert click to world → grid cell
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Ray ray = _cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
        float t = (ray.direction.z != 0f) ? -ray.origin.z / ray.direction.z : 0f;
        Vector3 world = ray.origin + ray.direction * t;
        world.z = 0f;

        if (GridManager.Instance == null) { SelectHero(); return; }

        Vector2Int cell = GridManager.Instance.WorldToCell(world);

        // If clicked cell has a tower, find and select it
        if (GridManager.Instance.IsInBounds(cell))
        {
            var state = GridManager.Instance.GetCellState(cell);
            if (state == GridManager.CellState.Ocupada || state == GridManager.CellState.EnConstruccion)
            {
                TowerBehaviour tower = FindTowerAtCell(cell);
                if (tower != null && tower is ISelectable s && s.IsSelectable)
                {
                    Select(s);
                    return;
                }
            }
        }

        // Clicked empty ground → reselect hero
        SelectHero();
    }

    /// <summary>
    /// Finds the TowerBehaviour at the given cell using an OverlapCircle on the cell center.
    /// </summary>
    private static TowerBehaviour FindTowerAtCell(Vector2Int cell)
    {
        Vector3 center = GridManager.Instance.CellToWorld(cell);
        Collider2D hit = Physics2D.OverlapCircle(center, GridManager.CellSize * 0.45f);
        if (hit != null && hit.TryGetComponent<TowerBehaviour>(out var tower))
            return tower;
        return null;
    }

    private void HandleCancelInput()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame ||
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            SelectHero();
        }
    }

    // ── Selection indicator (green ellipse) ──────────────────────────────────

    private void CreateIndicator()
    {
        if (_ellipseSprite == null)
            _ellipseSprite = CreateEllipseSprite(64);

        _indicator = new GameObject("SelectionIndicator");
        _indicator.transform.localScale = new Vector3(_scale.x, _scale.y, 1f);

        _indicatorSr                  = _indicator.AddComponent<SpriteRenderer>();
        _indicatorSr.sprite           = _ellipseSprite;
        _indicatorSr.color            = _color;
        _indicatorSr.sortingOrder     = -4500; // Above entity shadows, below entities

        _indicator.SetActive(false);
    }

    private static Sprite CreateEllipseSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float center = size * 0.5f;
        float radius = center;
        const float ringWidth = 0.15f; // proportion of radius used for the ring

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx   = x - center + 0.5f;
                float dy   = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / radius;

                // Ring: opaque near dist≈1, transparent at center and outside
                float outerFade = Mathf.Clamp01((1f - dist) / (ringWidth * 0.5f));
                float innerFade = Mathf.Clamp01((dist - (1f - ringWidth)) / (ringWidth * 0.5f));
                float alpha = outerFade * innerFade;

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
