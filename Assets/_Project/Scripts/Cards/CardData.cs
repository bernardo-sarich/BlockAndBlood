using UnityEngine;

/// <summary>
/// ScriptableObject definition for a single card.
/// Cards are collected by the player (max 6) and can be permanently applied to towers.
/// </summary>
[CreateAssetMenu(menuName = "Block&Blood/CardData", fileName = "NewCardData")]
public class CardData : ScriptableObject
{
    public enum Rarity { Common, Rare, Epic }

    [Header("Identity")]
    public string CardName;
    [TextArea(2, 4)]
    public string Description;
    public Rarity CardRarity;

    [Header("Visuals")]
    public Sprite Icon;

    [Header("Compatibility")]
    /// <summary>
    /// Tower types this card can be applied to. Empty array = compatible with all tower types.
    /// </summary>
    public TowerType[] CompatibleTowerTypes;

    /// <summary>Returns true if this card can be applied to the given tower type.</summary>
    public bool IsCompatibleWith(TowerType type)
    {
        if (CompatibleTowerTypes == null || CompatibleTowerTypes.Length == 0)
            return true;

        for (int i = 0; i < CompatibleTowerTypes.Length; i++)
            if (CompatibleTowerTypes[i] == type) return true;

        return false;
    }
}
