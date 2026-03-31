using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attached to the Blindado enemy prefab.
/// Emits a passive aura that grants +30 % armor (EnemyBehaviour.BruteAuraArmorBonus)
/// to all enemies within range, including itself.
///
/// Multiple sources do NOT stack: EnemyBehaviour tracks a reference count so an enemy
/// under two overlapping auras still receives only one +30 % bonus.
/// The aura is recalculated every frame; losing range contact removes the bonus
/// as long as no other carrier still covers that enemy.
/// </summary>
[RequireComponent(typeof(EnemyBehaviour))]
public class BruteBehaviour : MonoBehaviour
{
    private const float AuraRadius = 4f; // cells

    private EnemyBehaviour            _self;
    private int                       _enemyLayerMask;
    private readonly HashSet<EnemyBehaviour> _buffedEnemies = new();

    private void Awake()
    {
        _self           = GetComponent<EnemyBehaviour>();
        _enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    private void Update()
    {
        if (!_self.IsAlive)
        {
            ClearAllBuffs();
            return;
        }

        RefreshAura();
    }

    private void OnDisable()
    {
        ClearAllBuffs();
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private void RefreshAura()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, AuraRadius * GridManager.CellSize, _enemyLayerMask);

        // Build current-frame set
        var newBuffed = new HashSet<EnemyBehaviour>();
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyBehaviour>();
            if (enemy != null && enemy.IsAlive)
                newBuffed.Add(enemy);
        }

        // Remove buff from enemies that left range
        foreach (var enemy in _buffedEnemies)
            if (!newBuffed.Contains(enemy))
                enemy.RemoveBruteAura();

        // Add buff to enemies that entered range
        foreach (var enemy in newBuffed)
            if (!_buffedEnemies.Contains(enemy))
                enemy.AddBruteAura();

        _buffedEnemies.Clear();
        foreach (var e in newBuffed)
            _buffedEnemies.Add(e);
    }

    private void ClearAllBuffs()
    {
        foreach (var enemy in _buffedEnemies)
            if (enemy != null) enemy.RemoveBruteAura();
        _buffedEnemies.Clear();
    }
}
