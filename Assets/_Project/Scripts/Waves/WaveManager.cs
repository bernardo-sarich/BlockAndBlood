using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives continuous enemy spawning according to time-based WavePhase ScriptableObjects.
///
/// Rules:
///   • Only spawns when GameState == Playing.
///   • Phase is selected by elapsed playing time (paused time is excluded).
///   • Enemy type chosen probabilistically from the active phase's Composition weights.
///   • Rápido: never two consecutive Rápido spawns (re-roll once; forced non-Rápido if still Rápido).
///   • Blindado: always preceded by 3 Caminantes — schedules [Cam, Cam, Cam, Blindado] in a queue.
///   • OnLevelUp  → pause spawn coroutine (active enemies stay alive).
///   • OnCardChosen → resume spawn coroutine.
/// </summary>
public class WaveManager : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────
    [Header("Wave Data")]
    [SerializeField] private WavePhase[] _phases;

    [Header("Special-Rule Enemy References")]
    [Tooltip("Spawned as precursor ×3 before every Blindado.")]
    [SerializeField] private EnemyData _caminanteData;

    [Tooltip("Prevents consecutive Rápido spawns.")]
    [SerializeField] private EnemyData _rapidoData;

    [Tooltip("Triggers the 3-Caminante precursor queue.")]
    [SerializeField] private EnemyData _blindadoData;

    // ── Events ───────────────────────────────────────────────────────────
    /// <summary>Fired once when PlayingElapsed reaches 840 s — normal wave stream ends, boss should spawn.</summary>
    public static event System.Action OnBossPhaseStart;

    // ── Runtime state ────────────────────────────────────────────────────
    private Coroutine          _spawnCoroutine;
    private Queue<EnemyData>   _spawnQueue    = new Queue<EnemyData>();
    private EnemyData          _lastSpawned;
    private bool               _bossTriggered;

    // Pure playing-time tracking (pause-safe)
    // PlayingElapsed measures seconds since Playing state first began,
    // excluding time spent paused. WavePhase.StartTime/EndTime are expressed
    // in these same Playing-relative seconds.
    private float _playingStartTime;   // Time.time when Playing first began
    private float _totalPausedTime;    // Accumulated pause duration
    private float _pauseEnteredTime;   // Time.time when last pause began
    private bool  _playingEverStarted;

    /// <summary>Seconds of active playing time, excluding pauses.</summary>
    private float PlayingElapsed =>
        _playingEverStarted
            ? Time.time - _playingStartTime - _totalPausedTime
            : 0f;

    // ── Unity lifecycle ──────────────────────────────────────────────────

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleStateChanged;
        StopSpawning();
    }

    // ── State handling ───────────────────────────────────────────────────

    private void HandleStateChanged(GameManager.GameState state)
    {
        Debug.Log($"[WaveManager] Recibió estado: {state}");
        switch (state)
        {
            case GameManager.GameState.Playing:
                if (!_playingEverStarted)
                {
                    _playingStartTime   = Time.time;
                    _playingEverStarted = true;
                    _bossTriggered      = false;
                }
                else
                {
                    // Resuming from pause — account for paused duration
                    _totalPausedTime += Time.time - _pauseEnteredTime;
                }
                StartSpawning();
                break;

            case GameManager.GameState.Paused:
                _pauseEnteredTime = Time.time;
                StopSpawning();
                break;

            case GameManager.GameState.Defeat:
            case GameManager.GameState.Victory:
                StopSpawning();
                _spawnQueue.Clear();
                break;
        }
    }

    private void StartSpawning()
    {
        Debug.Log($"[WaveManager] Iniciando stream de enemigos (PlayingElapsed={PlayingElapsed:F1}s)");
        if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
        _spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    private void StopSpawning()
    {
        if (_spawnCoroutine == null) return;
        StopCoroutine(_spawnCoroutine);
        _spawnCoroutine = null;
    }

    // ── Spawn coroutine ──────────────────────────────────────────────────

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            // Boss gate — after 840 s of playing time the normal wave stream ends
            if (!_bossTriggered && PlayingElapsed >= 840f)
            {
                _bossTriggered = true;
                Debug.Log("[WaveManager] 840 s reached — triggering boss phase.");
                OnBossPhaseStart?.Invoke();
                yield break;
            }

            Debug.Log($"[SpawnLoop] tick — elapsed={PlayingElapsed:F1}s phases={_phases?.Length} queue={_spawnQueue.Count}");

            // Drain the scheduled queue first (Blindado precursors)
            if (_spawnQueue.Count > 0)
            {
                SpawnNext(_spawnQueue.Dequeue());
            }
            else
            {
                WavePhase phase = GetActivePhase();
                Debug.Log($"[SpawnLoop] activePhase={phase?.name ?? "NULL"}");
                if (phase != null)
                {
                    EnemyData selected = SelectEnemy(phase);
                    Debug.Log($"[SpawnLoop] selected={selected?.EnemyName ?? "NULL"}");
                    if (selected != null)
                        ScheduleEnemy(selected);
                    // If queue now has items, spawn the first one immediately this tick
                    if (_spawnQueue.Count > 0)
                        SpawnNext(_spawnQueue.Dequeue());
                }
                // No active phase yet (grace period before StartTime of first phase) — just wait
            }

            float interval = GetCurrentInterval();
            yield return new WaitForSeconds(interval);
        }
    }

    // ── Phase / enemy selection ──────────────────────────────────────────

    private WavePhase GetActivePhase()
    {
        float elapsed = PlayingElapsed;
        for (int i = 0; i < _phases.Length; i++)
        {
            WavePhase p = _phases[i];
            if (elapsed >= p.StartTime && elapsed < p.EndTime)
                return p;
        }
        return null;
    }

    private float GetCurrentInterval()
    {
        WavePhase phase = GetActivePhase();
        return phase != null ? phase.SpawnInterval : 1f; // 1 s fallback during grace period
    }

    /// <summary>Weighted random selection from a phase composition.</summary>
    private EnemyData SelectEnemy(WavePhase phase)
    {
        if (phase.Composition == null || phase.Composition.Length == 0) return null;

        // Sum total weight
        float total = 0f;
        for (int i = 0; i < phase.Composition.Length; i++)
            total += phase.Composition[i].Weight;

        if (total <= 0f) return phase.Composition[0].Data;

        // First pick
        EnemyData pick = WeightedPick(phase, total);

        // Rápido rule: no two consecutive Rápidos
        if (pick == _rapidoData && _lastSpawned == _rapidoData)
        {
            // Re-roll once
            pick = WeightedPick(phase, total);
            // If still Rápido, force the first non-Rápido entry
            if (pick == _rapidoData)
            {
                foreach (var entry in phase.Composition)
                {
                    if (entry.Data != _rapidoData && entry.Data != null)
                    {
                        pick = entry.Data;
                        break;
                    }
                }
            }
        }

        return pick;
    }

    private EnemyData WeightedPick(WavePhase phase, float total)
    {
        float roll = Random.Range(0f, total);
        float cumulative = 0f;
        for (int i = 0; i < phase.Composition.Length; i++)
        {
            cumulative += phase.Composition[i].Weight;
            if (roll < cumulative)
                return phase.Composition[i].Data;
        }
        return phase.Composition[phase.Composition.Length - 1].Data;
    }

    /// <summary>
    /// Enqueues an enemy, applying special spawn rules:
    ///   Blindado → enqueues [Caminante × 3, Blindado].
    /// </summary>
    private void ScheduleEnemy(EnemyData data)
    {
        if (data == _blindadoData && _caminanteData != null)
        {
            _spawnQueue.Enqueue(_caminanteData);
            _spawnQueue.Enqueue(_caminanteData);
            _spawnQueue.Enqueue(_caminanteData);
        }
        _spawnQueue.Enqueue(data);
    }

    // ── Actual spawn ─────────────────────────────────────────────────────

    private void SpawnNext(EnemyData data)
    {
        Debug.Log($"[SpawnNext] data={data?.EnemyName ?? "NULL"} pool={EnemyPool.Instance != null} grid={GridManager.Instance != null}");
        if (data == null || EnemyPool.Instance == null || GridManager.Instance == null) return;

        int col = Random.Range(GridVisualizer.PathColMin, GridVisualizer.PathColMax + 1);
        Vector3 spawnPos = GridManager.Instance.CellToWorld(new Vector2Int(col, GridManager.SpawnRow));
        Vector3 goalPos  = GridManager.Instance.CellToWorld(new Vector2Int(col, GridManager.GoalRow));
        spawnPos.z = 0f;
        goalPos.z  = 0f;

        Debug.Log($"[SpawnNext] spawning {data.EnemyName} at {spawnPos} → {goalPos}");
        var enemy = EnemyPool.Instance.Spawn(data, spawnPos, goalPos);
        Debug.Log($"[SpawnNext] enemy instance={enemy?.name ?? "NULL"}");
        _lastSpawned = data;
    }
}
