using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages active status effects on a single enemy (Burn, SlowArea, Slow, ArmorReduction).
/// Attach this component to every enemy prefab; EnemyBehaviour forwards ApplyEffect() calls here.
///
/// Burn:         Single slot. New application refreshes duration — does NOT stack.
/// SlowArea:     Single slot. Melee tower aura, refreshed every ~0.15 s while in range.
/// Slow:         Stacks from multiple projectile sources, capped at -70 %.
/// ArmorReduction: Single slot (takes max value), timer refreshed on reapplication.
/// </summary>
public class EffectSystem : MonoBehaviour
{
    // ── Burn ─────────────────────────────────────────────────────────────────
    private float _burnDps;
    private float _burnTimer;

    public bool  IsBurning            => _burnTimer > 0f;
    public float BurnDamagePerSecond  => IsBurning ? _burnDps : 0f;

    // ── Burn VFX ─────────────────────────────────────────────────────────────
    [SerializeField] private Sprite[] _burnFrames;
    private const float BurnAnimFps = 8f;

    // ── Lightning Strike VFX ─────────────────────────────────────────────────
    [SerializeField] private Sprite[] _lightningFrames;
    [SerializeField] private float    _lightningScaleY = 2f;
    private const float LightningAnimFps = 12f;

    private GameObject     _lightningVfxGO;
    private SpriteRenderer _lightningVfxSR;
    private Coroutine      _lightningAnim;

    // Material Additive compartido entre todos los enemies (creado una sola vez)
    private static Material s_additiveMat;
    private static Material AdditiveMat
    {
        get
        {
            if (s_additiveMat != null) return s_additiveMat;
            var shader = Shader.Find("Sprites/Additive") ?? Shader.Find("Sprites/Default");
            return s_additiveMat = new Material(shader);
        }
    }

    private GameObject     _burnVfxGO;
    private SpriteRenderer _burnVfxSR;
    private SpriteRenderer _parentSR;
    private Coroutine      _burnAnim;

    // ── SlowArea — Melee aura, non-stacking ──────────────────────────────────
    private float _slowAreaValue;
    private float _slowAreaTimer;

    // ── Slow — projectile-based, stacking ────────────────────────────────────
    private struct SlowInstance { public float Value; public float TimeRemaining; }
    private readonly List<SlowInstance> _slowSources = new List<SlowInstance>();

    // ── ArmorReduction ───────────────────────────────────────────────────────
    private float _armorReductionValue;
    private float _armorReductionTimer;

    // ── Poison ───────────────────────────────────────────────────────────────
    private int   _poisonStacks;
    private float _poisonDps;      // Value por stack
    private int   _poisonMaxStacks;
    private float _poisonTimer;

    public bool  IsPoisoned            => _poisonTimer > 0f && _poisonStacks > 0;
    public float PoisonDamagePerSecond => IsPoisoned ? _poisonDps * _poisonStacks : 0f;
    public int   PoisonStacks          => _poisonStacks;

    // ── Maldicion ────────────────────────────────────────────────────────────
    private float _maldicionValue;
    private float _maldicionTimer;

    /// <summary>Damage multiplier applied to ALL incoming damage (0 = inactive).</summary>
    public float CurrentMaldicion => _maldicionTimer > 0f ? _maldicionValue : 0f;

    // ── Stun ─────────────────────────────────────────────────────────────────
    private float _stunTimer;
    private float _preStunSpeed;   // velocidad guardada antes del stun
    private Pathfinding.AIPath _aiPath;

    public bool IsStunned => _stunTimer > 0f;

    // ── Computed results ─────────────────────────────────────────────────────

    /// <summary>
    /// Combined slow fraction 0..0.7 from all active sources.
    /// Multiply enemy base speed by (1 - CurrentSlowFraction).
    /// Computed on each access — call once per Update and cache locally.
    /// </summary>
    public float CurrentSlowFraction
    {
        get
        {
            float total = _slowAreaTimer > 0f ? _slowAreaValue : 0f;
            for (int i = 0; i < _slowSources.Count; i++) total += _slowSources[i].Value;
            return Mathf.Min(total, 0.7f);
        }
    }

