namespace C3Mud.Core.Combat.Models;

/// <summary>
/// Result of a complete combat round
/// </summary>
public class CombatRoundResult
{
    /// <summary>
    /// Round number
    /// </summary>
    public int RoundNumber { get; set; }

    /// <summary>
    /// Order in which combatants acted (by initiative)
    /// </summary>
    public List<ICombatant> InitiativeOrder { get; set; } = new();

    /// <summary>
    /// Results of all attacks made this round
    /// </summary>
    public List<AttackResult> AttackResults { get; set; } = new();

    /// <summary>
    /// Whether combat ended this round
    /// </summary>
    public bool CombatEnded { get; set; }

    /// <summary>
    /// Winner of the combat (if it ended)
    /// </summary>
    public ICombatant? Winner { get; set; }
}

/// <summary>
/// Result of a single attack
/// </summary>
public class AttackResult
{
    /// <summary>
    /// Attacker
    /// </summary>
    public ICombatant Attacker { get; set; } = null!;

    /// <summary>
    /// Target of the attack
    /// </summary>
    public ICombatant Target { get; set; } = null!;

    /// <summary>
    /// Hit calculation result
    /// </summary>
    public HitResult Hit { get; set; } = null!;

    /// <summary>
    /// Damage result (if hit)
    /// </summary>
    public DamageResult? Damage { get; set; }

    /// <summary>
    /// Message describing the attack
    /// </summary>
    public string Message { get; set; } = string.Empty;
}