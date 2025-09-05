using C3Mud.Core.Combat.Models;
using C3Mud.Core.Players;

namespace C3Mud.Core.Combat;

/// <summary>
/// Interface for the core combat engine
/// Handles combat initiation, hit/miss calculations, damage, and combat rounds
/// </summary>
public interface ICombatEngine
{
    /// <summary>
    /// Initiate combat between two players
    /// </summary>
    /// <param name="attacker">Player starting combat</param>
    /// <param name="defender">Target of the attack</param>
    /// <returns>True if combat was initiated successfully</returns>
    Task<bool> InitiateCombatAsync(IPlayer attacker, IPlayer defender);

    /// <summary>
    /// Calculate hit/miss result using THAC0 system
    /// </summary>
    /// <param name="attacker">Attacking player</param>
    /// <param name="defender">Defending player</param>
    /// <param name="forceDiceRoll">Optional forced dice roll for testing</param>
    /// <returns>Hit calculation result</returns>
    Task<HitResult> CalculateHitAsync(IPlayer attacker, IPlayer defender, int? forceDiceRoll = null);

    /// <summary>
    /// Calculate damage for a successful hit
    /// </summary>
    /// <param name="attacker">Attacking player</param>
    /// <param name="defender">Defending player</param>
    /// <param name="isCritical">Whether this is a critical hit</param>
    /// <returns>Damage calculation result</returns>
    Task<DamageResult> CalculateDamageAsync(IPlayer attacker, IPlayer defender, bool isCritical);

    /// <summary>
    /// Execute a single combat round for all combatants
    /// </summary>
    /// <param name="combatants">List of all combatants in the fight</param>
    /// <returns>Results of the combat round</returns>
    Task<CombatRoundResult> ExecuteCombatRoundAsync(List<ICombatant> combatants);

    /// <summary>
    /// Attempt to flee from combat
    /// </summary>
    /// <param name="player">Player attempting to flee</param>
    /// <returns>True if flee was successful</returns>
    Task<bool> AttemptFleeAsync(IPlayer player);
}