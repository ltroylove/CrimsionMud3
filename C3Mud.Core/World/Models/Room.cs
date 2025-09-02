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
    /// Mana regeneration rate in this room
    /// </summary>
    public int ManaRegen { get; set; }
    
    /// <summary>
    /// Hit point regeneration rate in this room
    /// </summary>
    public int HpRegen { get; set; }
    
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