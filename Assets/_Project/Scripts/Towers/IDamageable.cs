using UnityEngine;

/// <summary>
/// Contract that any damageable entity (enemies) must implement.
/// Used by TowerBehaviour and ProjectileBehaviour to interact with enemies
/// without a direct dependency on EnemyBehaviour.
/// </summary>
public interface IDamageable
{
    /// <summary>Apply damage. Armor rules are applied internally by the implementor.</summary>
    void TakeDamage(float amount, DamageType type);

    /// <summary>Apply a status effect (burn, slow, armor reduction).</summary>
    void ApplyEffect(EffectData effect);

    /// <summary>
    /// 0-1 normalized progress along the path toward the goal.
    /// Used by ranged towers to prioritise the most-advanced enemy.
    /// </summary>
    float GetPathProgress();

    /// <summary>False if the entity is dead or being pooled.</summary>
    bool IsAlive { get; }

    /// <summary>Current world-space position (for projectile homing).</summary>
    Vector3 Position { get; }
}
