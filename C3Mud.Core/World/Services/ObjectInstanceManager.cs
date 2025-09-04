using C3Mud.Core.World.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Implementation of object instance tracking and management
/// Provides efficient lookup and management of active objects using concurrent collections
/// </summary>
public class ObjectInstanceManager : IObjectInstanceManager
{
    private readonly ConcurrentDictionary<Guid, ObjectInstance> _activeObjects = new();
    
    /// <summary>
    /// Adds an object instance to the tracking system
    /// </summary>
    /// <param name="objectInstance">Object to track</param>
    public void TrackObject(ObjectInstance objectInstance)
    {
        if (objectInstance?.InstanceId != null)
        {
            _activeObjects[objectInstance.InstanceId] = objectInstance;
        }
    }
    
    /// <summary>
    /// Removes an object instance from tracking
    /// </summary>
    /// <param name="instanceId">Instance ID to remove</param>
    /// <returns>True if object was found and removed</returns>
    public bool RemoveObject(Guid instanceId)
    {
        return _activeObjects.TryRemove(instanceId, out _);
    }
    
    /// <summary>
    /// Gets all active objects being tracked
    /// </summary>
    /// <returns>Collection of all active objects</returns>
    public IEnumerable<ObjectInstance> GetAllActiveObjects()
    {
        return _activeObjects.Values.Where(obj => obj.IsActive);
    }
    
    /// <summary>
    /// Gets all objects currently in the specified room
    /// </summary>
    /// <param name="roomVnum">Room virtual number</param>
    /// <returns>Collection of objects in the room</returns>
    public IEnumerable<ObjectInstance> GetObjectsInRoom(int roomVnum)
    {
        return _activeObjects.Values
            .Where(obj => obj.IsActive && 
                         obj.Location == ObjectLocation.InRoom && 
                         obj.LocationId.Equals(roomVnum));
    }
    
    /// <summary>
    /// Gets all objects associated with a mobile (equipment and inventory)
    /// </summary>
    /// <param name="mobileId">Mobile instance ID</param>
    /// <returns>Collection of objects on the mobile</returns>
    public IEnumerable<ObjectInstance> GetObjectsOnMobile(Guid mobileId)
    {
        return _activeObjects.Values
            .Where(obj => obj.IsActive && 
                         (obj.Location == ObjectLocation.EquippedOnMobile || 
                          obj.Location == ObjectLocation.InMobileInventory) &&
                         obj.LocationId.Equals(mobileId));
    }
    
    /// <summary>
    /// Counts how many instances of a specific object template exist
    /// </summary>
    /// <param name="objectVnum">Object virtual number to count</param>
    /// <returns>Number of active instances of the object</returns>
    public int CountObjectsOfTemplate(int objectVnum)
    {
        return _activeObjects.Values
            .Count(obj => obj.IsActive && obj.Template?.VirtualNumber == objectVnum);
    }
    
    /// <summary>
    /// Removes all inactive objects from tracking
    /// </summary>
    /// <returns>Number of objects cleaned up</returns>
    public int CleanupInactiveObjects()
    {
        var inactiveObjects = _activeObjects.Values
            .Where(obj => !obj.IsActive)
            .ToList();
        
        var count = 0;
        foreach (var obj in inactiveObjects)
        {
            if (_activeObjects.TryRemove(obj.InstanceId, out _))
            {
                count++;
            }
        }
        
        return count;
    }
}