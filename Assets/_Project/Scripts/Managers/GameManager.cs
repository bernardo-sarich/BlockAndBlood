using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Central game manager. Owns the GameState machine and the 30-second preparation countdown.
///
/// State transitions:
///   Preparation → Playing    : countdown reaches zero (after 30 s)
///   Playing     → Paused     : XPManager.OnLevelUp (card-pick pause)
///   Paused      → Playing    : CardSystem.OnCardChosen
///   Playing     → Defeat     : LivesManager.OnGameOver
///   Playing     → Victory    : BossBehaviour.OnBossDefeated
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Inspector references ──────────────────────────────────────────────
    [Header("References")]
    public GridManager           GridManager;
    public EconomyManager        EconomyManager;
    public TowerPlacementManager TowerPlacementManager;
    public LivesManager          LivesManager;

    [Header("Preparation Phase")]
    [SerializeField] private float _prepDuration  = 5f;
    [SerializeField] private int   _prepGoldBonus = 50;

    // ── Game State ────────────────────────────────────────────────────────
    public enum GameState { Preparation, Playing, Paused, Victory, Defeat }

    /// <summary>Fired whenever the game state changes. Passes the new state.</summary>
    public static event Action<GameState> OnGameStateChanged;

    public GameState CurrentState { get; private set; }

    // ── Countdown UI (procedural) ─────────────────────────────────────────
    private TextMeshProUGUI _countdownText;
    private float           _prepTimeLeft;

    // ── Unity lifecycle ───────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // CurrentState is Preparation by default (first enum value = 0)
        _prepTimeLeft = _prepDuration;
        BuildCountdownUI();
    }

    private void Start()
    {
        // All managers have completed Awake by now — safe to call Add
        EconomyManager.Instance?.Add(_prepGoldBonus);
        OnGameStateChanged?.Invoke(GameState.Preparation);
    }

    private void OnEnable()
    {
        LivesManager.OnGameOver      += HandleGameOver;
        XPManager.OnLevelUp          += HandleLevelUp;
        CardSystem.OnCardChosen      += HandleCardChosen;
        BossBehaviour.OnBossDefeated += HandleBossDefeated;
    }

    private void OnDisable()
    {
        LivesManager.OnGameOver      -= HandleGameOver;
        XPManager.OnLevelUp          -= HandleLevelUp;
        CardSystem.OnCardChosen      -= HandleCardChosen;
        BossBehaviour.OnBossDefeated -= HandleBossDefeated;
    }

    private void Update()
    {
        if (CurrentState != GameState.Preparation) return;

        _prepTimeLeft -= Time.deltaTime;

        if (_countdownText != null)
            _countdownText.text = $"Preparación: {Mathf.CeilToInt(Mathf.Max(0f, _prepTimeLeft))}s";

        if (_prepTimeLeft <= 0f)
            TransitionTo(GameState.Playing);
    }

    // ── State machine ─────────────────────────────────────────────────────

    private void TransitionTo(GameState next)
    {
        if (CurrentState == next) return;
        CurrentState = next;
        Debug.Log($"[GameManager] Estado → {next}");

        switch (next)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                if (_countdownText != null)
                    _countdownText.gameObject.SetActive(false);
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                break;

            case GameState.Defeat:
            case GameState.Victory:
                Time.timeScale = 1f;
                break;
        }

        OnGameStateChanged?.Invoke(next);
    }

    // ── Event handlers ────────────────────────────────────────────────────

    private void HandleGameOver()
    {
        if (CurrentState == GameState.Playing)
            TransitionTo(GameState.Defeat);
    }

    private void HandleLevelUp()
    {
        if (CurrentState == GameState.Playing)
            TransitionTo(GameState.Paused);
    }

    private void HandleCardChosen()
    {
        if (CurrentState == GameState.Paused)
            TransitionTo(GameState.Playing);
    }

    private void HandleBossDefeated()
    {
        if (CurrentState == GameState.Playing)
            TransitionTo(GameState.Victory);
    }

    // ── Countdown UI ──────────────────────────────────────────────────────

    private void BuildCountdownUI()
    {
        var canvasGo = new GameObject("PrepCountdownCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var textGo = new GameObject("CountdownText");
        textGo.transform.SetParent(canvasGo.transform, false);

        var rt = textGo.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(400f, 80f);
        rt.anchoredPosition = Vector2.zero;

        _countdownText           = textGo.AddComponent<TextMeshProUGUI>();
        _countdownText.alignment = TextAlignmentOptions.Center;
        _countdownText.fontSize  = 40f;
        _countdownText.color     = Color.white;
        _countdownText.text      = $"Preparación: {Mathf.CeilToInt(_prepDuration)}s";
    }
}
