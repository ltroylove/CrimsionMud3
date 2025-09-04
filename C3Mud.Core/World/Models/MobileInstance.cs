using C3Mud.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace C3Mud.Core.World.Models;

/// <summary>
/// Represents a runtime instance of a mobile (NPC/Monster) that exists in the world
/// Created from a Mobile template and tracks current state and location
/// </summary>
public class MobileInstance
{
    /// <summary>
    /// Unique identifier for this mobile instance
    /// </summary>
    public Guid InstanceId { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Reference to the mobile template this instance was created from
    /// </summary>
    public Mobile Template { get; set; } = null!;
    
    /// <summary>
    /// Room virtual number where this mobile is currently located
    /// </summary>
    public int CurrentRoomVnum { get; set; }
    
    /// <summary>
    /// Current hit points (health) of this mobile instance
    /// </summary>
    public int CurrentHitPoints { get; set; }
    
    /// <summary>
    /// Current mana points of this mobile instance
    /// </summary>
    public int CurrentMana { get; set; }
    
    /// <summary>
    /// Current position of this mobile (standing, sitting, fighting, etc.)
    /// </summary>
    public PlayerPosition Position { get; set; } = PlayerPosition.Standing;
    
    /// <summary>
    /// When this mobile instance was created
    /// </summary>
    public DateTime SpawnTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether this mobile is still active in the world
    /// Inactive mobiles should be cleaned up during zone resets
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Gets the zone number for the current room location
    /// Calculated by dividing room vnum by 100 (standard MUD convention)
    /// </summary>
    public int CurrentZoneNumber => CurrentRoomVnum / 100;
    
    /// <summary>
    /// Equipment worn by this mobile, keyed by wear position
    /// </summary>
    public Dictionary<WearPosition, ObjectInstance> Equipment { get; set; } = new Dictionary<WearPosition, ObjectInstance>();
    
    /// <summary>
    /// Items carried in this mobile's inventory
    /// </summary>
    public List<ObjectInstance> Inventory { get; set; } = new List<ObjectInstance>();
    
    /// <summary>
    /// Checks if this mobile can wear the specified object
    /// </summary>
    /// <param name="objectInstance">Object to check</param>
    /// <returns>True if the object can be worn</returns>
    public bool CanWear(ObjectInstance objectInstance)
    {
        // Check if object has any wear flags set (excluding TAKE which is for carrying)
        var wearFlags = objectInstance.Template.WearFlags;
        return wearFlags != 0 && (wearFlags & (long)WearFlags.TAKE) != wearFlags;
    }
    
    /// <summary>
    /// Checks if this mobile can carry the specified object
    /// Basic implementation - could be enhanced with weight/capacity checks
    /// </summary>
    /// <param name="objectInstance">Object to check</param>
    /// <returns>True if the object can be carried</returns>
    public bool CanCarry(ObjectInstance objectInstance)
    {
        // Basic check - ensure object can be taken
        return (objectInstance.Template.WearFlags & (long)WearFlags.TAKE) != 0;
    }
}

/// <summary>
/// Alias for PlayerPosition to use for mobile positions
/// Mobiles use the same position system as players
/// </summary>
public enum MobilePosition : byte
{
    /// <summary>
    /// Mobile is dead
    /// </summary>
    Dead = 0,
    
    /// <summary>
    /// Mobile is mortally wounded
    /// </summary>
    MortallyWounded = 1,
    
    /// <summary>
    /// Mobile is incapacitated
    /// </summary>
    Incapacitated = 2,
    
    /// <summary>
    /// Mobile is stunned
    /// </summary>
    Stunned = 3,
    
    /// <summary>
    /// Mobile is sleeping
    /// </summary>
    Sleeping = 4,
    
    /// <summary>
    /// Mobile is resting
    /// </summary>
    Resting = 5,
    
    /// <summary>
    /// Mobile is sitting
    /// </summary>
    Sitting = 6,
    
    /// <summary>
    /// Mobile is fighting
    /// </summary>
    Fighting = 7,
    
    /// <summary>
    /// Mobile is standing (normal position)
    /// </summary>
    Standing = 8
}