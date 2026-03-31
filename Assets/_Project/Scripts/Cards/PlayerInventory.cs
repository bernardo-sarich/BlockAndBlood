using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that manages the player's card inventory (max 6 unique card types).
/// Each slot is a CardSlot (CardData + charge count). Picking the same card again
/// stacks its charges onto the existing slot instead of using a new slot.
/// Cards are gained from XP level-ups and spent (one charge at a time) by applying them to towers.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    /// <summary>Maximum number of distinct card types in the inventory.</summary>
    public const int MaxCards = 6;

    /// <summary>Fired whenever the inventory changes (card added or charge spent).</summary>
    public static event System.Action OnInventoryChanged;

    private readonly List<CardSlot> _slots = new List<CardSlot>(MaxCards);

    public IReadOnlyList<CardSlot> Slots => _slots;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    /// <summary>
    /// Adds a card to the inventory.
    /// If a slot for this card already exists, its charges are increased by card.MaxCharges.
    /// Otherwise a new slot is created (fails if inventory is already at MaxCards unique types).
    /// Returns false only when no slot exists and the inventory is full.
    /// </summary>
    public bool AddCard(CardData card)
    {
        if (card == null) return false;

        // Stack onto existing slot first (before checking capacity).
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].Card == card)
            {
                var s = _slots[i];
                s.Charges += Mathf.Max(1, card.MaxCharges);
                _slots[i] = s;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        // New card type — check capacity.
        if (_slots.Count >= MaxCards) return false;

        _slots.Add(new CardSlot(card));
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Spends one charge of the card at the given inventory slot index, applying it to the tower.
    /// Removes the slot when charges reach zero.
    /// Returns false if the slot is invalid or not compatible with the tower.
    /// </summary>
    public bool SpendCard(int slotIndex, TowerBehaviour tower)
    {
        if (tower == null) return false;
        if (slotIndex < 0 || slotIndex >= _slots.Count) return false;

        CardSlot slot = _slots[slotIndex];
        if (!slot.IsValid) return false;
        if (!tower.CanApplyCard(slot.Card)) return false;

        tower.ApplyCard(slot.Card);

        slot.Charges--;
        if (slot.Charges <= 0)
            _slots.RemoveAt(slotIndex);
        else
            _slots[slotIndex] = slot;

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Spends one charge of the given card (searched by reference), applying it to the tower.
    /// Convenience overload — prefer SpendCard(int, TowerBehaviour) when the index is known.
    /// </summary>
    public bool SpendCard(CardData card, TowerBehaviour tower)
    {
        if (card == null || tower == null) return false;

        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].Card == card)
                return SpendCard(i, tower);
        }
        return false;
    }
}
