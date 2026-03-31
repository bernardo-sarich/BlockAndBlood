using UnityEngine;

/// <summary>
/// Shows an animated blue halo below an enemy whenever it is covered by a Blindado armor aura.
/// The child SpriteRenderer is created at runtime; _frames is assigned in each enemy prefab.
///
/// Multiple overlapping Blindados still produce a single halo — EnemyBehaviour.IsUnderBruteAura
/// already consolidates the reference count, so this component just mirrors that boolean.
/// </summary>
public class AuraBlindadoVisual : MonoBehaviour
{
    private const float AnimFps    = 6f;
    private const float YOffset    = -0.73f;
    private const int   SortOrder  = 3;   // below enemy sprite (order 5), above ground

    [SerializeField] private Sprite[] _frames;

    private EnemyBehaviour _enemy;
    private SpriteRenderer _auraRenderer;
    private float          _timer;
    private int            _frameIndex;
    private bool           _isVisible;

    private void Awake()
    {
        _enemy = GetComponent<EnemyBehaviour>();

        var go = new GameObject("AuraEffect");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, YOffset, 0f);

        _auraRenderer              = go.AddComponent<SpriteRenderer>();
        _auraRenderer.sortingOrder = SortOrder;

        if (_frames != null && _frames.Length > 0)
            _auraRenderer.sprite = _frames[0];

        go.SetActive(false);
    }

    private void OnDisable()
    {
        // Reset visible flag so state is clean when the enemy returns from the pool.
        _isVisible = false;
    }

    private void Update()
    {
        if (_enemy == null || _frames == null || _frames.Length == 0) return;

        bool shouldShow = _enemy.IsUnderBruteAura;

        if (shouldShow != _isVisible)
        {
            _isVisible = shouldShow;
            _auraRenderer.gameObject.SetActive(shouldShow);
            if (shouldShow)
            {
                _frameIndex          = 0;
                _timer               = 0f;
                _auraRenderer.sprite = _frames[0];
            }
        }

        if (!_isVisible) return;

        _timer += Time.deltaTime;
        float frameDuration = 1f / AnimFps;
        if (_timer >= frameDuration)
        {
            _timer      -= frameDuration;
            _frameIndex  = (_frameIndex + 1) % _frames.Length;
            _auraRenderer.sprite = _frames[_frameIndex];
        }
    }
}
