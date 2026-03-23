using UnityEngine;

/// <summary>
/// Implemented by any entity that can be selected by the player (hero, tower).
/// SelectionManager reads this interface to position the selection indicator
/// and drive HUD visibility.
/// </summary>
public interface ISelectable
{
    /// <summary>World-space transform used to position the selection ellipse.</summary>
    Transform SelectionTransform { get; }

    /// <summary>False while the entity cannot be selected (e.g. tower still building).</summary>
    bool IsSelectable { get; }
}
