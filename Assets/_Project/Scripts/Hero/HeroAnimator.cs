using System;
using UnityEngine;

/// <summary>
/// Drives the hero's sprite animation using LPC-format sprite sheets.
/// Supports layered character rendering (one SpriteRenderer per body layer),
/// 4-directional walk cycle, and attack (slash) animation.
///
/// LPC row order: Up=0, Left=1, Down=2, Right=3
/// Walk sheet:   9 frames/direction (frame 0 = idle, frames 1-8 = walk cycle)
/// Attack sheet: 6 frames/direction
///
/// Layers are populated by HeroChainArmorSetup (Tools > Setup Hero Chain Armor).
/// </summary>
[ExecuteAlways]
public class HeroAnimator : MonoBehaviour
{
    [Serializable]
    public struct AnimLayer
    {
        public string         label;
        public SpriteRenderer renderer;
        public Sprite[]       walkSprites;    // 4 dirs * walkCols frames
        public Sprite[]       attackSprites;  // 4 dirs * attackCols frames (can be null/empty)
    }

    // LPC standard direction rows
    private enum Dir { Up = 0, Left = 1, Down = 2, Right = 3 }

    [Header("Layers (populated by HeroChainArmorSetup)")]
    [SerializeField] public AnimLayer[] layers;

    [Header("Frame Config")]
    [SerializeField] public int walkCols   = 9;   // 1 idle + 8 walk frames
    [SerializeField] public int attackCols = 6;

    [Header("Playback Speed")]
    [SerializeField] private float _walkFps   = 8f;
    [SerializeField] private float _attackFps = 12f;

    // ── State ──────────────────────────────────────────────────────────────────
    private Dir   _dir      = Dir.Up;
    private bool  _isMoving;
    private bool  _isAttacking;
    private int   _frame;
    private float _timer;

    // ── Unity lifecycle ────────────────────────────────────────────────────────

    private void Awake()
    {
        // Apply the idle frame immediately so components that run in Start()
        // (e.g. EntityShadow) find a valid sprite on each layer renderer.
        ApplySprite();
    }

    private void Update()
    {
        // In edit mode: just display the idle frame so the character is visible in Scene view
        if (!Application.isPlaying)
        {
            ApplySprite();
            return;
        }

        float interval = 1f / (_isAttacking ? _attackFps : _walkFps);
        _timer += Time.deltaTime;

        if (_timer >= interval)
        {
            _timer -= interval;
            AdvanceFrame();
        }

        ApplySprite();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Call every frame with the hero's movement input.
    /// Dominant axis wins for 4-directional facing. Zero vector = idle.
    /// </summary>
    public void SetMovement(Vector2 velocity)
    {
        bool moving = velocity.sqrMagnitude > 0.01f;

        if (moving)
        {
            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
                _dir = velocity.x > 0f ? Dir.Right : Dir.Left;
            else
                _dir = velocity.y > 0f ? Dir.Up : Dir.Down;
        }

        if (moving && !_isMoving)
            _frame = 1;

        _isMoving = moving;
    }

    /// <summary>Triggers the attack animation. Ignored if already playing.</summary>
    public void TriggerAttack()
    {
        if (_isAttacking) return;
        _isAttacking = true;
        _frame       = 0;
        _timer       = 0f;
    }

    // ── Internal ───────────────────────────────────────────────────────────────

    private void AdvanceFrame()
    {
        if (_isAttacking)
        {
            _frame++;
            if (_frame >= attackCols)
            {
                _isAttacking = false;
                _frame       = _isMoving ? 1 : 0;
            }
        }
        else if (_isMoving)
        {
            _frame++;
            if (_frame >= walkCols)
                _frame = 1;
        }
        else
        {
            _frame = 0;
        }
    }

    private void ApplySprite()
    {
        if (layers == null) return;

        for (int i = 0; i < layers.Length; i++)
        {
            var layer = layers[i];
            if (layer.renderer == null) continue;

            bool     useAttack = _isAttacking && layer.attackSprites != null && layer.attackSprites.Length > 0;
            Sprite[] sheet     = useAttack ? layer.attackSprites : layer.walkSprites;
            int      cols      = useAttack ? attackCols : walkCols;

            if (sheet == null || sheet.Length == 0)
            {
                layer.renderer.enabled = false;
                continue;
            }

            layer.renderer.enabled = true;
            int idx = (int)_dir * cols + _frame;
            idx = Mathf.Clamp(idx, 0, sheet.Length - 1);
            if (sheet[idx] != null)
                layer.renderer.sprite = sheet[idx];
        }
    }
}
