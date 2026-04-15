using UnityEngine;

/// <summary>
/// Floating HP bar shown above an enemy after it first takes damage.
/// Uses a 29-frame sprite sheet: frame 0 = 100 % HP, frame 28 = 0 % HP.
///
/// Add this component to a child GameObject of the enemy root that already
/// has a SpriteRenderer (Sorting Layer: Effects, Order: 10).
///
/// OnEnable resets visibility so object-pool reuse works correctly.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    private const int TotalFrames = 29;

    [SerializeField] private Sprite[] _frames;          // lifeRemaining_0 … lifeRemaining_28
    [SerializeField] private Vector3  _offset = new Vector3(0f, 0.7f, 0f);

    private SpriteRenderer  _sr;
    private EnemyBehaviour  _enemy;
    private bool            _hasBeenHit;

    private void Awake()
    {
        _sr    = GetComponent<SpriteRenderer>();
        _enemy = GetComponentInParent<EnemyBehaviour>();

        // Position relative to parent, driven by _offset so it can be tweaked per-prefab
        transform.localPosition = _offset;
    }

    // Called by the pool each time the enemy GO is activated (actionOnGet → SetActive(true))
    private void OnEnable()
    {
        _hasBeenHit = false;
        if (_sr != null) _sr.enabled = false;
    }

    private void LateUpdate()
    {
        if (_enemy == null || !_enemy.IsAlive) return;
        if (_sr == null) return;
        if (_frames == null || _frames.Length != TotalFrames)
        {
            Debug.LogWarning($"[EnemyHealthBar] _frames must have exactly {TotalFrames} sprites.", this);
            return;
        }

        float ratio = Mathf.Clamp01(_enemy.CurrentHp / _enemy.MaxHp);

        // Show once the enemy drops below full HP
        if (!_hasBeenHit)
        {
            if (ratio >= 1f) return;
            _hasBeenHit = true;
            _sr.enabled = true;
        }

        // Map HP ratio → frame index (0 = full, 28 = empty)
        int frameIdx = Mathf.RoundToInt((1f - ratio) * (TotalFrames - 1));
        frameIdx = Mathf.Clamp(frameIdx, 0, TotalFrames - 1);
        _sr.sprite = _frames[frameIdx];
    }
}
