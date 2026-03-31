using Pathfinding;
using UnityEngine;

/// <summary>
/// Drives walk animation for enemies using LPC-format walkcycle sprite sheets.
/// Reads movement direction from AIPath.velocity to select the correct row.
///
/// LPC row order: Up=0, Left=1, Down=2, Right=3
/// Walk sheet:   9 frames/direction (frame 0 = idle, frames 1-8 = walk cycle)
///
/// Populate walkSprites via Inspector or editor script (Tools > Setup Enemy Animator).
/// Array order must match: [row0_col0, row0_col1, ..., row0_col8, row1_col0, ..., row3_col8]
/// </summary>
public class EnemyAnimator : MonoBehaviour
{
    private enum Dir { Up = 0, Left = 1, Down = 2, Right = 3 }

    [Header("Sprites (4 dirs x N frames)")]
    [SerializeField] public Sprite[] walkSprites;

    [Header("Playback")]
    [SerializeField] private float _walkFps = 8f;
    [SerializeField] private int   _walkCols = 9;

    // When true, Update() skips — lets PriestBehaviour (or similar) own the sprite.
    public bool IsLocked { get; set; }

    private SpriteRenderer _sr;
    private AIPath         _aiPath;
    private Dir            _dir   = Dir.Down;
    private int            _frame;
    private float          _timer;
    private bool           _isMoving;

    private void Awake()
    {
        _sr     = GetComponent<SpriteRenderer>();
        _aiPath = GetComponent<AIPath>();
        ApplySprite();
    }

    private void Update()
    {
        if (IsLocked) return;
        if (_sr == null || walkSprites == null || walkSprites.Length == 0) return;

        // Read movement from AIPath
        Vector2 vel = _aiPath != null ? (Vector2)_aiPath.velocity : Vector2.zero;
        bool moving = vel.sqrMagnitude > 0.01f;

        if (moving)
        {
            if (Mathf.Abs(vel.x) > Mathf.Abs(vel.y))
                _dir = vel.x > 0f ? Dir.Right : Dir.Left;
            else
                _dir = vel.y > 0f ? Dir.Up : Dir.Down;
        }

        if (moving && !_isMoving)
            _frame = 1; // start walk cycle

        _isMoving = moving;

        // Advance frame
        float interval = 1f / _walkFps;
        _timer += Time.deltaTime;
        if (_timer >= interval)
        {
            _timer -= interval;
            if (_isMoving)
            {
                _frame++;
                if (_frame >= _walkCols)
                    _frame = 1;
            }
            else
            {
                _frame = 0;
            }
        }

        ApplySprite();
    }

    private void ApplySprite()
    {
        if (_sr == null || walkSprites == null || walkSprites.Length == 0) return;

        int idx = (int)_dir * _walkCols + _frame;
        idx = Mathf.Clamp(idx, 0, walkSprites.Length - 1);
        if (walkSprites[idx] != null)
            _sr.sprite = walkSprites[idx];
    }
}
