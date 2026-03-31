using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

/// <summary>
/// Attached to the Sacerdote (Priest) enemy prefab.
/// Every 2 seconds, heals nearby enemies for 15 % of their max HP.
/// Does NOT heal other Priests — only heals itself and non-Priest enemies in range.
/// Plays a cast animation, stops moving, and fires one HealOrb per target on frame 2.
/// </summary>
[RequireComponent(typeof(EnemyBehaviour))]
public class PriestBehaviour : MonoBehaviour
{
    private const float HealInterval = 2f;
    private const float HealRadius   = 4f; // cells
    private const float HealFraction = 0.15f;
    private const float CastFps      = 8f;
    private const float OrbFps       = 8f;

    [Header("Cast animation")]
    [SerializeField] public Sprite[] castSprites;

    [Header("Heal orb")]
    [SerializeField] private GameObject _orbPrefab;
    [SerializeField] public  Sprite[]   orbSprites;

    private EnemyBehaviour _self;
    private SpriteRenderer _sr;
    private EnemyAnimator  _animator;
    private AIPath         _aiPath;
    private float          _healTimer;
    private int            _enemyLayerMask;
    private Coroutine      _castRoutine;

    private void Awake()
    {
        _self           = GetComponent<EnemyBehaviour>();
        _sr             = GetComponent<SpriteRenderer>();
        _animator       = GetComponent<EnemyAnimator>();
        _aiPath         = GetComponent<AIPath>();
        _enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    private void OnEnable()
    {
        _healTimer = 0f;
    }

    private void Update()
    {
        if (!_self.IsAlive) return;

        _healTimer += Time.deltaTime;
        if (_healTimer >= HealInterval)
        {
            _healTimer = 0f;
            HealNearbyEnemies();
        }
    }

    private void HealNearbyEnemies()
    {
        var hits    = Physics2D.OverlapCircleAll(transform.position, HealRadius * GridManager.CellSize, _enemyLayerMask);
        var targets = new List<Transform>();

        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyBehaviour>();
            if (enemy == null || !enemy.IsAlive) continue;

            // Skip other Priests (but heal self)
            if (enemy != _self && hit.GetComponent<PriestBehaviour>() != null) continue;

            enemy.Heal(enemy.MaxHp * HealFraction);
            targets.Add(hit.transform);
        }

        if (castSprites != null && castSprites.Length > 0 && _sr != null)
        {
            if (_castRoutine != null) StopCoroutine(_castRoutine);
            _castRoutine = StartCoroutine(PlayCastAnimation(targets));
        }
    }

    private IEnumerator PlayCastAnimation(List<Transform> targets)
    {
        if (_animator != null) _animator.IsLocked = true;
        if (_aiPath   != null) _aiPath.canMove    = false;

        float frameDuration = 1f / CastFps;

        for (int i = 0; i < castSprites.Length; i++)
        {
            if (castSprites[i] != null) _sr.sprite = castSprites[i];

            // Frame 2 (index 1) — launch one orb per healed target
            if (i == 1 && _orbPrefab != null)
            {
                foreach (var target in targets)
                {
                    if (target == null) continue;
                    var orb = Instantiate(_orbPrefab, transform.position, Quaternion.identity);
                    orb.GetComponent<HealOrb>()?.Launch(target, orbSprites, OrbFps);
                }
            }

            yield return new WaitForSeconds(frameDuration);
        }

        if (_aiPath != null)
        {
            _aiPath.canMove = true;
            _aiPath.SearchPath();
        }
        if (_animator != null) _animator.IsLocked = false;
        _castRoutine = null;
    }
}
