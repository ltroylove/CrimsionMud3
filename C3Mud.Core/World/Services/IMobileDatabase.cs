using System.Collections.Generic;
using System.Threading.Tasks;
using C3Mud.Core.World.Models;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Interface for mobile (NPC/Monster) database providing template storage and lookup
/// </summary>
public interface IMobileDatabase
{
    /// <summary>
    /// Loads a single mobile template into the database
    /// Thread-safe operation that overwrites existing mobiles with the same VNum
    /// </summary>
    /// <param name="mobile">The mobile template to load</param>
    void LoadMobile(Mobile mobile);
    
    /// <summary>
    /// Loads multiple mobiles from a .mob file asynchronously
    /// Parses the entire .mob file and loads all mobile templates into memory
    /// </summary>
    /// <param name="filePath">Path to the .mob file</param>
    /// <returns>Task representing the async operation</returns>
    Task LoadMobilesAsync(string filePath);
    
    /// <summary>
    /// Retrieves a mobile template by virtual number with O(1) performance
    /// </summary>
    /// <param name="vnum">Virtual number of the mobile template</param>
    /// <returns>Mobile template if found, null otherwise</returns>
    Mobile? GetMobile(int vnum);
    
    /// <summary>
    /// Creates a new mobile instance from the template for spawning
    /// This is used when creating actual mobile instances in the world
    /// </summary>
    /// <param name="vnum">Virtual number of the mobile template</param>
    /// <returns>New mobile instance based on template, null if template not found</returns>
    Mobile? CreateMobileInstance(int vnum);
    
    /// <summary>
    /// Gets all mobile templates currently loaded in the database
    /// </summary>
    /// <returns>Collection of all mobile templates</returns>
    IEnumerable<Mobile> GetAllMobiles();
    
    /// <summary>
    /// Gets the total count of loaded mobile templates
    /// </summary>
    /// <returns>Number of mobile templates in the database</returns>
    int GetMobileCount();
    
    /// <summary>
    /// Checks if a mobile template is loaded in the database
    /// </summary>
    /// <param name="vnum">Virtual number to check</param>
    /// <returns>True if mobile template exists, false otherwise</returns>
    bool IsMobileLoaded(int vnum);
}