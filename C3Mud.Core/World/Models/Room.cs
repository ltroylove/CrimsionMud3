using C3Mud.Core.Players;

namespace C3Mud.Core.World.Models;

/// <summary>
/// Represents a room in the MUD world.
/// Based on the CircleMUD/DikuMUD room format from .wld files.
/// </summary>
public class Room
{
    /// <summary>
    /// Virtual Number - unique identifier for this room across the entire MUD
    /// </summary>
    public int VirtualNumber { get; set; }
    
    /// <summary>
    /// Short description/name of the room (from line after #vnum, ending with ~)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Long description of the room (multi-line text ending with ~)
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Dictionary of exits from this room, keyed by direction
    /// </summary>
    public Dictionary<Direction, Exit> Exits { get; set; } = new Dictionary<Direction, Exit>();
    
    /// <summary>
    /// Zone number this room belongs to
    /// </summary>
    public int Zone { get; set; }
    
    /// <summary>
    /// Room flags (bitfield) defining special properties
    /// </summary>
    public RoomFlags RoomFlags { get; set; }
    
    /// <summary>
    /// Sector type defining terrain characteristics
    /// </summary>
    public SectorType SectorType { get; set; }
    
    /// <summary>
    /// Light level in the room (0 = darkness, higher = brighter)
    /// </summary>
    public int LightLevel { get; set; }
    
    /// <summary>
    /// Number of active light sources in this room (torches, lanterns, etc.)
    /// Based on original CircleMUD world[room].light counter
    /// </summary>
    public int LightSources { get; set; }
    
    /// <summary>
    /// Mana regeneration rate in this room
    /// </summary>
    public int ManaRegen { get; set; }
    
    /// <summary>
    /// Hit point regeneration rate in this room
    /// </summary>
    public int HpRegen { get; set; }
    
    /// <summary>
    /// Maximum number of players allowed in this room (0 = unlimited)
    /// </summary>
    public int MaxPlayers { get; set; }
    
    /// <summary>
    /// Minimum level required to enter this room (0 = no restriction)
    /// </summary>
    public int MinimumLevel { get; set; }
    
    /// <summary>
    /// Players currently in this room
    /// </summary>
    public List<IPlayer> Players { get; set; } = new List<IPlayer>();
    
    /// <summary>
    /// Hidden objects that can be found by searching
    /// </summary>
    public List<WorldObject> HiddenObjects { get; set; } = new List<WorldObject>();
    
    /// <summary>
    /// Objects/items currently in this room
    /// </summary>
    public List<WorldObject> Objects { get; set; } = new List<WorldObject>();
    
    /// <summary>
    /// Alias for Objects - items currently in this room
    /// Provided for equipment system compatibility
    /// </summary>
    public List<WorldObject> Items => Objects;
    
    /// <summary>
    /// Features that can be examined in detail (e.g., "walls", "floor")
    /// </summary>
    public Dictionary<string, string> ExaminableFeatures { get; set; } = new Dictionary<string, string>();
    
    /// <summary>
    /// Gets an exit in the specified direction, or null if no exit exists
    /// </summary>
    /// <param name="direction">The direction to check</param>
    /// <returns>Exit object or null</returns>
    public Exit? GetExit(Direction direction)
    {
        return Exits.TryGetValue(direction, out var exit) ? exit : null;
    }
    
    /// <summary>
    /// Gets a list of all available exit directions from this room
    /// </summary>
    /// <returns>List of available directions</returns>
    public List<Direction> GetAvailableExits()
    {
        return Exits.Keys.ToList();
    }
}