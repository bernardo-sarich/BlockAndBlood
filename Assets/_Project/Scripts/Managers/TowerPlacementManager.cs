using UnityEngine;

/// <summary>
/// Orchestrates tower placement: tracks which tower the player has selected,
/// validates placement on click, spends gold, and instantiates the tower.
/// HeroBehaviour calls SelectTower() / RequestPlacement() to drive placement.
/// Right-click or Escape cancels current selection.
/// </summary>
public class TowerPlacementManager : MonoBehaviour
{
    public static TowerPlacementManager Instance { get; private set; }

    public static event System.Action<TowerData>      OnTowerSelected;
    public static event System.Action                 OnPlacementCancelled;
    public static event System.Action<TowerBehaviour> OnTowerPlaced;

    public TowerData SelectedTower { get; private set; }

    private Camera         _cam;
    private GridVisualizer _visualizer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _cam        = Camera.main;
        _visualizer = Object.FindFirstObjectByType<GridVisualizer>();
    }

    // ── Public API (called by UI buttons and HeroBehaviour) ──────────────────

    public void SelectTower(TowerData data)
    {
        SelectedTower = data;
        OnTowerSelected?.Invoke(data);
    }

    /// <summary>Clears the current selection without placing anything.</summary>
    public void CancelSelection()
    {
        SelectedTower = null;
        OnPlacementCancelled?.Invoke();
    }

    /// <summary>
    /// Attempts to place <paramref name="data"/> at <paramref name="cell"/>.
    /// Validates placement, spends gold, and instantiates + initialises the tower.
    /// </summary>
    public void RequestPlacement(Vector2Int cell, TowerData data)
    {
        if (data == null) return;

        bool valid = GridManager.Instance.CanPlaceTower(cell);
        _visualizer?.FlashTile(cell, valid);
        if (!valid) return;

        if (!EconomyManager.Instance.TrySpend(data.Cost))
        {
            Debug.Log("[TowerPlacementManager] Not enough gold.");
            return;
        }

        GameObject prefab = data.TowerPrefab;
        if (prefab == null)
        {
            Debug.LogError("[TowerPlacementManager] TowerData.TowerPrefab is not assigned.");
            EconomyManager.Instance.Add(data.Cost);
            return;
        }

        _visualizer?.RemoveDecoration(cell);

        Vector3 pos = GridManager.Instance.CellToWorld(cell);
        pos.z = 0f;

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        if (!go.TryGetComponent<TowerBehaviour>(out var tower))
        {
            Debug.LogError("[TowerPlacementManager] TowerPrefab missing TowerBehaviour.");
            EconomyManager.Instance.Add(data.Cost);
            Destroy(go);
            return;
        }

        tower.Initialize(data, cell);
        Debug.Log($"[TowerPlacementManager] Tower '{data.TowerName}' placed at {cell}, pos={pos}");
        OnTowerPlaced?.Invoke(tower);
    }
}
