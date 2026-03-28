using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders the 14x18 grid visuals.
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

    // Central path: cols 2–11 (10 wide), edge at 1 and 12, grass on 0 and 13
    // Public so GridManager, WaveManager, and HeroBehaviour can read them without hardcoding.
    public const int PathColMin = 2;
    public const int PathColMax = 11;

    private Sprite _spriteRestricted;
    private Sprite _spriteBlack;
    private Sprite _spriteOcupada;
    private Sprite _spriteBuilding;
    private Sprite _spriteInvalid;

    private Sprite _grassBase;
    private Sprite _pathBase;
    private Sprite _pathEdgeLeft;
    private Sprite _pathEdgeRight;
    private Sprite[] _pathVariants;

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

        LoadTileSprites();
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
        //BuildDecorations();
    }

    private void BuildBackground()
    {
        if (_grassBase == null) return;

        float scale = ComputeTileScale(_grassBase);
        const int extraCols = 8;

        for (int y = 0; y < GridManager.Rows; y++)
        {
            for (int x = -extraCols; x < 0; x++)
                SpawnBgTile(x, y, scale);

            for (int x = GridManager.Columns; x < GridManager.Columns + extraCols; x++)
                SpawnBgTile(x, y, scale);
        }
    }

    private void SpawnBgTile(int x, int y, float scale)
    {
        var worldPos = _grid.CellToWorld(new Vector2Int(x, y));
        var go = new GameObject($"BgTile_{x}_{y}");
        go.transform.SetParent(transform);
        go.transform.position   = worldPos;
        go.transform.localScale = new Vector3(scale, scale, 1f);

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = _grassBase;
        sr.sortingOrder = -20000;
    }

    private void BuildTileObjects()
    {
        _tiles = new SpriteRenderer[GridManager.Columns, GridManager.Rows];
        float scale = ComputeTileScale(_grassBase);

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
                sr.sortingOrder = -10000 + y;
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

    private void LoadTileSprites()
    {
        _grassBase     = Resources.Load<Sprite>("Decorations/grass_base");
        _pathBase      = Resources.Load<Sprite>("Decorations/path_base");
        _pathEdgeLeft  = Resources.Load<Sprite>("Decorations/path_edge_left");
        _pathEdgeRight = Resources.Load<Sprite>("Decorations/path_edge_right");

        _pathVariants = new Sprite[4];
        for (int i = 1; i <= 4; i++)
            _pathVariants[i - 1] = Resources.Load<Sprite>($"Decorations/rockPath_{i}");

        if (_grassBase == null)
            Debug.LogError("[GridVisualizer] grass_base sprite not found in Resources/Decorations/.");
    }

    private Sprite GetTileSprite(int col, int row)
    {
        bool isPath      = col >= PathColMin && col <= PathColMax;
        bool isLeftEdge  = col == PathColMin - 1;
        bool isRightEdge = col == PathColMax + 1;

        if (isPath)
        {
            bool variantsReady = _pathVariants != null && _pathVariants[0] != null;
            if (variantsReady)
            {
                Random.InitState(col * 1000 + row);
                int index = Random.Range(0, 4);
                Random.InitState(System.Environment.TickCount);
                return _pathVariants[index];
            }
            return _pathBase;
        }
        if (isLeftEdge)  return _pathEdgeLeft;
        if (isRightEdge) return _pathEdgeRight;
        return _grassBase;
    }

    private Sprite SpriteForCell(Vector2Int cell)
    {
        // Spawn row (top of grid) is restricted but should show normal path/grass tiles
        bool isSpawnRow = cell.y == GridManager.Rows - 1;
        if (_grid.IsRestricted(cell) && !HideRestrictedVisual(cell) && !isSpawnRow)
            return _spriteRestricted;

        return GetTileSprite(cell.x, cell.y);
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

        // Positions on the grid for each decoration (spread across 14×18 grid)
        var positions = new Vector2Int[]
        {
            new Vector2Int(2, 6),
            new Vector2Int(11, 12),
            new Vector2Int(1, 2),
            new Vector2Int(12, 9),
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

            // Scale: 16px sprite at 16 PPU = 1 world unit. Cell is CellSize units.
            float decoScale = GridManager.CellSize * 0.6f;
            go.transform.localScale = new Vector3(decoScale, decoScale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = decoSprites[i];
            sr.sortingOrder = -9000;

            _decorations[cell] = go;
        }
    }

    private IEnumerator FlashRoutine(Vector2Int cell, bool valid)
    {
        var sr         = _tiles[cell.x, cell.y];
        sr.color       = Color.white; // reset any tint left by an interrupted flash
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