    /// <summary>Armor fraction reduction (0..0.15). Applied before physical damage calc.</summary>
    public float CurrentArmorReduction => _armorReductionTimer > 0f ? _armorReductionValue : 0f;

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Apply or refresh a status effect.
    /// Called by EnemyBehaviour.ApplyEffect(), which is invoked by towers on hit or per tick.
    /// </summary>
    public void Apply(EffectData effect)
    {
        switch (effect.Type)
        {
            case EffectType.Burn:
                _burnDps   = effect.Value;
                _burnTimer = effect.Duration;
                ShowBurnVfx();
                break;

            case EffectType.SlowArea:
                _slowAreaValue = effect.Value;
                _slowAreaTimer = effect.Duration;
                break;

            case EffectType.Slow:
                _slowSources.Add(new SlowInstance
                {
                    Value         = effect.Value,
                    TimeRemaining = effect.Duration,
                });
                break;

            case EffectType.ArmorReduction:
                _armorReductionValue = Mathf.Max(_armorReductionValue, effect.Value);
                _armorReductionTimer = effect.Duration;
                break;

            case EffectType.Poison:
                _poisonDps       = effect.Value;
                _poisonMaxStacks = effect.MaxStacks > 0 ? effect.MaxStacks : 5;
                _poisonStacks    = Mathf.Min(_poisonStacks + 1, _poisonMaxStacks);
                _poisonTimer     = effect.Duration; // refresca todos los stacks
                break;

            case EffectType.Maldicion:
                _maldicionValue = Mathf.Max(_maldicionValue, effect.Value);
                _maldicionTimer = effect.Duration;
                break;

            case EffectType.Stun:
                if (_stunTimer > 0f) break; // ya está aturdido, ignorar
                if (Random.value > effect.Chance) break; // falló la probabilidad
                _stunTimer = effect.Value; // Value = duración en segundos
                if (_aiPath != null)
                {
                    _preStunSpeed    = _aiPath.maxSpeed;
                    _aiPath.maxSpeed = 0f;
                }
                break;

            case EffectType.LightningStrike:
                ShowLightningVfx();
                break;
        }
    }

    /// <summary>
    /// Clears all active effects. Called by EnemyPool when returning an enemy to the pool.
    /// </summary>
    public void ClearEffects()
    {
        HideBurnVfx();
        HideLightningVfx();
        _burnDps             = 0f;
        _burnTimer           = 0f;
        _slowAreaValue       = 0f;
        _slowAreaTimer       = 0f;
        _armorReductionValue = 0f;
        _armorReductionTimer = 0f;
        _slowSources.Clear();

        _poisonStacks    = 0;
        _poisonDps       = 0f;
        _poisonMaxStacks = 0;
        _poisonTimer     = 0f;

        _maldicionValue = 0f;
        _maldicionTimer = 0f;

        if (_stunTimer > 0f && _aiPath != null)
            _aiPath.maxSpeed = _preStunSpeed;
        _stunTimer    = 0f;
        _preStunSpeed = 0f;
    }

    // ── Burn VFX helpers ─────────────────────────────────────────────────────

    private void ShowBurnVfx()
    {
        if (_burnFrames == null || _burnFrames.Length == 0 || _burnVfxGO != null) return;

        _parentSR = GetComponent<SpriteRenderer>();

        // Scale fire to match enemy sprite dimensions
        float enemyW = _parentSR != null ? _parentSR.bounds.size.x : 1f;
        float enemyH = _parentSR != null ? _parentSR.bounds.size.y : 1f;

        _burnVfxGO = new GameObject("BurnVFX");
        _burnVfxGO.transform.SetParent(transform, false);
        _burnVfxGO.transform.localScale    = new Vector3(enemyW, enemyH, 1f);
        // bounds.min.y es la base real del sprite (pies), independiente del pivot.
        // localFeetY convierte eso a espacio local del enemy GO.
        // Sumamos enemyH/2 porque el pivot del fuego está al centro del sprite.
        float localFeetY = _parentSR.bounds.min.y - transform.position.y;
        _burnVfxGO.transform.localPosition = new Vector3(0f, localFeetY + enemyH * 0.5f, 0f);

        _burnVfxSR                = _burnVfxGO.AddComponent<SpriteRenderer>();
        _burnVfxSR.sprite         = _burnFrames[0];
        _burnVfxSR.sharedMaterial = AdditiveMat;
        _burnVfxSR.sortingLayerID = _parentSR != null ? _parentSR.sortingLayerID : 0;
        _burnVfxSR.sortingOrder   = _parentSR != null ? _parentSR.sortingOrder + 1 : 10;

        _burnAnim = StartCoroutine(AnimateBurnVfx());
    }

    private void HideBurnVfx()
    {
        if (_burnAnim  != null) { StopCoroutine(_burnAnim);  _burnAnim  = null; }
        if (_burnVfxGO != null) { Destroy(_burnVfxGO);       _burnVfxGO = null; _burnVfxSR = null; }
    }

