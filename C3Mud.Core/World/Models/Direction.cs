namespace C3Mud.Core.World.Models;

/// <summary>
/// Represents the six possible movement directions in the MUD world
/// Based on CircleMUD/DikuMUD direction mapping
/// </summary>
public enum Direction
{
    /// <summary>
    /// North - Direction 0 in CircleMUD format (D0)
    /// </summary>
    North = 0,
    
    /// <summary>
    /// East - Direction 1 in CircleMUD format (D1)
    /// </summary>
    East = 1,
    
    /// <summary>
    /// South - Direction 2 in CircleMUD format (D2)
    /// </summary>
    South = 2,
    
    /// <summary>
    /// West - Direction 3 in CircleMUD format (D3)
    /// </summary>
    West = 3,
    
    /// <summary>
    /// Up - Direction 4 in CircleMUD format (D4)
    /// </summary>
    Up = 4,
    
    /// <summary>
    /// Down - Direction 5 in CircleMUD format (D5)
    /// </summary>
    Down = 5
}