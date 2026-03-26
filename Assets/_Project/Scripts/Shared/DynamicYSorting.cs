using UnityEngine;

/// <summary>
/// Updates SpriteRenderer.sortingOrder every frame based on Y position.
/// Lower Y = higher sortingOrder = rendered in front (closer to camera).
/// Attach to any entity that moves (hero, enemies). Supports entities with
/// multiple child SpriteRenderers (e.g. layered hero) — each renderer keeps
/// its relative offset from the base computed order.
///
/// Static towers should set their sortingOrder once at placement time.
/// </summary>
public class DynamicYSorting : MonoBehaviour
{
    /// <summary>Multiplier for Y → sortingOrder conversion. Higher = more precision.</summary>
    private const int Precision = 100;

    private SpriteRenderer[] _renderers;
    private int[]            _offsets; // original sortingOrder offsets relative to first renderer

    private void Start()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (_renderers.Length == 0) return;

        // Store each renderer's offset relative to the first renderer so layered
        // sprites (hero body, armor, weapon) maintain their relative stacking.
        int baseOrder = _renderers[0].sortingOrder;
        _offsets = new int[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            _offsets[i] = _renderers[i].sortingOrder - baseOrder;
    }

    private void LateUpdate()
    {
        if (_renderers == null || _renderers.Length == 0) return;

        int order = Mathf.RoundToInt(-transform.position.y * Precision);

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            _renderers[i].sortingOrder = order + _offsets[i];
        }
    }
}
