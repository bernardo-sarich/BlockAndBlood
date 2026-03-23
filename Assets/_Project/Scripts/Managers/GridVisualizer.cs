using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders the 7x9 grid visuals.
///
/// Ground layer: a single large grass sprite covers the entire grid area (no tiling seams).
/// Tile layer: only non-libre cells (restricted) get individual tile sprites.
/// Libre cells are transparent — the grass ground shows through.
///
/// FlashTile still works: it temporarily assigns a sprite + tint to any cell's SpriteRenderer.
/// </summary>
[UnityEngine.ExecuteAlways]
public class GridVisualizer : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] private float _flashDuration = 0.5f;

    [Header("Restricted cells that should display as Libre")]
    [SerializeField] private Vector2Int[] _hideRestrictedVisual = new Vector2Int[0];

    private Sprite _spriteRestricted;
    private Sprite _spriteGrass;
    private Sprite _spriteBlack;
    private Sprite _spriteOcupada;
    private Sprite _spriteBuilding;
    private Sprite _spriteInvalid;
    private SpriteRenderer[,] _tiles;
    private Dictionary<Vector2Int, GameObject> _decorations = new Dictionary<Vector2Int, GameObject>();
    private GridManager _grid;

    private void Awake()
    {
        _spriteRestricted = Resources.Load<Sprite>("Grid/Tile_Restricted");
        _spriteBlack      = Resources.Load<Sprite>("Grid/Tile_Black");
        _spriteOcupada    = Resources.Load<Sprite>("Grid/Tile_Ocupada");
        _spriteBuilding   = Resources.Load<Sprite>("Grid/Tile_Building");
        _spriteInvalid    = Resources.Load<Sprite>("Grid/Tile_Invalid");

        // Load grass tile from GRASS+ sprite sheet
        var grassSprites = Resources.LoadAll<Sprite>("Decorations/GRASS+");
        foreach (var s in grassSprites)
        {
            if (s.name == "GRASS+_58") { _spriteGrass = s; break; }
        }

        if (_spriteGrass == null)
            Debug.LogError("[GridVisualizer] GRASS+_32 sprite not found in Resources/Decorations/GRASS+.");
    }

    private void Start()
    {
        _grid = GridManager.Instance != null
            ? GridManager.Instance
            : FindFirstObjectByType<GridManager>();

        if (_grid == null) return;

        // Destroy existing tile children to avoid duplicates on editor reloads
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }

        BuildBackground();
        BuildTileObjects();
        BuildDecorations();
    }

    private void BuildBackground()
    {
        Sprite bgSprite = _spriteBlack != null ? _spriteBlack : _spriteGrass;
        if (bgSprite == null) return;

        var bg = new GameObject("Background");
        bg.transform.SetParent(transform);
        bg.transform.position   = new Vector3(2.4f, 4.32f, 0f);
        float bgScale = ComputeTileScale(bgSprite) * 30f;
        bg.transform.localScale = new Vector3(bgScale, bgScale, 1f);

        var sr          = bg.AddComponent<SpriteRenderer>();
        sr.sprite       = bgSprite;
        sr.color        = _spriteBlack != null ? Color.white : Color.black;
        sr.sortingOrder = -1000;
    }

    private void BuildTileObjects()
    {
        _tiles = new SpriteRenderer[GridManager.Columns, GridManager.Rows];
        float scale = ComputeTileScale(_spriteGrass);

        for (int x = 0; x < GridManager.Columns; x++)
        {
            for (int y = 0; y < GridManager.Rows; y++)
            {
                var cell     = new Vector2Int(x, y);
                var worldPos = _grid.CellToWorld(cell);

                var go = new GameObject($"Tile_{x}_{y}");
                go.transform.SetParent(transform);
                go.transform.position   = worldPos;
                go.transform.localScale = new Vector3(scale, scale, 1f);

                var sr          = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = -100 - y;
                sr.sprite       = SpriteForCell(cell); // null for libre cells
                sr.color        = Color.white;
                _tiles[x, y]    = sr;
            }
        }
    }

    // ─── Public API ────────────────────────────────────────────────────────────

    /// <summary>Refreshes a tile to match its current state.</summary>
    public void RefreshTile(Vector2Int cell)
    {
        if (_tiles == null || !_grid.IsInBounds(cell)) return;
        var sr    = _tiles[cell.x, cell.y];
        sr.sprite = SpriteForCell(cell);
        sr.color  = Color.white;
    }

    /// <summary>Removes the decoration sprite on this cell, if any.</summary>
    public void RemoveDecoration(Vector2Int cell)
    {
        if (_decorations.TryGetValue(cell, out var go))
        {
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
            _decorations.Remove(cell);
        }
    }

    /// <summary>Flashes a tile: green tint = valid, red + X = invalid.</summary>
    public void FlashTile(Vector2Int cell, bool valid)
    {
        if (_tiles == null || !_grid.IsInBounds(cell)) return;
        StopAllCoroutines();
        StartCoroutine(FlashRoutine(cell, valid));
    }

    /// <summary>Refreshes all tiles.</summary>
    public void RefreshAll()
    {
        for (int x = 0; x < GridManager.Columns; x++)
            for (int y = 0; y < GridManager.Rows; y++)
                RefreshTile(new Vector2Int(x, y));
    }

    [ContextMenu("Refresh Tiles")]
    public void ForceRefresh()
    {
        if (_grid == null)
            _grid = FindFirstObjectByType<GridManager>();
        if (_grid == null || _tiles == null) return;
        RefreshAll();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            ForceRefresh();
        };
