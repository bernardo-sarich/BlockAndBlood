using UnityEngine;

/// <summary>
/// ScriptableObject holding all static configuration for a single enemy type.
/// Runtime state (current HP, active effects) lives in EnemyBehaviour + EffectSystem.
/// </summary>
[CreateAssetMenu(menuName = "Block&Blood/EnemyData", fileName = "NewEnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string EnemyName;

    [Header("Stats")]
    public float MaxHp;

    /// <summary>Base movement speed in cells per second (1 cell = GridManager.CellSize world units).</summary>
    public float MoveSpeed;

    /// <summary>Fraction of physical damage absorbed (0 = no armor, 0.5 = 50% reduction).</summary>
    [Range(0f, 1f)] public float ArmorFraction;

    [Header("Rewards")]
    public int GoldReward;
    public int XpReward;

    [Header("Prefab")]
    public GameObject Prefab;
}
