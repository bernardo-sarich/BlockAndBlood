using UnityEngine;

/// <summary>
/// Visual-only heal projectile spawned by PriestBehaviour.
/// Flies toward a target enemy while animating its sprite sheet.
/// Destroys itself on arrival or if the target is gone.
/// </summary>
public class HealOrb : MonoBehaviour
{
    private const float Speed = 8.33f; // cells/sec

    private Transform      _target;
    private SpriteRenderer _sr;
    private Sprite[]       _frames;
    private float          _fps;
    private float          _timer;
    private int            _frameIdx;

    /// <summary>Called immediately after Instantiate to initialise the orb.</summary>
    public void Launch(Transform target, Sprite[] frames, float fps)
    {
        _target   = target;
        _frames   = frames;
        _fps      = fps > 0f ? fps : 8f;
        _sr       = GetComponent<SpriteRenderer>();
        _frameIdx = 0;
        _timer    = 0f;

        if (_sr != null && _frames != null && _frames.Length > 0)
            _sr.sprite = _frames[0];
    }

    private void Update()
    {
        // Destroy if target disappeared
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        // Animate
        if (_frames != null && _frames.Length > 0 && _sr != null)
        {
            _timer += Time.deltaTime;
            float interval = 1f / _fps;
            if (_timer >= interval)
            {
                _timer    -= interval;
                _frameIdx  = (_frameIdx + 1) % _frames.Length;
                _sr.sprite = _frames[_frameIdx];
            }
        }

        // Move toward target
        Vector3 delta = _target.position - transform.position;
        float   dist  = delta.magnitude;
        float   step  = Speed * GridManager.CellSize * Time.deltaTime;

        if (dist <= step)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += delta.normalized * step;
    }
}
