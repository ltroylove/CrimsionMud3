using C3Mud.Core.World.Models;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Interface for zone reset management services
/// </summary>
public interface IZoneResetManager
{
    /// <summary>
    /// Executes a zone reset, spawning mobs, objects, and setting door states
    /// </summary>
    /// <param name="zone">Zone to reset</param>
    /// <returns>Reset result with success status and details</returns>
    ZoneResetResult ExecuteReset(Zone zone);
    
    /// <summary>
    /// Determines if a zone should be reset based on its age, reset mode, and player count
    /// </summary>
    /// <param name="zone">Zone to check</param>
    /// <returns>True if zone should be reset</returns>
    bool ShouldReset(Zone zone);
    
    /// <summary>
    /// Updates the age of a zone (usually called periodically)
    /// </summary>
    /// <param name="zone">Zone to age</param>
    /// <param name="minutesElapsed">Minutes elapsed since last update</param>
    void AgeZone(Zone zone, int minutesElapsed);
}

/// <summary>
/// Result of a zone reset operation
/// </summary>
public class ZoneResetResult
{
    /// <summary>
    /// Whether the reset was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Number of reset commands that were executed
    /// </summary>
    public int CommandsExecuted { get; set; }
    
    /// <summary>
    /// Error message if reset failed
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed log of what was reset
    /// </summary>
    public List<string> ResetLog { get; set; } = new List<string>();
}