#endif
    }

    // ─── Private ───────────────────────────────────────────────────────────────

    private bool HideRestrictedVisual(Vector2Int cell)
    {
        foreach (var c in _hideRestrictedVisual)
            if (c == cell) return true;
        return false;
    }

    private Sprite SpriteForCell(Vector2Int cell)
    {
        if (_grid.IsRestricted(cell) && !HideRestrictedVisual(cell))
            return _spriteRestricted;

        return _spriteGrass;
    }

    /// <summary>
    /// Places decorative sprites (broken trunks) at random grid positions
    /// to add visual variety to the grass ground.
    /// </summary>
    private void BuildDecorations()
    {
        // Load all sub-sprites from the GRASS+ sheet in Resources
        var allSprites = Resources.LoadAll<Sprite>("Decorations/GRASS+");
        if (allSprites == null || allSprites.Length == 0) return;

        // Find decoration sprites by name
        string[] decoNames = { "GRASS+_310", "GRASS+_311", "GRASS+_291", "GRASS+_317" };
        var decoSprites = new Sprite[decoNames.Length];
        foreach (var s in allSprites)
        {
            for (int i = 0; i < decoNames.Length; i++)
                if (s.name == decoNames[i]) decoSprites[i] = s;
        }

        // Positions on the grid for each decoration
        var positions = new Vector2Int[]
        {
            new Vector2Int(1, 3),
            new Vector2Int(5, 6),
            new Vector2Int(3, 1),
            new Vector2Int(4, 5),
        };

        for (int i = 0; i < positions.Length; i++)
        {
            if (decoSprites[i] == null) continue;
            var cell = positions[i];
            if (_grid.IsRestricted(cell)) continue;

            var worldPos = _grid.CellToWorld(cell);
            var go = new GameObject($"Deco_{i}");
            go.transform.SetParent(transform);
            go.transform.position = worldPos;

            // Scale: 16px sprite at 16 PPU = 1 world unit. Cell is 0.96 units.
            float decoScale = GridManager.CellSize * 0.6f;
            go.transform.localScale = new Vector3(decoScale, decoScale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = decoSprites[i];
            sr.sortingOrder = -90;

            _decorations[cell] = go;
        }
    }

    private IEnumerator FlashRoutine(Vector2Int cell, bool valid)
    {
        var sr         = _tiles[cell.x, cell.y];
        var origSprite = sr.sprite;
        var origColor  = sr.color;
        float half     = _flashDuration / 6f; // 3 flashes

        if (!valid) sr.sprite = _spriteInvalid;
        Color flash = valid ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.25f, 0.25f);

        for (int i = 0; i < 3; i++)
        {
            sr.color = flash;
            yield return new WaitForSeconds(half);
            sr.color = origColor;
            yield return new WaitForSeconds(half);
        }

        sr.sprite = origSprite;
    }

    // Uniform scale: tile fills CellSize world units + 2% overlap to prevent micro-gaps.
    private float ComputeTileScale(Sprite s)
    {
        if (s == null) return 1f;
        return GridManager.CellSize * s.pixelsPerUnit / s.rect.width * 1.02f;
    }
}
