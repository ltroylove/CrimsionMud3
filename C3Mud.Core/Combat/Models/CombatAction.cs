namespace C3Mud.Core.Combat.Models;

/// <summary>
/// Represents an action taken during combat
/// </summary>
public class CombatAction
{
    /// <summary>
    /// Type of action being performed
    /// </summary>
    public CombatActionType ActionType { get; set; }

    /// <summary>
    /// Target of the action (if applicable)
    /// </summary>
    public ICombatant? Target { get; set; }

    /// <summary>
    /// Additional data for the action
    /// </summary>
    public object? ActionData { get; set; }
}

/// <summary>
/// Types of combat actions
/// </summary>
public enum CombatActionType
{
    /// <summary>
    /// Standard melee attack
    /// </summary>
    Attack,

    /// <summary>
    /// Attempt to flee from combat
    /// </summary>
    Flee,

    /// <summary>
    /// Bash attack with shield
    /// </summary>
    Bash,

    /// <summary>
    /// Kick attack
    /// </summary>
    Kick,

    /// <summary>
    /// Cast a spell
    /// </summary>
    CastSpell,

    /// <summary>
    /// Do nothing this round
    /// </summary>
    Wait
}