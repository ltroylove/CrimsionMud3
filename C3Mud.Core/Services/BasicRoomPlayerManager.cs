using C3Mud.Core.Players;
using System.Collections.Concurrent;

namespace C3Mud.Core.Services;

/// <summary>
/// Basic implementation of room player management
/// Uses in-memory concurrent collections for thread-safe operations
/// This is a simplified version for command integration - advanced features later
/// </summary>
public class BasicRoomPlayerManager : IRoomPlayerManager
{
    private readonly ConcurrentDictionary<int, ConcurrentBag<IPlayer>> _roomPlayers;

    public BasicRoomPlayerManager()
    {
        _roomPlayers = new ConcurrentDictionary<int, ConcurrentBag<IPlayer>>();
    }

    public IEnumerable<IPlayer> GetPlayersInRoom(int roomVnum)
    {
        if (_roomPlayers.TryGetValue(roomVnum, out var players))
        {
            // Filter out disconnected players and return active ones
            return players.Where(p => p.IsConnected).ToList();
        }
        
        return Enumerable.Empty<IPlayer>();
    }

    public void AddPlayerToRoom(IPlayer player, int roomVnum)
    {
        _roomPlayers.AddOrUpdate(roomVnum, 
            new ConcurrentBag<IPlayer> { player },
            (key, existingPlayers) =>
            {
                // Remove player from this room first (in case they're already there)
                var newBag = new ConcurrentBag<IPlayer>(
                    existingPlayers.Where(p => !p.Id.Equals(player.Id, StringComparison.OrdinalIgnoreCase))
                );
                newBag.Add(player);
                return newBag;
            });
        
        // Update player's current room
        player.CurrentRoomVnum = roomVnum;
    }

    public void RemovePlayerFromRoom(IPlayer player, int roomVnum)
    {
        if (_roomPlayers.TryGetValue(roomVnum, out var players))
        {
            // Create new bag without the specified player
            var newBag = new ConcurrentBag<IPlayer>(
                players.Where(p => !p.Id.Equals(player.Id, StringComparison.OrdinalIgnoreCase))
            );
            
            _roomPlayers.TryUpdate(roomVnum, newBag, players);
        }
    }

    public void MovePlayerToRoom(IPlayer player, int fromRoomVnum, int toRoomVnum)
    {
        RemovePlayerFromRoom(player, fromRoomVnum);
        AddPlayerToRoom(player, toRoomVnum);
    }
}