using C3Mud.Core.Players;

namespace C3Mud.Core.Combat.Models;

/// <summary>
/// Interface representing any entity that can participate in combat
/// </summary>
public interface ICombatant
{
    /// <summary>
    /// Name of the combatant
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Current hit points
    /// </summary>
    int HitPoints { get; }

    /// <summary>
    /// Maximum hit points
    /// </summary>
    int MaxHitPoints { get; }

    /// <summary>
    /// Combat initiative (based on dexterity)
    /// </summary>
    int Initiative { get; }

    /// <summary>
    /// Whether this combatant is alive
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// Whether this combatant can act this round
    /// </summary>
    bool CanAct { get; }

    /// <summary>
    /// Apply damage to this combatant
    /// </summary>
    /// <param name="damage">Amount of damage to apply</param>
    Task ApplyDamageAsync(int damage);

    /// <summary>
    /// Get the next action this combatant wants to perform
    /// </summary>
    /// <returns>Combat action</returns>
    Task<CombatAction> GetNextActionAsync();

    /// <summary>
    /// Current target this combatant is fighting
    /// </summary>
    ICombatant? CurrentTarget { get; set; }

    /// <summary>
    /// Set the target this combatant should attack
    /// </summary>
    /// <param name="target">The target to attack</param>
    void SetTarget(ICombatant? target);
}