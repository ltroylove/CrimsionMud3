using C3Mud.Core.World.Models;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Service responsible for creating mobile instances from templates and spawning them in the world
/// </summary>
public interface IMobileSpawner
{
    /// <summary>
    /// Creates a new mobile instance from a template
    /// </summary>
    /// <param name="template">The mobile template to create an instance from</param>
    /// <returns>A new mobile instance with unique ID and current stats</returns>
    MobileInstance CreateInstance(Mobile template);
    
    /// <summary>
    /// Spawns a mobile instance in a specific room
    /// </summary>
    /// <param name="template">The mobile template to spawn</param>
    /// <param name="roomVnum">The room virtual number to spawn the mobile in</param>
    /// <returns>A new mobile instance placed in the specified room</returns>
    MobileInstance SpawnInRoom(Mobile template, int roomVnum);
    
    /// <summary>
    /// Equips an object on a mobile instance
    /// </summary>
    /// <param name="mobileInstance">The mobile to equip the object on</param>
    /// <param name="worldObject">The object to equip</param>
    /// <param name="wearPosition">The wear position/slot for the equipment</param>
    void EquipMobile(MobileInstance mobileInstance, WorldObject worldObject, int wearPosition);
    
    /// <summary>
    /// Gives an object to a mobile's inventory
    /// </summary>
    /// <param name="mobileInstance">The mobile to give the object to</param>
    /// <param name="worldObject">The object to add to inventory</param>
    void GiveToMobile(MobileInstance mobileInstance, WorldObject worldObject);
}