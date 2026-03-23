using UnityEngine;

/// <summary>
/// Tracks the player's gold, handles spending and income.
/// Fires OnGoldChanged whenever the balance changes so HUD can react without polling.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    /// <summary>Fired with the new gold total whenever gold is gained or spent.</summary>
    public static event System.Action<int> OnGoldChanged;

    [Header("Config")]
    [SerializeField] private int _startingGold = 50;

    public int Gold { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Gold = _startingGold;
    }

    /// <summary>
    /// Attempts to deduct <paramref name="amount"/> gold.
    /// Returns false (without deducting) if the player cannot afford it.
    /// </summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (Gold < amount) return false;
        Gold -= amount;
        OnGoldChanged?.Invoke(Gold);
        return true;
    }

    /// <summary>Adds gold to the pool (enemy kill reward, etc.).</summary>
    public void Add(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }
}
