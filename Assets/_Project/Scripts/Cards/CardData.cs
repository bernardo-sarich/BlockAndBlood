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
    /// <summary>Animation frames shown in the card picker popup. If empty, Icon is shown statically.</summary>
    public Sprite[] IconFrames;

    /// <summary>Best available static icon: Icon if set, otherwise the first IconFrame.</summary>
    public Sprite DisplayIcon => Icon != null ? Icon
        : (IconFrames != null && IconFrames.Length > 0 ? IconFrames[0] : null);

    [Header("Charges")]
    /// <summary>How many times this card can be applied to towers before being consumed.</summary>
    [Min(1)] public int MaxCharges = 1;

    [Header("Effect")]
    /// <summary>Flat damage added on top of the tower's base damage (melee: per second, ranged: per projectile).</summary>
    public float BonusDamage;

    /// <summary>
    /// Status effects this card adds to the tower's on-hit pipeline.
    /// Applied every time the tower hits an enemy (melee: per tick, ranged: per projectile).
    /// </summary>
    public EffectData[] OnHitEffects;

    [Header("Compatibility")]
    /// <summary>
    /// Tower types this card can be applied to. Empty array = compatible with all tower types.
    /// </summary>
    public TowerType[] CompatibleTowerTypes;

    private void OnValidate() { if (MaxCharges < 1) MaxCharges = 1; }

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
