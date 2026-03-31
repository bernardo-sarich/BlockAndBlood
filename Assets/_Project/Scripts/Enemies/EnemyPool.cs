using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Manages one ObjectPool&lt;EnemyBehaviour&gt; per enemy type (keyed by EnemyData asset).
/// Pools are created lazily on first Spawn of each type.
///
/// Usage:
///   EnemyPool.Instance.Spawn(enemyData, spawnPos, goalPos);
///   EnemyPool.Instance.Despawn(enemy);   // also called automatically by EnemyBehaviour on death/goal
/// </summary>
public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    private const int DefaultCapacity = 30;
    private const int MaxPoolSize     = 60;

    // One pool per EnemyData asset
    private readonly Dictionary<EnemyData, ObjectPool<EnemyBehaviour>> _pools     = new();
    // Tracks which pool each active enemy came from so Despawn can release it correctly
    private readonly Dictionary<EnemyBehaviour, ObjectPool<EnemyBehaviour>> _activeMap = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Gets an enemy from the correct pool, positions it and starts pathfinding.
    /// Returns null if the EnemyData has no Prefab assigned.
    /// </summary>
    public EnemyBehaviour Spawn(EnemyData data, Vector3 spawnPos, Vector3 goalPos)
    {
        var pool = GetOrCreatePool(data);
        if (pool == null) return null;

        var enemy = pool.Get();
        _activeMap[enemy] = pool;

        // Pass a null pool reference — EnemyBehaviour will route back through EnemyPool.Despawn
        enemy.Initialize(data, spawnPos, goalPos, pool: null);
        return enemy;
    }

    /// <summary>
    /// Returns an enemy to its pool. Called automatically by EnemyBehaviour on death or reaching the goal.
    /// Safe to call manually too (idempotent guard via _activeMap).
    /// </summary>
    public void Despawn(EnemyBehaviour enemy)
    {
        if (enemy == null) return;
        if (!_activeMap.TryGetValue(enemy, out var pool))
        {
            // Enemy was not spawned through this pool (e.g. TestEnemySpawner), just destroy
            Destroy(enemy.gameObject);
            return;
        }

        _activeMap.Remove(enemy);
        pool.Release(enemy);
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private ObjectPool<EnemyBehaviour> GetOrCreatePool(EnemyData data)
    {
        if (_pools.TryGetValue(data, out var existing)) return existing;

        if (data.Prefab == null)
        {
            Debug.LogError($"[EnemyPool] EnemyData '{data.EnemyName}' has no Prefab assigned. Cannot create pool.");
            return null;
        }

        var prefab = data.Prefab; // capture for closure

        var pool = new ObjectPool<EnemyBehaviour>(
            createFunc: () =>
            {
                var go = Instantiate(prefab);
                var eb = go.GetComponent<EnemyBehaviour>();
                if (eb == null)
                    Debug.LogError($"[EnemyPool] Prefab '{prefab.name}' is missing EnemyBehaviour component.");
                return eb;
            },
            actionOnGet:     e => e.gameObject.SetActive(true),
            actionOnRelease: e => e.gameObject.SetActive(false),
            actionOnDestroy: e => { if (e != null) Destroy(e.gameObject); },
            collectionCheck: false,
            defaultCapacity:  DefaultCapacity,
            maxSize:          MaxPoolSize
        );

        _pools[data] = pool;
        return pool;
    }
}
