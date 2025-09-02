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

        // Check if target room exists
        var targetRoom = _worldDatabase.GetRoom(exit.TargetRoomVnum);
        if (targetRoom == null)
        {
            await SendToPlayerAsync(player, "That way leads nowhere.");
            return false;
        }

        // Update player location
        player.CurrentRoomVnum = targetRoom.VirtualNumber;
        
        // Send movement message
        var directionText = direction.ToString().ToLowerInvariant();
        await SendToPlayerAsync(player, $"You head {directionText}.");

        return true;
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
}