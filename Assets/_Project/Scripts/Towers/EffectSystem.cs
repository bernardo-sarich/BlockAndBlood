using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages active status effects on a single enemy (Burn, SlowArea, Slow, ArmorReduction).
/// Attach this component to every enemy prefab; EnemyBehaviour forwards ApplyEffect() calls here.
///
/// Burn:         Single slot. New application refreshes duration — does NOT stack.
/// SlowArea:     Single slot. Melee tower aura, refreshed every ~0.15 s while in range.
/// Slow:         Stacks from multiple projectile sources, capped at -70 %.
/// ArmorReduction: Single slot (takes max value), timer refreshed on reapplication.
/// </summary>
public class EffectSystem : MonoBehaviour
{
    // ── Burn ─────────────────────────────────────────────────────────────────
    private float _burnDps;
    private float _burnTimer;

    public bool  IsBurning            => _burnTimer > 0f;
    public float BurnDamagePerSecond  => IsBurning ? _burnDps : 0f;

    // ── SlowArea — Melee aura, non-stacking ──────────────────────────────────
    private float _slowAreaValue;
    private float _slowAreaTimer;

    // ── Slow — projectile-based, stacking ────────────────────────────────────
    private struct SlowInstance { public float Value; public float TimeRemaining; }
    private readonly List<SlowInstance> _slowSources = new List<SlowInstance>();

    // ── ArmorReduction ───────────────────────────────────────────────────────
    private float _armorReductionValue;
    private float _armorReductionTimer;

    // ── Computed results ─────────────────────────────────────────────────────

    /// <summary>
    /// Combined slow fraction 0..0.7 from all active sources.
    /// Multiply enemy base speed by (1 - CurrentSlowFraction).
    /// Computed on each access — call once per Update and cache locally.
    /// </summary>
    public float CurrentSlowFraction
    {
        get
        {
            float total = _slowAreaTimer > 0f ? _slowAreaValue : 0f;
            for (int i = 0; i < _slowSources.Count; i++) total += _slowSources[i].Value;
            return Mathf.Min(total, 0.7f);
        }
    }

    /// <summary>Armor fraction reduction (0..0.15). Applied before physical damage calc.</summary>
    public float CurrentArmorReduction => _armorReductionTimer > 0f ? _armorReductionValue : 0f;

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Apply or refresh a status effect.
    /// Called by EnemyBehaviour.ApplyEffect(), which is invoked by towers on hit or per tick.
    /// </summary>
    public void Apply(EffectData effect)
    {
        switch (effect.Type)
        {
            case EffectType.Burn:
                _burnDps   = effect.Value;
                _burnTimer = effect.Duration;
                break;

            case EffectType.SlowArea:
                _slowAreaValue = effect.Value;
                _slowAreaTimer = effect.Duration;
                break;

            case EffectType.Slow:
                _slowSources.Add(new SlowInstance
                {
                    Value         = effect.Value,
                    TimeRemaining = effect.Duration,
                });
                break;

            case EffectType.ArmorReduction:
                _armorReductionValue = Mathf.Max(_armorReductionValue, effect.Value);
                _armorReductionTimer = effect.Duration;
                break;
        }
    }

    /// <summary>
    /// Clears all active effects. Called by EnemyPool when returning an enemy to the pool.
    /// </summary>
    public void ClearEffects()
    {
        _burnDps             = 0f;
        _burnTimer           = 0f;
        _slowAreaValue       = 0f;
        _slowAreaTimer       = 0f;
        _armorReductionValue = 0f;
        _armorReductionTimer = 0f;
        _slowSources.Clear();
    }

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Update()
    {
        float dt = Time.deltaTime;

        if (_burnTimer            > 0f) _burnTimer            -= dt;
        if (_slowAreaTimer        > 0f) _slowAreaTimer        -= dt;
        if (_armorReductionTimer  > 0f) _armorReductionTimer  -= dt;

        for (int i = _slowSources.Count - 1; i >= 0; i--)
        {
            SlowInstance s = _slowSources[i];
            s.TimeRemaining -= dt;
            if (s.TimeRemaining <= 0f) _slowSources.RemoveAt(i);
            else _slowSources[i] = s;
        }
    }
}
