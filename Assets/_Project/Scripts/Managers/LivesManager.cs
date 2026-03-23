using UnityEngine;

/// <summary>
/// Tracks the player's remaining lives.
/// Each enemy that reaches the goal costs one life.
/// Fires OnLivesChanged so HUD can react, and OnGameOver when lives hit zero.
/// </summary>
public class LivesManager : MonoBehaviour
{
    public static LivesManager Instance { get; private set; }

    /// <summary>Fired with the new lives total whenever a life is lost.</summary>
    public static event System.Action<int> OnLivesChanged;

    /// <summary>Fired once when lives reach zero.</summary>
    public static event System.Action OnGameOver;

    [Header("Config")]
    [SerializeField] private int _startingLives = 5;

    public int Lives { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Lives = _startingLives;
    }

    private void OnEnable()
    {
        EnemyBehaviour.OnEnemyReachedGoal += HandleEnemyReachedGoal;
    }

    private void OnDisable()
    {
        EnemyBehaviour.OnEnemyReachedGoal -= HandleEnemyReachedGoal;
    }

    private void HandleEnemyReachedGoal(EnemyBehaviour enemy)
    {
        if (Lives <= 0) return;

        Lives--;
        OnLivesChanged?.Invoke(Lives);

        if (Lives <= 0)
            OnGameOver?.Invoke();
    }
}
