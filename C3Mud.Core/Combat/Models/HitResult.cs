namespace C3Mud.Core.Combat.Models;

/// <summary>
/// Result of a hit/miss calculation using THAC0 system
/// </summary>
public class HitResult
{
    /// <summary>
    /// Whether the attack hit the target
    /// </summary>
    public bool IsHit { get; set; }

    /// <summary>
    /// Whether this was a critical hit (natural 20)
    /// </summary>
    public bool IsCriticalHit { get; set; }

    /// <summary>
    /// Whether this was a critical miss (natural 1)
    /// </summary>
    public bool IsCriticalMiss { get; set; }

    /// <summary>
    /// The attacker's THAC0 value
    /// </summary>
    public int AttackerTHAC0 { get; set; }

    /// <summary>
    /// Bonus to hit from strength
    /// </summary>
    public int StrengthBonus { get; set; }

    /// <summary>
    /// The d20 roll result
    /// </summary>
    public int DiceRoll { get; set; }

    /// <summary>
    /// Target's armor class
    /// </summary>
    public int TargetArmorClass { get; set; }

    /// <summary>
    /// Required roll to hit (THAC0 - AC)
    /// </summary>
    public int RequiredRoll { get; set; }
}