using C3Mud.Core.World.Models;
using System;
using System.Collections.Generic;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Service responsible for tracking and managing active mobile instances in the world
/// </summary>
public interface IMobileInstanceManager
{
    /// <summary>
    /// Adds a mobile instance to the active tracking list
    /// </summary>
    /// <param name="mobileInstance">The mobile instance to track</param>
    void TrackMobile(MobileInstance mobileInstance);
    
    /// <summary>
    /// Removes a mobile instance from tracking by its unique ID
    /// </summary>
    /// <param name="instanceId">The unique ID of the mobile instance to remove</param>
    /// <returns>True if the mobile was found and removed, false otherwise</returns>
    bool RemoveMobile(Guid instanceId);
    
    /// <summary>
    /// Gets all mobile instances currently in a specific room
    /// </summary>
    /// <param name="roomVnum">The room virtual number to search</param>
    /// <returns>Collection of mobile instances in the room</returns>
    IEnumerable<MobileInstance> GetMobilesInRoom(int roomVnum);
    
    /// <summary>
    /// Gets all mobile instances currently in a specific zone
    /// </summary>
    /// <param name="zoneNum">The zone number to search</param>
    /// <returns>Collection of mobile instances in the zone</returns>
    IEnumerable<MobileInstance> GetMobilesInZone(int zoneNum);
    
    /// <summary>
    /// Counts the number of active instances of a specific mobile template
    /// </summary>
    /// <param name="mobileVnum">The mobile virtual number (template) to count</param>
    /// <returns>Number of active instances of that mobile template</returns>
    int CountMobilesOfTemplate(int mobileVnum);
    
    /// <summary>
    /// Removes all inactive mobile instances from tracking
    /// </summary>
    /// <returns>Number of inactive mobiles that were cleaned up</returns>
    int CleanupInactiveMobiles();
    
    /// <summary>
    /// Gets all active mobile instances currently being tracked
    /// </summary>
    /// <returns>Collection of all active mobile instances</returns>
    IEnumerable<MobileInstance> GetAllActiveMobiles();
    
    /// <summary>
    /// Gets a mobile instance by its unique ID
    /// </summary>
    /// <param name="instanceId">The unique ID of the mobile instance</param>
    /// <returns>The mobile instance if found, null otherwise</returns>
    MobileInstance? GetMobileById(Guid instanceId);
    
    /// <summary>
    /// Counts all active mobiles in a specific zone (regardless of template)
    /// </summary>
    /// <param name="zoneNum">The zone number to count</param>
    /// <returns>Total number of active mobiles in the zone</returns>
    int CountMobilesInZone(int zoneNum);
}