using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Controls a placed tower: build timer, targeting, attacking, upgrading, and selling.
///
/// Lifecycle: TowerPlacementManager instantiates the prefab → calls Initialize(data, cell)
/// → 5-second build timer → Active state → Sell() or game ends.
///
/// Targeting uses Physics2D.OverlapCircleNonAlloc with a pre-allocated buffer (no GC alloc).
/// Enemies must be in the layer defined by _enemyLayer (Inspector) or an "Enemy" layer.
/// </summary>
public class TowerBehaviour : MonoBehaviour, ISelectable
{
    private enum TowerState { Building, Active, Sold }

    [SerializeField] private LayerMask _enemyLayer;

    [Header("Shoot Animation (Range towers)")]
    [SerializeField] private Sprite[] _shootFramesDown;
    [SerializeField] private Sprite[] _shootFramesUp;
    [SerializeField] private Sprite[] _shootFramesRight;
    [SerializeField] private Sprite[] _shootFramesLeft;
    [SerializeField] private float    _shootAnimFps = 10f;

    private TowerData  _data;
    private Vector2Int _cell;
    private TowerState _state;
    private int        _totalGoldInvested;
    private float      _attackTimer;
    private float      _meleeEffectTimer;
    private bool       _isShooting;

    // Melee re-applies aura effects every MeleeEffectInterval to keep EffectSystem timer alive.
    private const float MeleeEffectInterval = 0.15f;

    private GridVisualizer _visualizer;
    private ObjectPool<ProjectileBehaviour> _projectilePool;
    private SpriteRenderer _spriteRenderer;
    private GameObject _buildingVisual;

    // Pre-allocated overlap buffer — avoids per-frame GC alloc.
    private readonly Collider2D[] _hitBuffer = new Collider2D[20];

    // ── Card effects ────────────────────────────────────────────────────────
    public const int MaxAppliedCards = 6;
    private readonly List<CardData> _appliedEffects = new List<CardData>(MaxAppliedCards);
    public IReadOnlyList<CardData> AppliedEffects => _appliedEffects;

    // Merged values rebuilt whenever _data or _appliedEffects changes.
    private EffectData[] _effectiveOnHitEffects;
    private float        _effectiveDamageBase;   // physical — subject to armor
    private float        _effectiveBonusDamage;  // fire (card bonus) — ignores armor

    // ── Events ───────────────────────────────────────────────────────────────
    public static event System.Action<TowerBehaviour>      OnTowerBuilt;
    public static event System.Action<TowerBehaviour, int> OnTowerSold;
    public static event System.Action<TowerBehaviour>      OnTowerUpgraded;
    public static event System.Action<TowerBehaviour>      OnTowerClicked;
    public static event System.Action<TowerBehaviour>      OnEffectApplied;

    // ── Properties ───────────────────────────────────────────────────────────
    public TowerData Data              => _data;
    public int       TotalGoldInvested => _totalGoldInvested;
    public int       SellValue         => Mathf.RoundToInt(_totalGoldInvested * 0.6f);
    public bool      CanUpgrade        => _data.UpgradePaths != null && _data.UpgradePaths.Length > 0;
    public bool      IsBuilding        => _state == TowerState.Building;

    // ── ISelectable ─────────────────────────────────────────────────────────
    public Transform SelectionTransform => transform;
    public bool      IsSelectable       => _state == TowerState.Active;

    // ── Initialisation ───────────────────────────────────────────────────────

