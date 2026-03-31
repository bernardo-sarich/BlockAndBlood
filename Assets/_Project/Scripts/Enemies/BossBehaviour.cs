using UnityEngine;

/// <summary>
/// Stub — full Boss AI (Troll Anciano) will be implemented in a future task.
/// GameManager listens to OnBossDefeated to trigger the Victory state.
/// </summary>
public class BossBehaviour : MonoBehaviour
{
    /// <summary>Fired when the boss reaches 0 HP. Triggers GameState.Victory.</summary>
    public static event System.Action OnBossDefeated;

    /// <summary>Call this when boss HP hits zero (placeholder until full implementation).</summary>
    public static void NotifyDefeated() => OnBossDefeated?.Invoke();
}
