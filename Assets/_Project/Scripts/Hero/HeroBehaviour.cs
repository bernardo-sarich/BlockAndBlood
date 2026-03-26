using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the player's hero: WASD movement (flies freely, no collision),
/// automatic combat against the nearest enemy in range, and remote tower
/// construction via a build queue.
///
/// Build flow:
///   1. HUD selects a tower  → TowerPlacementManager.SelectTower()
///   2. Player clicks a cell → HeroBehaviour.QueueBuild(data, cell)
///   3. Hero auto-moves to the closest cell adjacent to the target
///   4. On arrival → TowerPlacementManager.RequestPlacement(cell) (5 s timer in TowerBehaviour)
///   5. Hero resumes normal WASD control; next queued build begins
///
/// Stats (Damage, AttackRange, AttackInterval, MoveSpeed, PrioritizeHighestHp)
/// are public properties so CardEffect can modify them at runtime.
/// </summary>
public class HeroBehaviour : MonoBehaviour, ISelectable
{
    public static HeroBehaviour Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float _baseMoveSpeed = 4f;

    [Header("Combat")]
    [SerializeField] private float _baseDamage        = 25f;
    [SerializeField] private float _baseAttackRange   = 1.5f * GridManager.CellSize;  // 1.5 cells
    [SerializeField] private float _baseAttackInterval = 0.667f; // 1.5 attacks/s
    [SerializeField] private LayerMask _enemyLayer;

    [Header("Build")]
    [SerializeField] private float _arrivalThreshold = 0.05f; // world units
    [SerializeField] private LayerMask _placementClickMask;   // optional — not required

    // ── Runtime stats (modified by cards) ────────────────────────────────────
    public float Damage           { get; set; }
    public float AttackRange      { get; set; }
    public float AttackInterval   { get; set; }
    public float MoveSpeed        { get; set; }
    /// <summary>When true, targets highest-HP enemy instead of nearest.</summary>
    public bool  PrioritizeHighestHp { get; set; }

    // ── ISelectable ─────────────────────────────────────────────────────────
    public Transform SelectionTransform => transform;
    public bool      IsSelectable       => true;

    // ── Internal state ────────────────────────────────────────────────────────
    private Camera         _cam;
    private GridVisualizer _visualizer;
    private float          _attackTimer;

    // Build queue: hero visits each target cell in order
    private readonly Queue<BuildOrder> _buildQueue = new Queue<BuildOrder>();
    private BuildOrder  _currentBuild;
    private bool        _isAutoMoving;
    private Vector3     _autoMoveTarget;

    // Pre-allocated overlap buffer — no GC alloc per frame
    private readonly Collider2D[] _hitBuffer = new Collider2D[32];

    private HeroAnimator _heroAnimator;

    // ── Nested types ──────────────────────────────────────────────────────────
    private struct BuildOrder
    {
        public TowerData  Data;
        public Vector2Int Cell;
        public Vector3    AdjacentWorldPos; // precomputed on enqueue
    }

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Initialise runtime stats from base values
        Damage         = _baseDamage;
        AttackRange    = _baseAttackRange;
        AttackInterval = _baseAttackInterval;
        MoveSpeed      = _baseMoveSpeed;

