using C3Mud.Core.World.Models;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Interface for spawning and managing object instances in the world
/// Handles creation from templates and placement in various locations
/// </summary>
public interface IObjectSpawner
{
    /// <summary>
    /// Creates a new object instance from a template
    /// </summary>
    /// <param name="template">Object template to create instance from</param>
    /// <returns>New object instance</returns>
    ObjectInstance CreateInstance(WorldObject template);
    
    /// <summary>
    /// Spawns an object instance in a room
    /// </summary>
    /// <param name="template">Object template to spawn</param>
    /// <param name="roomVnum">Room virtual number to spawn in</param>
    /// <returns>New object instance placed in room</returns>
    ObjectInstance SpawnInRoom(WorldObject template, int roomVnum);
    
    /// <summary>
    /// Equips an object on a mobile at the specified position
    /// </summary>
    /// <param name="objectInstance">Object to equip</param>
    /// <param name="mobile">Mobile to equip on</param>
    /// <param name="position">Wear position</param>
    void EquipOnMobile(ObjectInstance objectInstance, MobileInstance mobile, WearPosition position);
    
    /// <summary>
    /// Gives an object to a mobile's inventory
    /// </summary>
    /// <param name="objectInstance">Object to give</param>
    /// <param name="mobile">Mobile to give to</param>
    void GiveToMobile(ObjectInstance objectInstance, MobileInstance mobile);
    
    /// <summary>
    /// Puts an object inside a container object
    /// </summary>
    /// <param name="objectInstance">Object to put in container</param>
    /// <param name="container">Container object</param>
    void PutInContainer(ObjectInstance objectInstance, ObjectInstance container);
}