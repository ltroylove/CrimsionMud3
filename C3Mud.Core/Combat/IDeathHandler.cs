using C3Mud.Core.Players;
using C3Mud.Core.World.Models;

namespace C3Mud.Core.Combat;

/// <summary>
/// Interface for handling player death and resurrection
/// </summary>
public interface IDeathHandler
{
    /// <summary>
    /// Process a player's death - create corpse, handle inventory, apply penalties
    /// </summary>
    /// <param name="player">Player who died</param>
    Task ProcessDeathAsync(IPlayer player);

    /// <summary>
    /// Resurrect a dead player with appropriate penalties
    /// </summary>
    /// <param name="player">Player to resurrect</param>
    Task ResurrectPlayerAsync(IPlayer player);

    /// <summary>
    /// Process corpse decay for a room
    /// </summary>
    /// <param name="room">Room to process corpse decay in</param>
    Task ProcessCorpseDecayAsync(Room room);
}