    private IEnumerator AnimateBurnVfx()
    {
        var wait = new WaitForSeconds(1f / BurnAnimFps);
        int f = 0;
        while (true)
        {
            _burnVfxSR.sprite = _burnFrames[f % _burnFrames.Length];
            f++;
            yield return wait;
        }
    }

    // ── Lightning VFX helpers ────────────────────────────────────────────────

    private void ShowLightningVfx()
    {
        if (_lightningFrames == null || _lightningFrames.Length == 0) return;

        if (_parentSR == null) _parentSR = GetComponent<SpriteRenderer>();

        // Reinicia la corrutina si ya hay una corriendo (retrigger en el mismo enemigo)
        if (_lightningAnim != null)
        {
            StopCoroutine(_lightningAnim);
            _lightningAnim = null;
        }

        // Crea el GO hijo si no existe todavía
        if (_lightningVfxGO == null)
        {
            _lightningVfxGO = new GameObject("LightningVFX");
            _lightningVfxGO.transform.SetParent(transform, false);

            _lightningVfxSR               = _lightningVfxGO.AddComponent<SpriteRenderer>();
            _lightningVfxSR.sharedMaterial = AdditiveMat;
            // Capa "Effects" (sobre TowerTop) → siempre visible encima de torres
            _lightningVfxSR.sortingLayerID = SortingLayer.NameToID("Effects");
            _lightningVfxSR.sortingOrder   = Mathf.RoundToInt(-transform.position.y * 100) + 2;
        }

        // Sprite: 64×128 px a 100 PPU = 0.64u × 1.28u. Pivot en centro (0.5,0.5).
        // Base del sprite en los pies del enemigo → centro Y = pies + 0.64 * scaleY
        float localFeetY = _parentSR != null
            ? _parentSR.bounds.min.y - transform.position.y
            : -0.5f;
        _lightningVfxGO.transform.localScale    = new Vector3(1f, _lightningScaleY, 1f);
        _lightningVfxGO.transform.localPosition = new Vector3(0f, localFeetY + 0.64f * _lightningScaleY, 0f);

        _lightningVfxSR.sprite = _lightningFrames[0];
        _lightningAnim = StartCoroutine(AnimateLightningVfx());
    }

    private void HideLightningVfx()
    {
        if (_lightningAnim  != null) { StopCoroutine(_lightningAnim);  _lightningAnim  = null; }
        if (_lightningVfxGO != null) { Destroy(_lightningVfxGO);       _lightningVfxGO = null; _lightningVfxSR = null; }
    }

    private IEnumerator AnimateLightningVfx()
    {
        var wait = new WaitForSeconds(1f / LightningAnimFps);
        for (int f = 0; f < _lightningFrames.Length; f++)
        {
            if (_lightningVfxSR != null) _lightningVfxSR.sprite = _lightningFrames[f];
            yield return wait;
        }
        // One-shot: destruye el GO al terminar
        _lightningAnim = null;
        if (_lightningVfxGO != null) { Destroy(_lightningVfxGO); _lightningVfxGO = null; _lightningVfxSR = null; }
    }

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _aiPath = GetComponent<Pathfinding.AIPath>();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (_burnTimer > 0f)
        {
            _burnTimer -= dt;
            if (_burnTimer <= 0f) HideBurnVfx();
            else if (_burnVfxSR != null && _parentSR != null)
                _burnVfxSR.sortingOrder = _parentSR.sortingOrder + 1;
        }
        if (_lightningVfxSR != null)
            _lightningVfxSR.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100) + 2;
        if (_slowAreaTimer        > 0f) _slowAreaTimer        -= dt;
        if (_armorReductionTimer  > 0f) _armorReductionTimer  -= dt;

        for (int i = _slowSources.Count - 1; i >= 0; i--)
        {
            SlowInstance s = _slowSources[i];
            s.TimeRemaining -= dt;
            if (s.TimeRemaining <= 0f) _slowSources.RemoveAt(i);
            else _slowSources[i] = s;
        }

        if (_poisonTimer > 0f)
        {
            _poisonTimer -= dt;
            if (_poisonTimer <= 0f)
            {
                _poisonStacks = 0;
                _poisonDps    = 0f;
            }
        }

        if (_maldicionTimer > 0f)
        {
            _maldicionTimer -= dt;
            if (_maldicionTimer <= 0f) _maldicionValue = 0f;
        }

        if (_stunTimer > 0f)
        {
            _stunTimer -= dt;
            if (_stunTimer <= 0f && _aiPath != null)
                _aiPath.maxSpeed = _preStunSpeed;
        }
    }
}
