namespace C3Mud.Core.World.Models;

/// <summary>
/// Represents an exit from one room to another in the MUD world
/// Based on the CircleMUD/DikuMUD exit format from .wld files
/// </summary>
public class Exit
{
    /// <summary>
    /// Direction this exit leads (North, South, East, West, Up, Down)
    /// </summary>
    public Direction Direction { get; set; }
    
    /// <summary>
    /// Name/title of the exit (from door name field, can be empty)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the exit (from door description field, can be empty)
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Virtual number of the target room this exit leads to
    /// </summary>
    public int TargetRoomVnum { get; set; }
    
    /// <summary>
    /// Door flags (0 = no door, other values for doors, locks, etc.)
    /// For this iteration, we'll store but not process complex door mechanics
    /// </summary>
    public int DoorFlags { get; set; }
    
    /// <summary>
    /// Virtual number of key needed to unlock this exit (-1 = no key needed)
    /// </summary>
    public int KeyVnum { get; set; } = -1;
}