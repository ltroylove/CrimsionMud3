using C3Mud.Core.Players;
using C3Mud.Core.World.Services;
using C3Mud.Core.World.Models;

namespace C3Mud.Core.Commands.Basic;

/// <summary>
/// Look command - allows players to examine their surroundings
/// Based on original do_look() function from act.informative.c
/// Now integrated with real world data from WorldDatabase
/// </summary>
public class LookCommand : BaseCommand
{
    private readonly IWorldDatabase? _worldDatabase;
    
    public override string Name => "look";
    public override string[] Aliases => new[] { "l" };
    public override PlayerPosition MinimumPosition => PlayerPosition.Sleeping;
    public override int MinimumLevel => 1;
    
    /// <summary>
    /// Constructor for dependency injection - accepts WorldDatabase for real room data
    /// </summary>
    public LookCommand(IWorldDatabase worldDatabase)
    {
        _worldDatabase = worldDatabase;
    }
    
    /// <summary>
    /// Parameterless constructor for backwards compatibility with existing systems
    /// </summary>
    public LookCommand()
    {
        _worldDatabase = null;
    }

    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            // Look at the room
            await LookAtRoom(player);
        }
        else
        {
            // TODO: PLACEHOLDER - Implement looking at specific objects/players/directions
            // REQUIRED FIXES:
            // 1. Parse target argument (object names, player names, directions)
            // 2. Search current room for matching objects/players
            // 3. Display detailed descriptions for found targets
            // 4. Handle partial name matching like original MUD
            // 5. Show appropriate "You don't see that here" only when truly not found
            await SendToPlayerAsync(player, "You don't see that here.");
        }
    }

    private async Task LookAtRoom(IPlayer player)
    {
        // Use real world data if WorldDatabase is available
        if (_worldDatabase != null)
        {
            await LookAtRoomWithWorldData(player);
        }
        else
        {
            // Fallback to placeholder for backwards compatibility
            await LookAtPlaceholderRoom(player);
        }
    }
    
    private async Task LookAtRoomWithWorldData(IPlayer player)
    {
        var room = _worldDatabase!.GetRoom(player.CurrentRoomVnum);
        if (room == null)
        {
            await SendToPlayerAsync(player, "You are floating in the void!");
            return;
        }
        
        // Show real room name with color formatting
        await SendToPlayerAsync(player, $"&W{room.Name}&N", formatted: true);
        
        // Add blank line after room name
        await SendToPlayerAsync(player, "", formatted: true);
        
        // Show real room description
        await SendToPlayerAsync(player, room.Description, formatted: true);
        
        // Add blank line before exits
        await SendToPlayerAsync(player, "", formatted: true);
        
        // Show real exits
        var availableExits = room.GetAvailableExits();
        if (availableExits.Any())
        {
            var exitNames = availableExits.Select(FormatDirectionName);
            var exitText = $"&YExits:&N [{string.Join("&N] [&Y", exitNames)}&N]";
            await SendToPlayerAsync(player, exitText, formatted: true);
        }
        else
        {
            await SendToPlayerAsync(player, "&YExits:&N None.", formatted: true);
        }
        
        // TODO: Show other players in room (placeholder for now)
        // TODO: Show objects in room (placeholder for now)
        // TODO: Show mobiles in room (placeholder for now)
    }
    
    private async Task LookAtPlaceholderRoom(IPlayer player)
    {
        // Original placeholder code for backwards compatibility
        var roomDescription = @"&WA Simple Room&N

You are standing in a basic testing room. This is a placeholder room description
that will be replaced once the world system is implemented. The walls are bare
and the floor is made of stone.

&YObvious exits:&N
  None (world system not yet implemented)

&GPlayers here:&N";

        await SendToPlayerAsync(player, roomDescription, formatted: true);
        await SendToPlayerAsync(player, $"  {player.Name} is standing here.", formatted: true);
    }
    
    private static string FormatDirectionName(Direction direction)
    {
        return direction switch
        {
            Direction.North => "north",
            Direction.South => "south", 
            Direction.East => "east",
            Direction.West => "west",
            Direction.Up => "up",
            Direction.Down => "down",
            _ => direction.ToString().ToLower()
        };
    }
}