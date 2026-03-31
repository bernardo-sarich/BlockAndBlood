using UnityEngine;

/// <summary>
/// Pairs a CardData reference with a remaining charge count.
/// Stored in PlayerInventory instead of raw CardData so multiple applications
/// of the same card stack on a single inventory slot.
/// </summary>
[System.Serializable]
public struct CardSlot
{
    public CardData Card;
    public int      Charges;

    /// <summary>Creates a slot with the card's default MaxCharges.</summary>
    public CardSlot(CardData card)
    {
        Card    = card;
        Charges = Mathf.Max(1, card.MaxCharges);
    }

    /// <summary>Creates a slot with an explicit charge count (for testing / special cases).</summary>
    public CardSlot(CardData card, int charges)
    {
        Card    = card;
        Charges = charges;
    }

    public bool IsValid => Card != null && Charges > 0;
}
