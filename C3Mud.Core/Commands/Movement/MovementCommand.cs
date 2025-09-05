using C3Mud.Core.Players;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;

namespace C3Mud.Core.Commands.Movement;

/// <summary>
/// Base class for all directional movement commands
/// Provides common movement logic and validation for North, South, East, West, Up, Down commands
/// Based on CircleMUD movement implementation with modern async patterns
/// </summary>
public abstract class MovementCommand : BaseCommand
{
    protected readonly IWorldDatabase _worldDatabase;
    
    /// <summary>
    /// The direction this command moves the player
    /// </summary>
    protected abstract Direction Direction { get; }

    /// <summary>
    /// Initialize movement command with world database dependency
    /// </summary>
    /// <param name="worldDatabase">World database for room lookups</param>
    public MovementCommand(IWorldDatabase worldDatabase)
    {
        _worldDatabase = worldDatabase ?? throw new ArgumentNullException(nameof(worldDatabase));
    }

    /// <summary>
    /// All movement commands require standing position
    /// </summary>
    public override PlayerPosition MinimumPosition => PlayerPosition.Standing;

    /// <summary>
    /// Execute the movement command
    /// </summary>
    /// <param name="player">Player attempting to move</param>
    /// <param name="arguments">Command arguments (ignored for basic movement)</param>
    /// <param name="commandId">Legacy command ID for compatibility</param>
    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        if (!CanExecute(player))
        {
            await SendToPlayerAsync(player, $"You can't do that while {GetPositionDescription(player.Position)}.");
            return;
        }

