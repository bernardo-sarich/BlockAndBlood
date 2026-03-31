using UnityEngine;

/// <summary>
/// Accumulates XP from killed enemies and fires OnLevelUp at each threshold.
/// Umbrales: 100 XP por nivel, máximo nivel 15 (~1500 XP total).
/// Ritmo esperado: jugador eficiente sube ~1 nivel/minuto.
/// Jugadores que dejan pasar enemigos suben más lento y llegan
/// al boss con menos cartas — penalización implícita de diseño.
/// </summary>
public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }

    /// <summary>Fired with new total XP whenever XP is gained.</summary>
    public static event System.Action<int> OnXpChanged;

    /// <summary>Fired once at each XP threshold. GameManager pauses the game on this event.</summary>
    public static event System.Action OnLevelUp;

    public const int MaxLevel       = 15;
    public const int XpPerLevel     = 100;

    public int CurrentXp    { get; private set; }
    public int CurrentLevel { get; private set; } // 0 = no level-up yet; max = MaxLevel

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        EnemyBehaviour.OnEnemyDeath += HandleEnemyDeath;
    }

    private void OnDisable()
    {
        EnemyBehaviour.OnEnemyDeath -= HandleEnemyDeath;
    }

    private void HandleEnemyDeath(EnemyBehaviour _, int gold, int xp)
    {
        if (CurrentLevel >= MaxLevel) return;

        CurrentXp += xp;
        OnXpChanged?.Invoke(CurrentXp);

        if (CurrentXp >= (CurrentLevel + 1) * XpPerLevel)
        {
            CurrentLevel++;
            OnLevelUp?.Invoke();
        }
    }

    // ── Rarity helper ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns a weighted-random CardRarity based on the player's current level.
    /// Levels 1–5:   80% Common, 20% Rare,  0% Epic.
    /// Levels 6–10:  50% Common, 40% Rare, 10% Epic.
    /// Levels 11–15: 20% Common, 50% Rare, 30% Epic.
    /// </summary>
    public static CardRarity GetRarityForCurrentLevel()
    {
        int level = Instance != null ? Instance.CurrentLevel : 0;
        float roll = UnityEngine.Random.value; // [0, 1)

        if (level <= 5)
        {
            // 80% Common, 20% Rare
            return roll < 0.80f ? CardRarity.Common : CardRarity.Rare;
        }
        else if (level <= 10)
        {
            // 50% Common, 40% Rare, 10% Epic
            if (roll < 0.50f) return CardRarity.Common;
            if (roll < 0.90f) return CardRarity.Rare;
            return CardRarity.Epic;
        }
        else
        {
            // 20% Common, 50% Rare, 30% Epic
            if (roll < 0.20f) return CardRarity.Common;
            if (roll < 0.70f) return CardRarity.Rare;
            return CardRarity.Epic;
        }
    }
}
