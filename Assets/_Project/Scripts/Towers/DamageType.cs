/// <summary>
/// Classifies tower attack types for damage and armor calculations.
/// Physical: reduced by armor. Fire: ignores armor (burn DoT). Water: applies slow + armor reduction.
/// </summary>
public enum DamageType
{
    Physical,
    Fire,
    Water,
}
