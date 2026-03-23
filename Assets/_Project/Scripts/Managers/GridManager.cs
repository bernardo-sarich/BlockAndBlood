using UnityEngine;
using Pathfinding;

/// <summary>
/// Manages the 5x9 game grid and validates tower placement via A* Pathfinding.
/// Spawn row: 0 (top). Goal row: 8 (bottom).
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    public enum CellState { Libre, EnConstruccion, Ocupada }

    /// <summary>Cells that can never have a tower placed on them (enemy spawn row at top).</summary>
    public static readonly Vector2Int[] RestrictedCells = new Vector2Int[]
    {
        new Vector2Int(0, SpawnRow),
        new Vector2Int(1, SpawnRow),
        new Vector2Int(2, SpawnRow),
        new Vector2Int(3, SpawnRow),
        new Vector2Int(4, SpawnRow),
        new Vector2Int(5, SpawnRow),
        new Vector2Int(6, SpawnRow),
    };

    public const int Columns = 7;
    public const int Rows = 9;
    public const float CellSize = 0.96f;

    /// <summary>Enemies enter from top (row 8) and walk down to bottom (row 0).</summary>
    public const int SpawnRow = 8;
    public const int GoalRow  = 0;

    [Header("Grid Origin (bottom-left corner)")]
    [SerializeField] private Vector3 _gridOrigin = Vector3.zero;

    private CellState[,] _grid = new CellState[Columns, Rows];

    public Vector2Int SpawnCell => new Vector2Int(Columns / 2, SpawnRow);
    public Vector2Int GoalCell  => new Vector2Int(Columns / 2, GoalRow);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ClearGrid();
        ConfigureAstarGraph();
    }

    /// <summary>
    /// Ensures the AstarPath GridGraph matches the game grid dimensions.
    /// Safe to call multiple times — only reconfigures if needed.
    /// </summary>
    private void ConfigureAstarGraph()
    {
        if (AstarPath.active == null)
        {
            Debug.LogError("[GridManager] No AstarPath component found in scene!");
            return;
        }

        GridGraph graph = AstarPath.active.data.gridGraph;
        if (graph == null)
        {
            graph = AstarPath.active.data.AddGraph(typeof(GridGraph)) as GridGraph;
        }

        // Grid center: offset so cell (0,0) aligns with _gridOrigin
        Vector3 center = _gridOrigin + new Vector3(
            Columns * CellSize * 0.5f,
            Rows    * CellSize * 0.5f,
            0f);

        graph.SetDimensions(Columns, Rows, CellSize);
        graph.center    = center;
        graph.is2D      = true;
        graph.cutCorners = false; // Prevent diagonal paths between adjacent towers

        // Disable obstacle/height checks — we control walkability manually via cell state
        graph.collision.use2D             = true;
        graph.collision.mask              = 0;         // no collision layers
        graph.collision.heightMask        = 0;         // no height layers
        graph.collision.heightCheck       = false;

        AstarPath.active.Scan();

        // Diagnostic: verify graph has walkable nodes
        int walkable = 0;
        graph.GetNodes(node => { if (node.Walkable) walkable++; });
        Debug.Log($"[GridManager] A* graph scanned: {graph.CountNodes()} nodes, {walkable} walkable, center={center}");
    }

    private void ClearGrid()
    {
        for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
                _grid[x, y] = CellState.Libre;
    }

    // ─── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if a tower can be placed at the given cell.
    /// Validates bounds, state, restricted rows, and that a spawn→goal path still exists.
    /// </summary>
    public bool CanPlaceTower(Vector2Int cell)
    {
        if (!IsInBounds(cell)) { Debug.Log($"[Grid] {cell} rejected: out of bounds"); return false; }
        if (_grid[cell.x, cell.y] != CellState.Libre) { Debug.Log($"[Grid] {cell} rejected: state={_grid[cell.x, cell.y]}"); return false; }
        if (cell == SpawnCell || cell == GoalCell) { Debug.Log($"[Grid] {cell} rejected: spawn/goal"); return false; }
        if (IsRestricted(cell)) { Debug.Log($"[Grid] {cell} rejected: restricted"); return false; }
        bool pathOk = PathExistsWithCellBlocked(cell);
        if (!pathOk) Debug.Log($"[Grid] {cell} rejected: path blocked");
        return pathOk;
    }

    public bool IsRestricted(Vector2Int cell)
    {
        foreach (var r in RestrictedCells)
            if (r == cell) return true;
        return false;
    }

    /// <summary>Updates a cell state and notifies pathfinding when the graph changes.</summary>
    public void SetCellState(Vector2Int cell, CellState state)
    {
        if (!IsInBounds(cell)) return;
        _grid[cell.x, cell.y] = state;
        NotifyPathfindingSystem(cell);
    }

    public CellState GetCellState(Vector2Int cell) =>
        IsInBounds(cell) ? _grid[cell.x, cell.y] : CellState.Ocupada;

    /// <summary>Converts a grid cell to its world-space center.</summary>
    public Vector3 CellToWorld(Vector2Int cell)
    {
        return _gridOrigin + new Vector3(
            cell.x * CellSize + CellSize * 0.5f,
            cell.y * CellSize + CellSize * 0.5f,
            0f);
    }

    /// <summary>Converts a world position to the nearest grid cell.</summary>
    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector3 local = worldPos - _gridOrigin;
        return new Vector2Int(
            Mathf.FloorToInt(local.x / CellSize),
            Mathf.FloorToInt(local.y / CellSize));
    }

    // ─── Pathfinding ───────────────────────────────────────────────────────────

    /// <summary>Sets the A* node walkability to match the cell state and forces agents to repath.</summary>
    public void NotifyPathfindingSystem(Vector2Int cell)
    {
        if (AstarPath.active == null) return;

        GraphNode node = GetNodeAt(cell);
        if (node == null) return;

        // Only free cells are walkable — both building and occupied towers block enemies
        bool walkable = _grid[cell.x, cell.y] == CellState.Libre;

        // Direct node set — guaranteed correct regardless of GUO bounds precision
        node.Walkable = walkable;

        // GUO updates the graph and triggers internal bookkeeping (flood fill, etc.)
        var bounds = new Bounds(CellToWorld(cell), new Vector3(CellSize, CellSize, 1f));
        var guo = new Pathfinding.GraphUpdateObject(bounds);
        guo.modifyWalkability = true;
        guo.setWalkability    = walkable;
        guo.updatePhysics     = false;
        AstarPath.active.UpdateGraphs(guo);
        AstarPath.active.FlushGraphUpdates();

        // Force all active enemies to recalculate paths synchronously.
        // Without this, enemies follow their stale path (which may cross
        // the just-blocked cell) until the async repath completes.
        ForceAllEnemiesRepath();

        Debug.Log($"[GridManager] Node ({cell.x},{cell.y}) walkable={walkable}, verified={node.Walkable}");
    }

    /// <summary>
    /// Recalculates paths for every active AIPath agent synchronously.
    /// Cheap on a 7×9 grid (~63 nodes) even with 30+ enemies.
    /// </summary>
    private void ForceAllEnemiesRepath()
    {
        var agents = Object.FindObjectsByType<AIPath>(FindObjectsSortMode.None);
        foreach (var ai in agents)
        {
            if (ai == null || !ai.enabled || !ai.hasPath) continue;

            var path = ABPath.Construct(ai.position, ai.destination, null);
            AstarPath.StartPath(path);
            AstarPath.BlockUntilCalculated(path);
            ai.SetPath(path);
        }
    }

    private bool PathExistsWithCellBlocked(Vector2Int cell)
    {
        if (AstarPath.active == null)
        {
            Debug.LogWarning("[GridManager] AstarPath not found in scene — allowing placement.");
            return true;
        }

        GraphNode node = GetNodeAt(cell);
        if (node == null) return true;

        // Temporarily block the node and test synchronously
        bool wasWalkable = node.Walkable;
        node.Walkable = false;
        AstarPath.active.FlushGraphUpdates();

        ABPath path = ABPath.Construct(CellToWorld(SpawnCell), CellToWorld(GoalCell), null);
        AstarPath.StartPath(path);
        AstarPath.BlockUntilCalculated(path);
        bool pathExists = !path.error;

        // Restore node state
        node.Walkable = wasWalkable;
        AstarPath.active.FlushGraphUpdates();

        return pathExists;
    }

    private GraphNode GetNodeAt(Vector2Int cell)
    {
        GridGraph graph = AstarPath.active?.data.gridGraph;
        return graph?.GetNode(cell.x, cell.y);
    }

    public bool IsInBounds(Vector2Int cell) =>
        cell.x >= 0 && cell.x < Columns && cell.y >= 0 && cell.y < Rows;

    // ─── Editor Gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                Vector3 center = _gridOrigin + new Vector3(
                    x * CellSize + CellSize * 0.5f,
                    y * CellSize + CellSize * 0.5f,
                    0f);

                CellState state = Application.isPlaying ? _grid[x, y] : CellState.Libre;
                Gizmos.color = state switch
                {
                    CellState.Libre          => new Color(1f, 1f, 1f, 0.1f),
                    CellState.EnConstruccion => new Color(1f, 0.9f, 0f, 0.3f),
                    CellState.Ocupada        => new Color(0.3f, 0.3f, 0.3f, 0.4f),
                    _                        => Color.white
                };
                Gizmos.DrawCube(center, new Vector3(CellSize * 0.95f, CellSize * 0.95f, 0.01f));

                Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.4f);
                Gizmos.DrawWireCube(center, new Vector3(CellSize, CellSize, 0.01f));
            }
        }
    }
#endif
}
