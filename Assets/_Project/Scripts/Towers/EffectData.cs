/// <summary>
/// Types of status effects that towers can apply to enemies.
/// SlowArea: non-stacking aura slow (Melee towers — refreshed continuously while in range).
/// Slow: stacking projectile-based slow (accumulates up to -70%, applied via cards).
/// Burn: damage-over-time that ignores armor (applied via cards).
/// ArmorReduction: temporarily lowers physical armor (applied via cards).
/// </summary>
public enum EffectType
{
    Burn,
    SlowArea,
    Slow,
    ArmorReduction,
}

/// <summary>
/// Data payload for a single status effect applied on hit or per tick.
/// Serializable so it can be configured in TowerData ScriptableObjects.
/// </summary>
[System.Serializable]
public struct EffectData
{
    /// <summary>Which status effect to apply.</summary>
    public EffectType Type;

    /// <summary>
    /// Scalar value for the effect.
    /// Burn: damage per second (e.g. 4).
    /// SlowArea / Slow: speed reduction fraction (e.g. 0.15 = -15%).
    /// ArmorReduction: armor reduction fraction (e.g. 0.15 = -15%).
    /// </summary>
    public float Value;

    /// <summary>
    /// How long the effect lasts in seconds.
    /// For SlowArea this should be slightly longer than the reapply interval (~0.2s)
    /// so it reads as continuous while the enemy stays in range.
    /// </summary>
    public float Duration;
}
