using System;
using System.Collections.Generic;

namespace C3Mud.Core.World.Models;

/// <summary>
/// Represents a runtime instance of an object in the game world
/// Created from WorldObject templates and tracks current state and location
/// </summary>
public class ObjectInstance
{
    /// <summary>
    /// Unique identifier for this object instance
    /// </summary>
    public Guid InstanceId { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Reference to the object template this instance was created from
    /// </summary>
    public WorldObject Template { get; set; } = null!;
    
    /// <summary>
    /// Where this object is currently located
    /// </summary>
    public ObjectLocation Location { get; set; }
    
    /// <summary>
    /// ID of the location (room vnum, mobile instance ID, or container instance ID)
    /// </summary>
    public object LocationId { get; set; } = null!;
    
    /// <summary>
    /// Current condition/durability of this object instance (0-100)
    /// </summary>
    public int Condition { get; set; } = 100;
    
    /// <summary>
    /// When this object instance was created
    /// </summary>
    public DateTime SpawnTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether this object is still active in the world
    /// Inactive objects should be cleaned up
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Objects contained within this object (for containers)
    /// </summary>
    public List<ObjectInstance> ContainedObjects { get; set; } = new List<ObjectInstance>();
}

/// <summary>
/// Enum defining where an object instance is located
/// </summary>
public enum ObjectLocation
{
    /// <summary>Object is placed in a room</summary>
    InRoom,
    
    /// <summary>Object is in a mobile's inventory</summary>
    InMobileInventory,
    
    /// <summary>Object is equipped on a mobile</summary>
    EquippedOnMobile,
    
    /// <summary>Object is inside a container object</summary>
    InContainer
}

/// <summary>
/// Enum defining equipment wear positions
/// </summary>
public enum WearPosition
{
    /// <summary>Light source</summary>
    Light = 0,
    
    /// <summary>Right finger ring</summary>
    RightFinger = 1,
    
    /// <summary>Left finger ring</summary>
    LeftFinger = 2,
    
    /// <summary>First neck slot</summary>
    Neck1 = 3,
    
    /// <summary>Second neck slot</summary>
    Neck2 = 4,
    
    /// <summary>Body armor</summary>
    Body = 5,
    
    /// <summary>Head protection</summary>
    Head = 6,
    
    /// <summary>Leg protection</summary>
    Legs = 7,
    
    /// <summary>Foot protection</summary>
    Feet = 8,
    
    /// <summary>Hand protection</summary>
    Hands = 9,
    
    /// <summary>Arm protection</summary>
    Arms = 10,
    
    /// <summary>Shield</summary>
    Shield = 11,
    
    /// <summary>About body (cloak, robe)</summary>
    About = 12,
    
    /// <summary>Around waist</summary>
    Waist = 13,
    
    /// <summary>Right wrist</summary>
    RightWrist = 14,
    
    /// <summary>Left wrist</summary>
    LeftWrist = 15,
    
    /// <summary>Primary weapon</summary>
    Wield = 16,
    
    /// <summary>Held item</summary>
    Hold = 17,
    
    /// <summary>Two-handed weapon</summary>
    TwoHand = 18
}