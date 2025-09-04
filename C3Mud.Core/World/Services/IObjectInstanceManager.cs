using C3Mud.Core.World.Models;
using System;
using System.Collections.Generic;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Interface for tracking and managing object instances in the world
/// Provides efficient lookup and management of active objects
/// </summary>
public interface IObjectInstanceManager
{
    /// <summary>
    /// Adds an object instance to the tracking system
    /// </summary>
    /// <param name="objectInstance">Object to track</param>
    void TrackObject(ObjectInstance objectInstance);
    
    /// <summary>
    /// Removes an object instance from tracking
    /// </summary>
    /// <param name="instanceId">Instance ID to remove</param>
    /// <returns>True if object was found and removed</returns>
    bool RemoveObject(Guid instanceId);
    
    /// <summary>
    /// Gets all active objects being tracked
    /// </summary>
    /// <returns>Collection of all active objects</returns>
    IEnumerable<ObjectInstance> GetAllActiveObjects();
    
    /// <summary>
    /// Gets all objects currently in the specified room
    /// </summary>
    /// <param name="roomVnum">Room virtual number</param>
    /// <returns>Collection of objects in the room</returns>
    IEnumerable<ObjectInstance> GetObjectsInRoom(int roomVnum);
    
    /// <summary>
    /// Gets all objects associated with a mobile (equipment and inventory)
    /// </summary>
    /// <param name="mobileId">Mobile instance ID</param>
    /// <returns>Collection of objects on the mobile</returns>
    IEnumerable<ObjectInstance> GetObjectsOnMobile(Guid mobileId);
    
    /// <summary>
    /// Counts how many instances of a specific object template exist
    /// </summary>
    /// <param name="objectVnum">Object virtual number to count</param>
    /// <returns>Number of active instances of the object</returns>
    int CountObjectsOfTemplate(int objectVnum);
    
    /// <summary>
    /// Removes all inactive objects from tracking
    /// </summary>
    /// <returns>Number of objects cleaned up</returns>
    int CleanupInactiveObjects();
}