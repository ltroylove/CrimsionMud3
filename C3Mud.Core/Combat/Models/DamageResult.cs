namespace C3Mud.Core.Combat.Models;

/// <summary>
/// Result of damage calculation for an attack
/// </summary>
public class DamageResult
{
    /// <summary>
    /// Base damage from weapon dice roll
    /// </summary>
    public int BaseDamage { get; set; }

    /// <summary>
    /// Damage bonus from strength
    /// </summary>
    public int StrengthBonus { get; set; }

    /// <summary>
    /// Bonus damage from magic weapon
    /// </summary>
    public int WeaponBonus { get; set; }

    /// <summary>
    /// Total damage dealt
    /// </summary>
    public int TotalDamage { get; set; }

    /// <summary>
    /// Whether this was a critical hit (double damage)
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// Name of the weapon used
    /// </summary>
    public string WeaponName { get; set; } = string.Empty;

    /// <summary>
    /// Type of damage (slashing, piercing, bludgeoning, etc.)
    /// </summary>
    public string DamageType { get; set; } = "physical";
}