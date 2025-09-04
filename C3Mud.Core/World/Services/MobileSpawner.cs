using C3Mud.Core.Players;
using C3Mud.Core.World.Models;
using System;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Service responsible for creating mobile instances from templates and spawning them in the world
/// </summary>
public class MobileSpawner : IMobileSpawner
{
    /// <summary>
    /// Creates a new mobile instance from a template
    /// </summary>
    /// <param name="template">The mobile template to create an instance from</param>
    /// <returns>A new mobile instance with unique ID and current stats</returns>
    public MobileInstance CreateInstance(Mobile template)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));
            
        return new MobileInstance
        {
            InstanceId = Guid.NewGuid(),
            Template = template,
            CurrentHitPoints = template.MaxHitPoints,
            CurrentMana = template.MaxMana,
            Position = (PlayerPosition)template.DefaultPosition,
            SpawnTime = DateTime.UtcNow,
            IsActive = true,
            CurrentRoomVnum = 0 // Will be set when placed in room
        };
    }
    
    /// <summary>
    /// Spawns a mobile instance in a specific room
    /// </summary>
    /// <param name="template">The mobile template to spawn</param>
    /// <param name="roomVnum">The room virtual number to spawn the mobile in</param>
    /// <returns>A new mobile instance placed in the specified room</returns>
    public MobileInstance SpawnInRoom(Mobile template, int roomVnum)
    {
        var instance = CreateInstance(template);
        instance.CurrentRoomVnum = roomVnum;
        return instance;
    }
    
    /// <summary>
    /// Equips an object on a mobile instance
    /// </summary>
    /// <param name="mobileInstance">The mobile to equip the object on</param>
    /// <param name="worldObject">The object to equip</param>
    /// <param name="wearPosition">The wear position/slot for the equipment</param>
    public void EquipMobile(MobileInstance mobileInstance, WorldObject worldObject, int wearPosition)
    {
        if (mobileInstance == null)
            throw new ArgumentNullException(nameof(mobileInstance));
        if (worldObject == null)
            throw new ArgumentNullException(nameof(worldObject));
            
        // TODO: Implement equipment system
        // For now, just log the action (placeholder)
        // In a full implementation, this would:
        // - Check if wear position is valid
        // - Check if mobile can wear the object
        // - Remove existing equipment from that slot
        // - Add object to mobile's equipment slots
        Console.WriteLine($"Equipped {worldObject.ShortDescription} on mobile {mobileInstance.Template.ShortDescription} at position {wearPosition}");
    }
    
    /// <summary>
    /// Gives an object to a mobile's inventory
    /// </summary>
    /// <param name="mobileInstance">The mobile to give the object to</param>
    /// <param name="worldObject">The object to add to inventory</param>
    public void GiveToMobile(MobileInstance mobileInstance, WorldObject worldObject)
    {
        if (mobileInstance == null)
            throw new ArgumentNullException(nameof(mobileInstance));
        if (worldObject == null)
            throw new ArgumentNullException(nameof(worldObject));
            
        // TODO: Implement inventory system
        // For now, just log the action (placeholder)
        // In a full implementation, this would:
        // - Check carrying capacity
        // - Add object to mobile's inventory list
        // - Handle weight/size restrictions
        Console.WriteLine($"Gave {worldObject.ShortDescription} to mobile {mobileInstance.Template.ShortDescription}");
    }
}