    /// <summary>Starts the 5-second build timer. Call once, immediately after Instantiate.</summary>
    public void Initialize(TowerData data, Vector2Int cell)
    {
        _data              = data;
        _cell              = cell;
        _totalGoldInvested = data.Cost;
        _state             = TowerState.Building;

        _visualizer     = Object.FindFirstObjectByType<GridVisualizer>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (!data.IsAreaAttack && data.ProjectilePrefab != null)
            BuildProjectilePool(data.ProjectilePrefab);

        if (_enemyLayer.value == 0)
        {
            int idx = LayerMask.NameToLayer("Enemy");
            if (idx >= 0) _enemyLayer = 1 << idx;
        }

        // Hide actual tower sprite during construction
        if (_spriteRenderer != null) _spriteRenderer.enabled = false;

        // Disable spin during construction
        var spin = GetComponent<SpinForever>();
        if (spin != null) spin.enabled = false;

        // Show "under construction" visual
        ShowBuildingVisual();

        RebuildEffectiveEffects();

        GridManager.Instance.SetCellState(cell, GridManager.CellState.EnConstruccion);
        _visualizer?.RefreshTile(cell);
        StartCoroutine(BuildTimer());
    }

    private static Sprite _buildingSprite;
    private void ShowBuildingVisual()
    {
        if (_buildingSprite == null)
            _buildingSprite = Resources.Load<Sprite>("Grid/Tower_Building");

        if (_buildingSprite == null)
        {
            Debug.LogWarning("[TowerBehaviour] Missing Resources/Grid/Tower_Building sprite.");
            return;
        }

        _buildingVisual = new GameObject("BuildingVisual");
        _buildingVisual.transform.SetParent(transform, false);
        _buildingVisual.transform.localPosition = Vector3.zero;

        // Scale so the building visual fills ~80% of a cell regardless of parent scale
        float spriteUnits = _buildingSprite.rect.width / _buildingSprite.pixelsPerUnit;
        float desiredSize = GridManager.CellSize * 0.8f;
        float worldScale  = desiredSize / spriteUnits;
        Vector3 ps = transform.localScale;
        _buildingVisual.transform.localScale = new Vector3(
            worldScale / ps.x,
            worldScale / ps.y,
            1f);

        var sr          = _buildingVisual.AddComponent<SpriteRenderer>();
        sr.sprite       = _buildingSprite;
        // Y-based sorting for perspective camera depth ordering
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
    }

    private IEnumerator BuildTimer()
    {
        yield return new WaitForSeconds(2.5f);
        if (_state == TowerState.Sold) yield break;
        _state = TowerState.Active;

        // Remove construction visual, show real tower
        if (_buildingVisual != null) Destroy(_buildingVisual);
        if (_spriteRenderer != null)
        {
            _spriteRenderer.enabled = true;
            // Static Y-based sorting for perspective camera depth ordering
            _spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
        }

        // Re-enable spin
        var spin = GetComponent<SpinForever>();
        if (spin != null) spin.enabled = true;

        GridManager.Instance.SetCellState(_cell, GridManager.CellState.Ocupada);
        _visualizer?.RefreshTile(_cell);
        OnTowerBuilt?.Invoke(this);
    }

    // ── Card effects ────────────────────────────────────────────────────────

    /// <summary>Returns true if this tower can accept the given card.</summary>
    public bool CanApplyCard(CardData card)
    {
        if (card == null) return false;
        if (_state != TowerState.Active) return false;
        if (_appliedEffects.Count >= MaxAppliedCards) return false;
        return card.IsCompatibleWith(_data.Type);
    }

    /// <summary>Permanently applies a card effect to this tower.</summary>
    public void ApplyCard(CardData card)
    {
        if (!CanApplyCard(card)) return;
        _appliedEffects.Add(card);
        RebuildEffectiveEffects();
        OnEffectApplied?.Invoke(this);
    }

