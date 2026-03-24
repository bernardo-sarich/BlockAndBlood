using UnityEngine;

/// <summary>
/// ScriptableObject that holds all configuration for a single tower type or upgrade tier.
/// UpgradePaths references the next-tier TowerData SOs (empty array = no upgrades available).
/// </summary>
[CreateAssetMenu(menuName = "Block&Blood/TowerData", fileName = "NewTowerData")]
public class TowerData : ScriptableObject
{
    [Header("Identity")]
    public string TowerName;
    public TowerType Type;

    /// <summary>Gold cost incremental for this tier (construction cost or upgrade delta).</summary>
    public int Cost;

    [Header("Combat")]
    public float DamageBase;

    /// <summary>Attacks per second. Ignored for IsAreaAttack towers (damage is per-second continuous).</summary>
    public float AttackSpeed;

    /// <summary>Attack radius in world units. Use CellSize multiples: 1.44 = 1.5 cells, 2.88 = 3 cells.</summary>
    public float Range;

    public DamageType DamageType;

    /// <summary>True for Melee towers: deals continuous AoE damage, no projectile.</summary>
    public bool IsAreaAttack;

    /// <summary>Status effects applied on hit (or per reapply tick for area towers).</summary>
    public EffectData[] OnHitEffects;

    [Header("Visuals")]
    /// <summary>Icon sprite for HUD buttons and placement preview cursor.</summary>
    public Sprite Icon;

    [Header("Prefabs")]
    /// <summary>The placed tower GameObject. Must have a TowerBehaviour component.</summary>
    public GameObject TowerPrefab;

    /// <summary>Projectile GameObject. Must have a ProjectileBehaviour component. Leave null for area towers.</summary>
    public GameObject ProjectilePrefab;

    [Header("Progression")]
    /// <summary>Available upgrade paths (empty = no upgrades available).</summary>
    public TowerData[] UpgradePaths;
}
