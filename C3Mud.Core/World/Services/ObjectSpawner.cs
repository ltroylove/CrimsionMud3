using C3Mud.Core.World.Models;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Implementation of object spawning and placement services
/// Handles creation from templates and placement in various locations
/// </summary>
public class ObjectSpawner : IObjectSpawner
{
    /// <summary>
    /// Creates a new object instance from a template
    /// </summary>
    /// <param name="template">Object template to create instance from</param>
    /// <returns>New object instance</returns>
    public ObjectInstance CreateInstance(WorldObject template)
    {
        return new ObjectInstance
        {
            Template = template,
            Location = ObjectLocation.InRoom, // Default location
            LocationId = 0, // Will be set when placed
            Condition = 100, // New objects start in perfect condition
            SpawnTime = System.DateTime.UtcNow,
            IsActive = true
        };
    }
    
    /// <summary>
    /// Spawns an object instance in a room
    /// </summary>
    /// <param name="template">Object template to spawn</param>
    /// <param name="roomVnum">Room virtual number to spawn in</param>
    /// <returns>New object instance placed in room</returns>
    public ObjectInstance SpawnInRoom(WorldObject template, int roomVnum)
    {
        var instance = CreateInstance(template);
        instance.Location = ObjectLocation.InRoom;
        instance.LocationId = roomVnum;
        return instance;
    }
    
    /// <summary>
    /// Equips an object on a mobile at the specified position
    /// </summary>
    /// <param name="objectInstance">Object to equip</param>
    /// <param name="mobile">Mobile to equip on</param>
    /// <param name="position">Wear position</param>
    public void EquipOnMobile(ObjectInstance objectInstance, MobileInstance mobile, WearPosition position)
    {
        // Update object location
        objectInstance.Location = ObjectLocation.EquippedOnMobile;
        objectInstance.LocationId = mobile.InstanceId;
        
        // Add to mobile's equipment
        mobile.Equipment[position] = objectInstance;
    }
    
    /// <summary>
    /// Gives an object to a mobile's inventory
    /// </summary>
    /// <param name="objectInstance">Object to give</param>
    /// <param name="mobile">Mobile to give to</param>
    public void GiveToMobile(ObjectInstance objectInstance, MobileInstance mobile)
    {
        // Update object location
        objectInstance.Location = ObjectLocation.InMobileInventory;
        objectInstance.LocationId = mobile.InstanceId;
        
        // Add to mobile's inventory
        mobile.Inventory.Add(objectInstance);
    }
    
    /// <summary>
    /// Puts an object inside a container object
    /// </summary>
    /// <param name="objectInstance">Object to put in container</param>
    /// <param name="container">Container object</param>
    public void PutInContainer(ObjectInstance objectInstance, ObjectInstance container)
    {
        // Update object location
        objectInstance.Location = ObjectLocation.InContainer;
        objectInstance.LocationId = container.InstanceId;
        
        // Add to container's contents
        container.ContainedObjects.Add(objectInstance);
    }
}