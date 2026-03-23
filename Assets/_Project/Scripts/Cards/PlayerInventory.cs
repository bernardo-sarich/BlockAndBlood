using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that manages the player's card inventory (max 6 cards).
/// Cards are gained from XP level-ups and spent by applying them to towers.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    public const int MaxCards = 6;

    /// <summary>Fired whenever the inventory changes (card added or spent).</summary>
    public static event System.Action OnInventoryChanged;

    private readonly List<CardData> _cards = new List<CardData>(MaxCards);

    public IReadOnlyList<CardData> Cards => _cards;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    /// <summary>
    /// Adds a card to the inventory. Returns false if inventory is full.
    /// </summary>
    public bool AddCard(CardData card)
    {
        if (card == null || _cards.Count >= MaxCards) return false;
        _cards.Add(card);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Removes a card from the inventory and applies it to the tower.
    /// Returns false if the card is not in inventory or not compatible.
    /// </summary>
    public bool SpendCard(CardData card, TowerBehaviour tower)
    {
        if (card == null || tower == null) return false;
        if (!_cards.Contains(card)) return false;
        if (!tower.CanApplyCard(card)) return false;

        _cards.Remove(card);
        tower.ApplyCard(card);
        OnInventoryChanged?.Invoke();
        return true;
    }
}