    /// <summary>
    /// Rebuilds _effectiveOnHitEffects by merging TowerData base effects with all applied card effects.
    /// Call whenever _data or _appliedEffects changes.
    /// </summary>
    private void RebuildEffectiveEffects()
    {
        // Physical damage — goes through armor calculation
        _effectiveDamageBase  = _data.DamageBase;

        // Card bonus damage — applied as Fire (ignores armor)
        _effectiveBonusDamage = 0f;
        for (int i = 0; i < _appliedEffects.Count; i++)
            _effectiveBonusDamage += _appliedEffects[i].BonusDamage;

        // On-hit effects = tower base effects + all card effects merged
        int baseCount = _data.OnHitEffects?.Length ?? 0;
        int cardCount = 0;
        for (int i = 0; i < _appliedEffects.Count; i++)
            cardCount += _appliedEffects[i].OnHitEffects?.Length ?? 0;

        _effectiveOnHitEffects = new EffectData[baseCount + cardCount];
        int idx = 0;

        if (_data.OnHitEffects != null)
            for (int i = 0; i < _data.OnHitEffects.Length; i++)
                _effectiveOnHitEffects[idx++] = _data.OnHitEffects[i];

        for (int c = 0; c < _appliedEffects.Count; c++)
        {
            if (_appliedEffects[c].OnHitEffects == null) continue;
            for (int e = 0; e < _appliedEffects[c].OnHitEffects.Length; e++)
                _effectiveOnHitEffects[idx++] = _appliedEffects[c].OnHitEffects[e];
        }
    }

    // ── Upgrade / Sell ───────────────────────────────────────────────────────

    /// <summary>
    /// Upgrades to UpgradePaths[pathIndex], spending the upgrade cost.
    /// Returns false if upgrade unavailable or insufficient gold.
    /// </summary>
    public bool TryUpgrade(int pathIndex = 0)
    {
        if (_state != TowerState.Active)                              return false;
        if (_data.UpgradePaths == null)                              return false;
        if (pathIndex < 0 || pathIndex >= _data.UpgradePaths.Length) return false;

        TowerData next = _data.UpgradePaths[pathIndex];
        if (!EconomyManager.Instance.TrySpend(next.Cost))            return false;

        _totalGoldInvested += next.Cost;
        _data               = next;
        RebuildEffectiveEffects();

        _projectilePool?.Clear();
        _projectilePool = null;
        if (!_data.IsAreaAttack && _data.ProjectilePrefab != null)
            BuildProjectilePool(_data.ProjectilePrefab);

        OnTowerUpgraded?.Invoke(this);
        return true;
    }

    /// <summary>Sells the tower, refunds 60 % total cost, frees the grid cell.</summary>
    public void Sell()
    {
        if (_state == TowerState.Sold) return;
        _state = TowerState.Sold;

        EconomyManager.Instance.Add(SellValue);
        GridManager.Instance.SetCellState(_cell, GridManager.CellState.Libre);
        _visualizer?.RefreshTile(_cell);

        OnTowerSold?.Invoke(this, SellValue);
        _projectilePool?.Clear();
        Destroy(gameObject);
    }

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Update()
    {
        if (_state != TowerState.Active) return;

        float dt    = Time.deltaTime;
        float range = _data.Range * GridManager.CellSize;

        if (_data.IsAreaAttack)
        {
            // ── Melee: continuous AoE damage ─────────────────────────────────
            int count = Physics2D.OverlapCircleNonAlloc(
                transform.position, range, _hitBuffer, _enemyLayer);

            float dmg      = _effectiveDamageBase * dt;
            float bonusDmg = _effectiveBonusDamage * dt;
            for (int i = 0; i < count; i++)
            {
                if (_hitBuffer[i].TryGetComponent<IDamageable>(out var t) && t.IsAlive)
                {
                    t.TakeDamage(dmg, _data.DamageType);
                    if (bonusDmg > 0f) t.TakeDamage(bonusDmg, DamageType.Fire);
                }
            }

            // Re-apply aura effects at fixed interval
            _meleeEffectTimer -= dt;
            if (_meleeEffectTimer <= 0f && _effectiveOnHitEffects != null && _effectiveOnHitEffects.Length > 0)
            {
                _meleeEffectTimer = MeleeEffectInterval;
                for (int i = 0; i < count; i++)
                {
                    if (_hitBuffer[i].TryGetComponent<IDamageable>(out var t) && t.IsAlive)
                        for (int e = 0; e < _effectiveOnHitEffects.Length; e++)
                            t.ApplyEffect(_effectiveOnHitEffects[e]);
                }
            }
        }
        else
        {
            // ── Ranged: single-target projectile ─────────────────────────────
            _attackTimer -= dt;
            if (_attackTimer > 0f || _isShooting) return;

            int count = Physics2D.OverlapCircleNonAlloc(
                transform.position, range, _hitBuffer, _enemyLayer);

            IDamageable best     = null;
            float       bestProg = float.MinValue;

            for (int i = 0; i < count; i++)
            {
                if (!_hitBuffer[i].TryGetComponent<IDamageable>(out var c)) continue;
                if (!c.IsAlive) continue;
                float p = c.GetPathProgress();
                if (p > bestProg) { bestProg = p; best = c; }
            }

            if (best == null) return;

            _isShooting = true;
            StartCoroutine(ShootRoutine(best));
        }
    }

