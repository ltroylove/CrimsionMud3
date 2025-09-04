using C3Mud.Core.Players;

namespace C3Mud.Core.Services;

/// <summary>
/// Interface for managing players in rooms
/// Provides basic room player tracking for communication commands
/// </summary>
public interface IRoomPlayerManager
{
    /// <summary>
    /// Get all players in the specified room
    /// </summary>
    /// <param name="roomVnum">Room virtual number</param>
    /// <returns>List of players in the room</returns>
    IEnumerable<IPlayer> GetPlayersInRoom(int roomVnum);
    
    /// <summary>
    /// Add a player to a room
    /// </summary>
    /// <param name="player">Player to add</param>
    /// <param name="roomVnum">Room virtual number</param>
    void AddPlayerToRoom(IPlayer player, int roomVnum);
    
    /// <summary>
    /// Remove a player from a room
    /// </summary>
    /// <param name="player">Player to remove</param>
    /// <param name="roomVnum">Room virtual number</param>
    void RemovePlayerFromRoom(IPlayer player, int roomVnum);
    
    /// <summary>
    /// Move a player from one room to another
    /// </summary>
    /// <param name="player">Player to move</param>
    /// <param name="fromRoomVnum">Source room</param>
    /// <param name="toRoomVnum">Destination room</param>
    void MovePlayerToRoom(IPlayer player, int fromRoomVnum, int toRoomVnum);
}