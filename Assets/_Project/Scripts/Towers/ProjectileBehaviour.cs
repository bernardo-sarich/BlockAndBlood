using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Controls a single in-flight projectile from a ranged tower.
/// Homes toward an IDamageable target; applies damage and effects on contact.
/// Managed by an ObjectPool owned by TowerBehaviour — never Destroy manually.
/// Call Launch() immediately after taking from the pool.
/// </summary>
public class ProjectileBehaviour : MonoBehaviour
{
    [SerializeField] private float _speed     = 8f;
    [SerializeField] private float _hitRadius = 0.15f;

    private IDamageable _target;
    private float       _damage;
    private DamageType  _damageType;
    private EffectData[] _onHitEffects;
    private IObjectPool<ProjectileBehaviour> _pool;

    public static event System.Action<ProjectileBehaviour> OnHit;

    public void Launch(
        IDamageable target,
        float damage,
        DamageType damageType,
        EffectData[] onHitEffects,
        IObjectPool<ProjectileBehaviour> pool)
    {
        _target       = target;
        _damage       = damage;
        _damageType   = damageType;
        _onHitEffects = onHitEffects;
        _pool         = pool;
    }

    private void Update()
    {
        if (_target == null || !_target.IsAlive)
        {
            _target = null;
            _pool?.Release(this);
            return;
        }

        Vector3 toTarget = _target.Position - transform.position;

        // Rotate arrow to point at target (sprite faces right at angle 0)
        float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (toTarget.magnitude <= _hitRadius)
        {
            _target.TakeDamage(_damage, _damageType);
            if (_onHitEffects != null)
                for (int i = 0; i < _onHitEffects.Length; i++)
                    _target.ApplyEffect(_onHitEffects[i]);

            OnHit?.Invoke(this);
            _target = null;
            _pool?.Release(this);
        }
        else
        {
            transform.position += toTarget.normalized * (_speed * Time.deltaTime);
        }
    }
}