    private Sprite[] GetShootFrames(Vector3 targetPos)
    {
        Vector2 delta = targetPos - transform.position;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x > 0f ? _shootFramesRight : _shootFramesLeft;
        return delta.y > 0f ? _shootFramesUp : _shootFramesDown;
    }

    private IEnumerator ShootRoutine(IDamageable target)
    {
        Sprite[] frames = target != null ? GetShootFrames(target.Position) : _shootFramesDown;

        if (frames != null && frames.Length > 0 && _shootAnimFps > 0f)
        {
            float frameTime = 1f / _shootAnimFps;
            for (int i = 0; i < frames.Length; i++)
            {
                if (_spriteRenderer != null && frames[i] != null)
                    _spriteRenderer.sprite = frames[i];
                yield return new WaitForSeconds(frameTime);
            }
            // Restore idle: frame 0 of the direction just used
            if (_spriteRenderer != null && frames[0] != null)
                _spriteRenderer.sprite = frames[0];
        }

        if (target != null && target.IsAlive && _projectilePool != null)
        {
            ProjectileBehaviour proj = _projectilePool.Get();
            proj.transform.position  = GetSpawnPoint(target.Position);
            proj.Launch(target, _effectiveDamageBase, _data.DamageType,
                        _effectiveOnHitEffects, _projectilePool, _effectiveBonusDamage);
        }

        _attackTimer = _data.AttackSpeed > 0f ? 1f / _data.AttackSpeed : 1f;
        _isShooting  = false;
    }

    private void OnMouseDown()
    {
        if (_state != TowerState.Active) return;
        OnTowerClicked?.Invoke(this);
    }

    private void OnDestroy()
    {
        _projectilePool?.Clear();
    }

    /// <summary>
    /// Returns a spawn point on the edge of the tower facing the target.
    /// Uses half the cell size as the offset distance.
    /// </summary>
    private Vector3 GetSpawnPoint(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        float offset = GridManager.CellSize * 0.5f;
        return transform.position + dir * offset;
    }

    // ── Object pool (parameter avoids 0-param duplicate detection) ───────────

    private void BuildProjectilePool(GameObject prefabGO)
    {
        var comp = prefabGO.GetComponent<ProjectileBehaviour>();
        if (comp == null)
        {
            Debug.LogError("[TowerBehaviour] ProjectilePrefab missing ProjectileBehaviour component.");
            return;
        }

        _projectilePool = new ObjectPool<ProjectileBehaviour>(
            createFunc:      () => Instantiate(comp),
            actionOnGet:     p  => p.gameObject.SetActive(true),
            actionOnRelease: p  => p.gameObject.SetActive(false),
            actionOnDestroy: p  => Destroy(p.gameObject),
            collectionCheck: false,
            defaultCapacity: 5,
            maxSize: 20
        );
    }
}
