using C3Mud.Core.World.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Service responsible for tracking and managing active mobile instances in the world
/// Uses thread-safe collections for optimal performance under concurrent access
/// </summary>
public class MobileInstanceManager : IMobileInstanceManager
{
    private readonly ConcurrentDictionary<Guid, MobileInstance> _activeMobiles;
    
    /// <summary>
    /// Initializes a new MobileInstanceManager with empty mobile tracking
    /// </summary>
    public MobileInstanceManager()
    {
        _activeMobiles = new ConcurrentDictionary<Guid, MobileInstance>();
    }
    
    /// <summary>
    /// Adds a mobile instance to the active tracking list
    /// </summary>
    /// <param name="mobileInstance">The mobile instance to track</param>
    public void TrackMobile(MobileInstance mobileInstance)
    {
        if (mobileInstance == null)
            throw new ArgumentNullException(nameof(mobileInstance));
            
        _activeMobiles.AddOrUpdate(mobileInstance.InstanceId, mobileInstance, (key, oldValue) => mobileInstance);
    }
    
    /// <summary>
    /// Removes a mobile instance from tracking by its unique ID
    /// </summary>
    /// <param name="instanceId">The unique ID of the mobile instance to remove</param>
    /// <returns>True if the mobile was found and removed, false otherwise</returns>
    public bool RemoveMobile(Guid instanceId)
    {
        return _activeMobiles.TryRemove(instanceId, out _);
    }
    
    /// <summary>
    /// Gets all mobile instances currently in a specific room
    /// </summary>
    /// <param name="roomVnum">The room virtual number to search</param>
    /// <returns>Collection of mobile instances in the room</returns>
    public IEnumerable<MobileInstance> GetMobilesInRoom(int roomVnum)
    {
        return _activeMobiles.Values
            .Where(mobile => mobile.IsActive && mobile.CurrentRoomVnum == roomVnum);
    }
    
    /// <summary>
    /// Gets all mobile instances currently in a specific zone
    /// </summary>
    /// <param name="zoneNum">The zone number to search</param>
    /// <returns>Collection of mobile instances in the zone</returns>
    public IEnumerable<MobileInstance> GetMobilesInZone(int zoneNum)
    {
        return _activeMobiles.Values
            .Where(mobile => mobile.IsActive && mobile.CurrentZoneNumber == zoneNum);
    }
    
    /// <summary>
    /// Counts the number of active instances of a specific mobile template
    /// </summary>
    /// <param name="mobileVnum">The mobile virtual number (template) to count</param>
    /// <returns>Number of active instances of that mobile template</returns>
    public int CountMobilesOfTemplate(int mobileVnum)
    {
        return _activeMobiles.Values
            .Count(mobile => mobile.IsActive && mobile.Template.VirtualNumber == mobileVnum);
    }
    
    /// <summary>
    /// Removes all inactive mobile instances from tracking
    /// </summary>
    /// <returns>Number of inactive mobiles that were cleaned up</returns>
    public int CleanupInactiveMobiles()
    {
        var inactiveMobiles = _activeMobiles.Values
            .Where(mobile => !mobile.IsActive)
            .ToList();
            
        var cleanedCount = 0;
        foreach (var mobile in inactiveMobiles)
        {
            if (_activeMobiles.TryRemove(mobile.InstanceId, out _))
            {
                cleanedCount++;
            }
        }
        
        return cleanedCount;
    }
    
    /// <summary>
    /// Gets all active mobile instances currently being tracked
    /// </summary>
    /// <returns>Collection of all active mobile instances</returns>
    public IEnumerable<MobileInstance> GetAllActiveMobiles()
    {
        return _activeMobiles.Values.Where(mobile => mobile.IsActive);
    }
    
    /// <summary>
    /// Gets a mobile instance by its unique ID
    /// </summary>
    /// <param name="instanceId">The unique ID of the mobile instance</param>
    /// <returns>The mobile instance if found, null otherwise</returns>
    public MobileInstance? GetMobileById(Guid instanceId)
    {
        return _activeMobiles.TryGetValue(instanceId, out var mobile) ? mobile : null;
    }
    
    /// <summary>
    /// Counts all active mobiles in a specific zone (regardless of template)
    /// </summary>
    /// <param name="zoneNum">The zone number to count</param>
    /// <returns>Total number of active mobiles in the zone</returns>
    public int CountMobilesInZone(int zoneNum)
    {
        return _activeMobiles.Values
            .Count(mobile => mobile.IsActive && mobile.CurrentZoneNumber == zoneNum);
    }
}