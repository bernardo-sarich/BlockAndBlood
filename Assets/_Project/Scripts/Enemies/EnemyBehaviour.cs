using Pathfinding;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Controls a single enemy: A* pathfinding toward the goal, damage/armor logic,
/// status effects (Burn DoT, Slow, ArmorReduction) and death rewards.
///
/// Lifecycle:
///   WaveManager (or test code) calls Initialize(data, spawnPos, goalPos, pool).
///   On reaching the goal: fires OnEnemyReachedGoal and returns itself to pool.
///   On death:            fires OnEnemyDeath, awards gold, returns to pool.
///
/// Requires EffectSystem, Seeker, and AIPath on the same GameObject.
/// The GameObject must be in the "Enemy" layer for tower OverlapCircle detection.
/// </summary>
[RequireComponent(typeof(EffectSystem))]
[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(AIPath))]
public class EnemyBehaviour : MonoBehaviour, IDamageable, IHasHp
{
    // World units per cell — must match GridManager.CellSize.
    private const float CellSize = GridManager.CellSize;

    // ── Events ───────────────────────────────────────────────────────────────
    /// <summary>Fired on death. Parameters: enemy, goldReward, xpReward.</summary>
    public static event System.Action<EnemyBehaviour, int, int> OnEnemyDeath;

    /// <summary>Fired when the enemy reaches the goal (base damage).</summary>
    public static event System.Action<EnemyBehaviour> OnEnemyReachedGoal;

    // ── Runtime state ────────────────────────────────────────────────────────
    private EnemyData  _data;
    private float      _currentHp;
    private bool       _isDead;
    private float      _totalPathLength;

    private EffectSystem                _effects;
    private AIPath                      _aiPath;
    private IObjectPool<EnemyBehaviour> _pool;

    // ── IDamageable / IHasHp ─────────────────────────────────────────────────
    public float   CurrentHp => _currentHp;
    public bool    IsAlive   => !_isDead;
    public Vector3 Position  => transform.position;

    // ── Initialisation ───────────────────────────────────────────────────────

    /// <summary>
    /// Call immediately after taking from the pool (or after Instantiate for test spawns).
    /// Sets HP, positions the enemy at spawnPos and starts pathfinding toward goalPos.
    /// </summary>
    public void Initialize(EnemyData data, Vector3 spawnPos, Vector3 goalPos,
                           IObjectPool<EnemyBehaviour> pool = null)
    {
        _data            = data;
        _currentHp       = data.MaxHp;
        _isDead          = false;
        _totalPathLength = 0f;
        _pool            = pool;

        transform.position = spawnPos;

        _effects = GetComponent<EffectSystem>();
        _aiPath  = GetComponent<AIPath>();

        // Ensure dynamic Y-sorting is present for perspective camera depth ordering
        if (GetComponent<DynamicYSorting>() == null)
            gameObject.AddComponent<DynamicYSorting>();

        if (_aiPath != null)
        {
            _aiPath.maxSpeed    = data.MoveSpeed * CellSize;
            _aiPath.destination = goalPos;
            _aiPath.SearchPath();
        }
    }

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Update()
    {
        if (_isDead || _data == null) return;

        float dt = Time.deltaTime;

        // ── Burn DoT (ignores armor — always DamageType.Fire) ─────────────
        if (_effects.IsBurning)
            TakeDamageInternal(_effects.BurnDamagePerSecond * dt, ignoreArmor: true);

        if (_isDead) return;

        // ── Sync AIPath speed with slow effect ────────────────────────────
        if (_aiPath != null)
        {
            _aiPath.maxSpeed = _data.MoveSpeed * CellSize * (1f - _effects.CurrentSlowFraction);

            // Capture total path length on the first frame the path is ready
            if (_totalPathLength <= 0f && _aiPath.hasPath)
                _totalPathLength = _aiPath.remainingDistance;

            // Goal reached → remove enemy (no HP damage to base in MVP)
            if (_aiPath.reachedEndOfPath)
                ReachGoal();
        }
    }

    // ── IDamageable ──────────────────────────────────────────────────────────

    public void TakeDamage(float amount, DamageType type)
    {
        if (_isDead) return;
        bool ignoreArmor = type != DamageType.Physical;
        TakeDamageInternal(amount, ignoreArmor);
    }

    public void ApplyEffect(EffectData effect)
    {
        if (_isDead) return;
        _effects.Apply(effect);
    }

    /// <summary>
    /// Returns 0–1 normalized progress toward the goal.
    /// 0 = just spawned, 1 = reached goal.
    /// Used by ranged towers to prioritise the most-advanced enemy.
    /// </summary>
    public float GetPathProgress()
    {
        if (_aiPath == null || _totalPathLength <= 0f) return 0f;
        return Mathf.Clamp01(1f - _aiPath.remainingDistance / _totalPathLength);
    }

    // ── Internal helpers ─────────────────────────────────────────────────────

    private void TakeDamageInternal(float amount, bool ignoreArmor)
    {
        if (_isDead) return;

        float effective = amount;

        if (!ignoreArmor && _data.ArmorFraction > 0f)
        {
            float armor = Mathf.Max(0f, _data.ArmorFraction - _effects.CurrentArmorReduction);
            effective  *= (1f - armor);
        }

        _currentHp -= effective;

        if (_currentHp <= 0f)
            Die();
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        EconomyManager.Instance?.Add(_data.GoldReward);
        OnEnemyDeath?.Invoke(this, _data.GoldReward, _data.XpReward);

        ReturnToPool();
    }

    private void ReachGoal()
    {
        if (_isDead) return;
        _isDead = true;

        OnEnemyReachedGoal?.Invoke(this);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (_pool != null) _pool.Release(this);
        else               Destroy(gameObject);
    }
}
