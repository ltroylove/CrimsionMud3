namespace C3Mud.Core.Characters.Models;

/// <summary>
/// Character alignment based on original CircleMUD alignment system
/// Numeric values match original alignment constants
/// </summary>
public enum Alignment
{
    /// <summary>
    /// Chaotic Evil (-1000 to -700)
    /// </summary>
    ChaoticEvil = -850,
    
    /// <summary>
    /// Neutral Evil (-699 to -300)
    /// </summary>
    NeutralEvil = -500,
    
    /// <summary>
    /// Lawful Evil (-299 to -100)
    /// </summary>
    LawfulEvil = -200,
    
    /// <summary>
    /// True Neutral (-99 to 99)
    /// </summary>
    Neutral = 0,
    
    /// <summary>
    /// Lawful Good (100 to 299)
    /// </summary>
    LawfulGood = 200,
    
    /// <summary>
    /// Neutral Good (300 to 699)
    /// </summary>
    NeutralGood = 500,
    
    /// <summary>
    /// Chaotic Good (700 to 1000)
    /// </summary>
    ChaoticGood = 850,
    
    /// <summary>
    /// Pure Evil (extreme negative alignment)
    /// </summary>
    Evil = -1000,
    
    /// <summary>
    /// Pure Good (extreme positive alignment)
    /// </summary>
    Good = 1000
}