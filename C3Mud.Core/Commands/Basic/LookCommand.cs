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
            // Look at specific target (direction, object, player, etc.)
            await LookAtTarget(player, arguments.Trim().ToLowerInvariant());
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
        
        // Check for darkness
        if (IsRoomDark(room, player))
        {
            await SendToPlayerAsync(player, "It is pitch black, and you can't see a thing!");
            return;
        }
        
        // Show real room name with color formatting
        await SendToPlayerAsync(player, $"&W{room.Name}&N", formatted: true);
        
        // Add blank line after room name
        await SendToPlayerAsync(player, "", formatted: true);
        
        // Show real room description
        await SendToPlayerAsync(player, room.Description, formatted: true);
        
        // Show weather effects for outdoor rooms
        if (IsOutdoorRoom(room))
        {
            var weather = _worldDatabase.GetCurrentWeather();
            if (!string.IsNullOrEmpty(weather))
            {
                await SendToPlayerAsync(player, weather, formatted: true);
            }
        }
        
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
    
    private async Task LookAtTarget(IPlayer player, string target)
    {
        var room = _worldDatabase?.GetRoom(player.CurrentRoomVnum);
        if (room == null)
        {
            await SendToPlayerAsync(player, "You are floating in the void!");
            return;
        }
        
        // Check for darkness - can't look at specific things in the dark
        if (IsRoomDark(room, player))
        {
            await SendToPlayerAsync(player, "It is pitch black, and you can't see a thing!");
            return;
        }
        
        // Try to parse as a direction first
        if (TryParseDirection(target, out var direction))
        {
            await LookAtDirection(player, room, direction);
            return;
        }
        
        // Try looking at door names
        if (await TryLookAtDoor(player, room, target))
        {
            return;
        }
        
        // Try examining room features
        if (await TryExamineFeature(player, room, target))
        {
            return;
        }
        
        // Default - not found
        await SendToPlayerAsync(player, "You don't see anything special in that direction.");
    }
    
    private async Task LookAtDirection(IPlayer player, Room room, Direction direction)
    {
        var exit = room.GetExit(direction);
        if (exit == null)
        {
            await SendToPlayerAsync(player, "You don't see anything special in that direction.");
            return;
        }
        
        // Show exit description if available
        if (!string.IsNullOrEmpty(exit.Description))
        {
            await SendToPlayerAsync(player, exit.Description);
        }
        
        // Check door state using modern C# flag patterns
        var doorFlags = (DoorFlags)exit.DoorFlags;
        if (doorFlags.HasFlag(DoorFlags.ISDOOR) && doorFlags.HasFlag(DoorFlags.CLOSED))
        {
            await SendToPlayerAsync(player, "The door is closed.");
        }
        else if (!string.IsNullOrEmpty(exit.Name) && exit.Name.Contains("door"))
        {
            // Open door - look through it
            var targetRoom = _worldDatabase?.GetRoom(exit.TargetRoomVnum);
            if (targetRoom != null)
            {
                await SendToPlayerAsync(player, $"Through the door you can see:");
                await SendToPlayerAsync(player, targetRoom.Name);
            }
        }
    }
    
    private async Task<bool> TryLookAtDoor(IPlayer player, Room room, string target)
    {
        // Look for exits with matching door names
        foreach (var exit in room.Exits.Values)
        {
            if (!string.IsNullOrEmpty(exit.Name) && exit.Name.ToLowerInvariant().Contains(target))
            {
                if (!string.IsNullOrEmpty(exit.Description))
                {
                    await SendToPlayerAsync(player, exit.Description);
                }
                
                var exitDoorFlags = (DoorFlags)exit.DoorFlags;
                if (exitDoorFlags.HasFlag(DoorFlags.ISDOOR) && exitDoorFlags.HasFlag(DoorFlags.CLOSED))
                {
                    await SendToPlayerAsync(player, "The door is closed.");
                }
                else
                {
                    var targetRoom = _worldDatabase?.GetRoom(exit.TargetRoomVnum);
                    if (targetRoom != null)
                    {
                        await SendToPlayerAsync(player, $"Through the door you can see:");
                        await SendToPlayerAsync(player, targetRoom.Name);
                    }
                }
                
                return true;
            }
        }
        
        return false;
    }
    
    private async Task<bool> TryExamineFeature(IPlayer player, Room room, string target)
    {
        if (room.ExaminableFeatures.TryGetValue(target, out var description))
        {
            await SendToPlayerAsync(player, description);
            return true;
        }
        
        return false;
    }
    
    private static bool TryParseDirection(string input, out Direction direction)
    {
        direction = Direction.North; // default
        
        return input switch
        {
            "north" or "n" => SetDirection(Direction.North, out direction),
            "south" or "s" => SetDirection(Direction.South, out direction), 
            "east" or "e" => SetDirection(Direction.East, out direction),
            "west" or "w" => SetDirection(Direction.West, out direction),
            "up" or "u" => SetDirection(Direction.Up, out direction),
            "down" or "d" => SetDirection(Direction.Down, out direction),
            _ => false
        };
        
        static bool SetDirection(Direction dir, out Direction result)
        {
            result = dir;
            return true;
        }
    }
    
    private static bool IsRoomDark(Room room, IPlayer player)
    {
        // Room is dark if it has the Dark flag and player has no light
        return room.RoomFlags.HasFlag(RoomFlags.Dark) && !player.HasLight;
    }
    
    private static bool IsOutdoorRoom(Room room)
    {
        // Check if room is outdoor sector type
        return room.SectorType == SectorType.Field || 
               room.SectorType == SectorType.Forest || 
               room.SectorType == SectorType.Hills ||
               room.SectorType == SectorType.Mountain ||
               room.SectorType == SectorType.Desert ||
               room.SectorType == SectorType.Flying;
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