        var moved = await MovePlayerInDirection(player, Direction);
        if (moved)
        {
            // Show new room description after successful movement
            await ShowNewRoom(player);
        }
    }

    /// <summary>
    /// Move player in the specified direction
    /// Handles validation, room lookup, and player location updates
    /// </summary>
    /// <param name="player">Player to move</param>
    /// <param name="direction">Direction to move</param>
    /// <returns>True if movement succeeded, false otherwise</returns>
    protected async Task<bool> MovePlayerInDirection(IPlayer player, Direction direction)
    {
        // Get current room
        var currentRoomVnum = player.CurrentRoomVnum; // Read the property
        var currentRoom = _worldDatabase.GetRoom(currentRoomVnum);
        if (currentRoom == null)
        {
            await SendToPlayerAsync(player, "You are lost in the void!");
            return false;
        }

        // Check if exit exists in current room
        var exit = currentRoom.GetExit(direction);
        if (exit == null)
        {
            await SendToPlayerAsync(player, "You can't go that way.");
            return false;
        }

        // Check for door restrictions
        if (!await CanPassThroughExit(player, exit))
        {
            return false; // Error message already sent in CanPassThroughExit
        }

        // Check if target room exists
        var targetRoom = _worldDatabase.GetRoom(exit.TargetRoomVnum);
        if (targetRoom == null)
        {
            await SendToPlayerAsync(player, "That way leads nowhere.");
            return false;
        }

        // Check room-specific restrictions
        if (!await CanEnterRoom(player, targetRoom))
        {
            return false; // Error message already sent in CanEnterRoom
        }

        // Check movement delay
        if (!await CanMoveNow(player))
        {
            await SendToPlayerAsync(player, "You can't move that fast!");
            return false;
        }

        // Send departure message to other players in current room
        await SendMovementMessageToOthersInRoom(currentRoom, player, $"{player.Name} leaves {FormatDirectionName(direction)}.");
        
        // Remove player from current room's player list
        currentRoom.Players.Remove(player);
        
        // Update player location and movement time
        player.CurrentRoomVnum = targetRoom.VirtualNumber;
        player.LastMovementTime = DateTime.UtcNow;
        
        // Add player to destination room's player list
        targetRoom.Players.Add(player);
        
        // Send arrival message to other players in destination room
        var oppositeDirection = GetOppositeDirection(direction);
        await SendMovementMessageToOthersInRoom(targetRoom, player, $"{player.Name} has arrived from the {FormatDirectionName(oppositeDirection)}.");
        
        // Send movement message to the moving player
        var directionText = direction.ToString().ToLowerInvariant();
        await SendToPlayerAsync(player, $"You head {directionText}.");

        return true;
    }
    
    /// <summary>
    /// Send a movement-related message to all other players in a room (excluding the moving player)
    /// </summary>
    /// <param name="room">Room containing the players</param>
    /// <param name="excludePlayer">Player to exclude from receiving the message</param>
    /// <param name="message">Message to send</param>
    protected async Task SendMovementMessageToOthersInRoom(Room room, IPlayer excludePlayer, string message)
    {
        var otherPlayers = room.Players.Where(p => p != excludePlayer && p.IsConnected);
        
        foreach (var otherPlayer in otherPlayers)
        {
            if (otherPlayer.Connection != null)
            {
                await otherPlayer.Connection.SendDataAsync(message + "\r\n");
            }
        }
    }
    
    /// <summary>
    /// Get the opposite direction for arrival messages
    /// </summary>
    /// <param name="direction">Original direction</param>
    /// <returns>Opposite direction</returns>
    protected static Direction GetOppositeDirection(Direction direction)
    {
        return direction switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East => Direction.West,
            Direction.West => Direction.East,
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            _ => direction // For unknown directions, return the same
        };
    }

    /// <summary>
    /// Show the new room description after movement
    /// This integrates with the Look command to display room details
    /// </summary>
    /// <param name="player">Player who moved</param>
    protected async Task ShowNewRoom(IPlayer player)
    {
        var room = _worldDatabase.GetRoom(player.CurrentRoomVnum);
        if (room == null)
        {
            await SendToPlayerAsync(player, "You are lost in the void!");
            return;
        }

        // Show room name in white/bold
        await SendToPlayerAsync(player, $"&W{room.Name}&N", formatted: true);
        
        // Show room description
        await SendToPlayerAsync(player, room.Description, formatted: true);
        
        // Show exits
        var exits = room.GetAvailableExits();
        if (exits.Any())
        {
            var exitNames = exits.Select(FormatDirectionName);
            var exitText = "&YObvious exits:&N " + string.Join(", ", exitNames);
            await SendToPlayerAsync(player, exitText, formatted: true);
        }
        else
        {
            await SendToPlayerAsync(player, "&YObvious exits:&N None.", formatted: true);
        }
    }

    /// <summary>
    /// Format direction name for display
    /// </summary>
    /// <param name="direction">Direction to format</param>
    /// <returns>Formatted direction name</returns>
    protected static string FormatDirectionName(Direction direction)
    {
        return direction switch
        {
            Direction.North => "north",
            Direction.South => "south", 
            Direction.East => "east",
            Direction.West => "west",
            Direction.Up => "up",
            Direction.Down => "down",
            _ => direction.ToString().ToLowerInvariant()
        };
    }

    /// <summary>
    /// Check if player can pass through an exit (handles doors, locks, keys)
    /// </summary>
    /// <param name="player">Player attempting to move</param>
    /// <param name="exit">Exit to check</param>
    /// <returns>True if player can pass through</returns>
    protected async Task<bool> CanPassThroughExit(IPlayer player, Exit exit)
    {
        // Check for closed door (EX_CLOSED = 1)
        if ((exit.DoorFlags & 1) != 0)
        {
            // Check for locked door (EX_LOCKED = 2, so combined with closed = 3)
            if ((exit.DoorFlags & 2) != 0)
            {
                // Door is locked - check if player has key
                if (exit.KeyVnum > 0 && player.HasItem(exit.KeyVnum))
                {
                    // Player has key - unlock and open door
                    exit.DoorFlags = 0; // Clear all door flags
                    await SendToPlayerAsync(player, "*Click*");
                    await SendToPlayerAsync(player, "You unlock and open the door.");
                    return true;
                }
                else
                {
                    await SendToPlayerAsync(player, "The door is locked.");
                    return false;
                }
            }
            else
            {
                // Door is closed but not locked
                await SendToPlayerAsync(player, "The door is closed.");
                return false;
            }
        }

        return true; // No door or door is open
    }

    /// <summary>
    /// Check if player can enter a specific room
    /// </summary>
    /// <param name="player">Player attempting to enter</param>
    /// <param name="room">Room to enter</param>
    /// <returns>True if player can enter</returns>
    protected async Task<bool> CanEnterRoom(IPlayer player, Room room)
    {
        // Check if room requires flying
        if (room.SectorType == SectorType.Flying && !player.CanFly)
        {
            await SendToPlayerAsync(player, "You need to fly to go there!");
            return false;
        }

        // Check room capacity
        if (room.MaxPlayers > 0 && room.Players.Count >= room.MaxPlayers)
        {
            await SendToPlayerAsync(player, "There's no room for you there.");
            return false;
        }

        // Check minimum level requirement
        if (room.MinimumLevel > 0 && player.Level < room.MinimumLevel)
        {
            await SendToPlayerAsync(player, "You are not experienced enough to enter that area.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Check if player can move now (movement delay check)
    /// </summary>
    /// <param name="player">Player attempting to move</param>
    /// <returns>True if enough time has passed since last movement</returns>
    protected async Task<bool> CanMoveNow(IPlayer player)
    {
        const double MovementDelaySeconds = 1.0; // 1 second delay between moves
        
        var timeSinceLastMove = DateTime.UtcNow - player.LastMovementTime;
        if (timeSinceLastMove.TotalSeconds < MovementDelaySeconds)
        {
            return false;
        }

        return true;
    }
}