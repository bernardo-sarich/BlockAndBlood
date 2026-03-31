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
    [SerializeField] private float    _speed           = 16.67f; // cells/sec
    [SerializeField] private float    _hitRadius       = 0.31f;  // cells
    [SerializeField] private Sprite[] _fireArrowFrames;
    [SerializeField] private float    _fireAnimFps     = 8f;

    private IDamageable _target;
    private float       _damage;
    private float       _bonusDamage; // ignores armor (DamageType.Fire)
    private DamageType  _damageType;
    private EffectData[] _onHitEffects;
    private IObjectPool<ProjectileBehaviour> _pool;

    private SpriteRenderer _spriteRenderer;
    private Sprite         _defaultSprite;
    private bool           _isFire;
    private float          _fireAnimTimer;
    private int            _fireAnimFrame;

    public static event System.Action<ProjectileBehaviour> OnHit;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Launch(
        IDamageable target,
        float damage,
        DamageType damageType,
        EffectData[] onHitEffects,
        IObjectPool<ProjectileBehaviour> pool,
        float bonusDamage = 0f)
    {
        _target       = target;
        _damage       = damage;
        _bonusDamage  = bonusDamage;
        _damageType   = damageType;
        _onHitEffects = onHitEffects;
        _pool         = pool;

        _isFire = bonusDamage > 0f && _fireArrowFrames != null && _fireArrowFrames.Length > 0;

        if (_spriteRenderer != null)
        {
            if (_defaultSprite == null) _defaultSprite = _spriteRenderer.sprite;

            if (_isFire)
            {
                _fireAnimFrame = 0;
                _fireAnimTimer = 0f;
                _spriteRenderer.sprite = _fireArrowFrames[0];
            }
            else
            {
                _spriteRenderer.sprite = _defaultSprite;
            }
        }
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

        // Animate fire arrow frames while in flight
        if (_isFire && _spriteRenderer != null && _fireArrowFrames.Length > 0)
        {
            _fireAnimTimer += Time.deltaTime;
            float frameDuration = 1f / _fireAnimFps;
            if (_fireAnimTimer >= frameDuration)
            {
                _fireAnimTimer -= frameDuration;
                _fireAnimFrame = (_fireAnimFrame + 1) % _fireArrowFrames.Length;
                _spriteRenderer.sprite = _fireArrowFrames[_fireAnimFrame];
            }
        }

        if (toTarget.magnitude <= _hitRadius * GridManager.CellSize)
        {
            _target.TakeDamage(_damage, _damageType);
            if (_bonusDamage > 0f) _target.TakeDamage(_bonusDamage, DamageType.Fire);
            if (_onHitEffects != null)
                for (int i = 0; i < _onHitEffects.Length; i++)
                    _target.ApplyEffect(_onHitEffects[i]);

            OnHit?.Invoke(this);
            _target = null;
            _pool?.Release(this);
        }
        else
        {
            transform.position += toTarget.normalized * (_speed * GridManager.CellSize * Time.deltaTime);
        }
    }
}