        // Ensure dynamic Y-sorting is present for perspective camera depth ordering
        if (GetComponent<DynamicYSorting>() == null)
            gameObject.AddComponent<DynamicYSorting>();
    }

    private void Start()
    {
        _cam          = Camera.main;
        _visualizer   = Object.FindFirstObjectByType<GridVisualizer>();
        _heroAnimator = GetComponent<HeroAnimator>();

        if (_enemyLayer.value == 0)
        {
            int idx = LayerMask.NameToLayer("Enemy");
            if (idx >= 0) _enemyLayer = 1 << idx;
        }
    }

    private void Update()
    {
        HandleBuildInput();
        HandleMovement();
        HandleCancelInput();
        HandleAutoMove();
        HandleCombat();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads left-click during placement mode. Queues a build order and clears
    /// the tower selection so the cursor returns to normal.
    /// Hero walks to the target cell; on arrival the 5s build timer starts.
    /// </summary>
    private void HandleBuildInput()
    {
        if (TowerPlacementManager.Instance == null) return;
        if (TowerPlacementManager.Instance.SelectedTower == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (_cam == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Ray ray = _cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
        float t = (ray.direction.z != 0f) ? -ray.origin.z / ray.direction.z : 0f;
        Vector3 world = ray.origin + ray.direction * t;
        world.z = 0f;
        Vector2Int cell = GridManager.Instance.WorldToCell(world);

        if (!GridManager.Instance.CanPlaceTower(cell))
        {
            _visualizer?.FlashTile(cell, false);
            Debug.Log($"[Hero] CanPlaceTower({cell}) = false");
            return;
        }

        Debug.Log($"[Hero] QueueBuild at {cell}, tower={TowerPlacementManager.Instance.SelectedTower.TowerName}");
        QueueBuild(TowerPlacementManager.Instance.SelectedTower, cell);
        TowerPlacementManager.Instance.CancelSelection();
    }

    private void HandleCancelInput()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
            TowerPlacementManager.Instance?.CancelSelection();
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    private void HandleMovement()
    {
        // WASD always applies; auto-move blends in when a build target is active.
        var kb = Keyboard.current;
        float h = 0f, v = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h = -1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h =  1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v = -1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v =  1f;

        // Always update animator (includes the idle → zero case)
        _heroAnimator?.SetMovement(new Vector2(h, v));

        if (h == 0f && v == 0f) return;

        Vector3 dir = new Vector3(h, v, 0f).normalized;
        transform.position += dir * (MoveSpeed * Time.deltaTime);
        ClampToScreen();

        // Manual input cancels auto-move steering (player has full control)
        _isAutoMoving = false;
    }

    /// <summary>
    /// When no WASD input is active and a build is queued, steer the hero
    /// toward the precomputed adjacent tile automatically.
    /// </summary>
    private void HandleAutoMove()
    {
        if (_currentBuild.Data == null) return;
        if (_isAutoMoving == false) return;

        Vector3 delta = _autoMoveTarget - transform.position;
        float   dist  = delta.magnitude;

        if (dist <= _arrivalThreshold)
        {
            // Arrived — execute the build
            transform.position = _autoMoveTarget;
            Debug.Log($"[Hero] Arrived at {_currentBuild.Cell}, calling RequestPlacement with {_currentBuild.Data?.TowerName}");
            TowerPlacementManager.Instance.RequestPlacement(_currentBuild.Cell, _currentBuild.Data);
            StartNextBuild();
            return;
        }

        transform.position += delta.normalized * (MoveSpeed * Time.deltaTime);
        ClampToScreen();
    }

    // ── Build queue ───────────────────────────────────────────────────────────

    /// <summary>
    /// Enqueues a build order. If the hero is idle, begins auto-moving immediately.
    /// </summary>
    public void QueueBuild(TowerData data, Vector2Int cell)
    {
        Vector3 adjacent = FindAdjacentWorldPos(cell);

        _buildQueue.Enqueue(new BuildOrder
        {
            Data             = data,
            Cell             = cell,
            AdjacentWorldPos = adjacent
        });

        if (_currentBuild.Data == null)
            StartNextBuild();
    }

    private void StartNextBuild()
    {
        _currentBuild = default;

        if (_buildQueue.Count == 0)
        {
            _isAutoMoving = false;
            return;
        }

        _currentBuild   = _buildQueue.Dequeue();
        _autoMoveTarget = _currentBuild.AdjacentWorldPos;
        _isAutoMoving   = true;
    }

    /// <summary>
    /// Finds the world-space center of the grid cell adjacent to <paramref name="targetCell"/>
    /// that is currently closest to the hero.
    /// Searches the 4 cardinal neighbors; falls back to the target cell itself if none are in bounds.
    /// </summary>
    private Vector3 FindAdjacentWorldPos(Vector2Int targetCell)
    {
        Vector2Int[] neighbors =
        {
            targetCell + Vector2Int.up,
            targetCell + Vector2Int.down,
            targetCell + Vector2Int.left,
            targetCell + Vector2Int.right,
        };

        Vector3 heroPos   = transform.position;
        Vector3 bestPos   = GridManager.Instance.CellToWorld(targetCell);
        float   bestDist  = float.MaxValue;
        bool    found     = false;

        foreach (var n in neighbors)
        {
            if (!GridManager.Instance.IsInBounds(n)) continue;

            Vector3 w    = GridManager.Instance.CellToWorld(n);
            float   dist = Vector3.SqrMagnitude(w - heroPos);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestPos  = w;
                found    = true;
            }
        }

        if (!found)
            Debug.LogWarning($"[HeroBehaviour] No valid adjacent cell found for {targetCell}, using cell center.");

        return bestPos;
    }

    // ── Combat ────────────────────────────────────────────────────────────────

    private void HandleCombat()
    {
        _attackTimer -= Time.deltaTime;
        if (_attackTimer > 0f) return;

        IDamageable target = FindTarget();
        if (target == null) return;

        target.TakeDamage(Damage, DamageType.Physical);
        _heroAnimator?.TriggerAttack();
        _attackTimer = AttackInterval;
    }

    private IDamageable FindTarget()
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position, AttackRange, _hitBuffer, _enemyLayer);

        if (count == 0) return null;

        IDamageable best     = null;
        float       bestVal  = float.MinValue;

        for (int i = 0; i < count; i++)
        {
            if (!_hitBuffer[i].TryGetComponent<IDamageable>(out var candidate)) continue;
            if (!candidate.IsAlive) continue;

            float val = PrioritizeHighestHp
                ? GetCurrentHp(candidate)                          // Instinto cazador card
                : -Vector3.SqrMagnitude(candidate.Position - transform.position); // nearest

            if (val > bestVal)
            {
                bestVal = val;
                best    = candidate;
            }
        }

        return best;
    }

    /// <summary>
    /// Returns current HP for Instinto cazador targeting.
    /// Falls back to 0 if the concrete type doesn't expose HP directly —
    /// EnemyBehaviour should implement this via a separate interface or cast.
    /// </summary>
    private static float GetCurrentHp(IDamageable target)
    {
        // EnemyBehaviour will implement IHasHp in TASK-03; cast when available.
        if (target is IHasHp hpProvider)
            return hpProvider.CurrentHp;
        return 0f;
    }

    // ── Screen bounds ─────────────────────────────────────────────────────────

    private void ClampToScreen()
    {
        // Clamp to grid world bounds derived from GridManager origin.
        if (GridManager.Instance == null) return;
        Vector3 origin = GridManager.Instance.GridOrigin;
        float gridWidth  = GridManager.Columns * GridManager.CellSize;
        float gridHeight = GridManager.Rows    * GridManager.CellSize;

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, origin.x, origin.x + gridWidth);
        p.y = Mathf.Clamp(p.y, origin.y, origin.y + gridHeight);
        transform.position = p;
    }

    // ── Editor gizmos ─────────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Attack range
        UnityEditor.Handles.color = new Color(1f, 0.8f, 0f, 0.4f);
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, AttackRange);

        // Auto-move target
        if (_isAutoMoving)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, _autoMoveTarget);
            Gizmos.DrawSphere(_autoMoveTarget, 0.1f);
        }
    }
#endif
}
