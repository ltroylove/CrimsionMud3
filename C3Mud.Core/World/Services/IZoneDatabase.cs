using C3Mud.Core.World.Models;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Interface for zone database operations
/// </summary>
public interface IZoneDatabase
{
    /// <summary>
    /// Loads a single zone into the database
    /// </summary>
    /// <param name="zone">Zone to load</param>
    void LoadZone(Zone zone);
    
    /// <summary>
    /// Loads zones from a .zon file
    /// </summary>
    /// <param name="filePath">Path to the .zon file</param>
    /// <returns>Task representing the async loading operation</returns>
    Task LoadZonesAsync(string filePath);
    
    /// <summary>
    /// Gets a zone by its virtual number
    /// </summary>
    /// <param name="vnum">Zone virtual number</param>
    /// <returns>Zone if found, null otherwise</returns>
    Zone? GetZone(int vnum);
    
    /// <summary>
    /// Gets all loaded zones
    /// </summary>
    /// <returns>All zones in the database</returns>
    IEnumerable<Zone> GetAllZones();
    
    /// <summary>
    /// Gets the count of loaded zones
    /// </summary>
    /// <returns>Number of zones loaded</returns>
    int GetZoneCount();
    
    /// <summary>
    /// Gets zones that should be reset based on their age and configuration
    /// </summary>
    /// <returns>Zones that need to be reset</returns>
    IEnumerable<Zone> GetZonesNeedingReset();
    
    /// <summary>
    /// Updates a zone's age and last reset time
    /// </summary>
    /// <param name="zone">Zone to update</param>
    void UpdateZone(Zone zone);
}