namespace C3Mud.Core.World.Models;

/// <summary>
/// Exit flags for special exit properties
/// Based on CircleMUD exit flags
/// </summary>
[Flags]
public enum ExitFlags
{
    /// <summary>
    /// No special properties
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Exit is hidden and not shown in obvious exits
    /// </summary>
    Hidden = 1 << 0,
    
    /// <summary>
    /// Exit is secret and requires searching to find
    /// </summary>
    Secret = 1 << 1,
    
    /// <summary>
    /// Exit is blocked or impassable
    /// </summary>
    Blocked = 1 << 2,
    
    /// <summary>
    /// Exit is one-way only (cannot return)
    /// </summary>
    OneWay = 1 << 3,
    
    /// <summary>
    /// Exit is protected and requires special conditions
    /// </summary>
    Protected = 1 << 4
}