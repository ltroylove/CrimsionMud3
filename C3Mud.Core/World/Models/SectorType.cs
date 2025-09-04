namespace C3Mud.Core.World.Models;

/// <summary>
/// Sector type enumeration from CircleMUD/DikuMUD format
/// Defines the terrain type of a room, affecting movement costs, weather, etc.
/// </summary>
public enum SectorType
{
    /// <summary>
    /// Inside a building or structure
    /// </summary>
    Inside = 0,
    
    /// <summary>
    /// City or town area
    /// </summary>
    City = 1,
    
    /// <summary>
    /// Open field or grassland
    /// </summary>
    Field = 2,
    
    /// <summary>
    /// Forest or wooded area
    /// </summary>
    Forest = 3,
    
    /// <summary>
    /// Hills or rolling terrain
    /// </summary>
    Hills = 4,
    
    /// <summary>
    /// Mountain terrain
    /// </summary>
    Mountain = 5,
    
    /// <summary>
    /// Water that can be swum in
    /// </summary>
    WaterSwim = 6,
    
    /// <summary>
    /// Water that cannot be entered without boat
    /// </summary>
    WaterNoSwim = 7,
    
    /// <summary>
    /// Underwater location
    /// </summary>
    Underwater = 8,
    
    /// <summary>
    /// Flying/air location
    /// </summary>
    Flying = 9,
    
    /// <summary>
    /// Desert terrain
    /// </summary>
    Desert = 10,
    
    /// <summary>
    /// Swamp or marsh terrain
    /// </summary>
    Swamp = 11,
    
    /// <summary>
    /// Icy or frozen terrain
    /// </summary>
    Ice = 12,
    
    /// <summary>
    /// Road or path
    /// </summary>
    Road = 13,
    
    /// <summary>
    /// Underground cavern or tunnel
    /// </summary>
    Underground = 14
}