using System.Collections.Generic;

namespace C3Mud.Core.World.Models;

/// <summary>
/// Represents a zone in the MUD world containing reset information
/// </summary>
public class Zone
{
    /// <summary>
    /// Virtual number of the zone
    /// </summary>
    public int VirtualNumber { get; set; }
    
    /// <summary>
    /// Name of the zone
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Top room vnum for this zone
    /// </summary>
    public int TopRoom { get; set; }
    
    /// <summary>
    /// Reset time in minutes
    /// </summary>
    public int ResetTime { get; set; }
    
    /// <summary>
    /// Reset mode (0=never, 1=empty, 2=always)
    /// </summary>
    public ResetMode ResetMode { get; set; }
    
    /// <summary>
    /// Percentage chance of reset occurring
    /// </summary>
    public int ResetChance { get; set; }
    
    /// <summary>
    /// Maximum number of players who can be in zone
    /// </summary>
    public int MaxPlayers { get; set; }
    
    /// <summary>
    /// Minimum level to enter zone
    /// </summary>
    public int MinLevel { get; set; }
    
    /// <summary>
    /// List of reset commands for this zone
    /// </summary>
    public List<ResetCommand> ResetCommands { get; set; } = new List<ResetCommand>();
    
    /// <summary>
    /// Last time this zone was reset
    /// </summary>
    public DateTime LastReset { get; set; }
    
    /// <summary>
    /// Age of the zone in minutes since last reset
    /// </summary>
    public int Age { get; set; }
}

/// <summary>
/// Zone reset mode enumeration
/// </summary>
public enum ResetMode
{
    /// <summary>
    /// Never reset automatically
    /// </summary>
    Never = 0,
    
    /// <summary>
    /// Reset when zone is empty of players
    /// </summary>
    WhenEmpty = 1,
    
    /// <summary>
    /// Always reset regardless of players
    /// </summary>
    Always = 2
}