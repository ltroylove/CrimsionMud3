using Xunit;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace C3Mud.Tests.World;

/// <summary>
/// Tests for object instance system: creation, spawning, tracking, and inventory/equipment management
/// These tests follow TDD methodology - written first to fail, then implemented to pass
/// </summary>
public class ObjectInstanceTests
{
    private readonly WorldObject _testObject;
    private readonly Mobile _testMobile;
    private readonly Room _testRoom;
    
    public ObjectInstanceTests()
    {
        // Test object template - a basic sword
        _testObject = new WorldObject
        {
            VirtualNumber = 1001,
            Name = "sword test blade",
            ShortDescription = "a test sword",
            LongDescription = "A test sword lies here.",
            ObjectType = ObjectType.WEAPON,
            WearFlags = (long)WearFlags.TAKE | (long)WearFlags.WIELD,
            Weight = 5,
            Cost = 100,
            Values = new int[] { 6, 2, 1, 0 } // 2d6+1 damage
        };
        
        // Test mobile template
        _testMobile = new Mobile
        {
            VirtualNumber = 2001,
            Keywords = "test warrior",
            ShortDescription = "a test warrior",
            LongDescription = "A test warrior stands here.",
            Level = 10,
            MaxHitPoints = 100,
            MaxMana = 50
        };
        
        // Test room
        _testRoom = new Room
        {
            VirtualNumber = 3001,
            Name = "Test Room",
            Description = "This is a test room."
        };
    }
    
    #region ObjectSpawner Tests
    
    /// <summary>
    /// Test that ObjectSpawner can create unique instances from templates
    /// </summary>
    [Fact]
    public void ObjectSpawner_CreateInstance_ReturnsUniqueObjectInstance()
    {
        // Arrange
        var spawner = new ObjectSpawner();
        
        // Act
        var instance1 = spawner.CreateInstance(_testObject);
        var instance2 = spawner.CreateInstance(_testObject);
        
        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.NotEqual(instance1.InstanceId, instance2.InstanceId);
        Assert.Equal(_testObject, instance1.Template);
        Assert.Equal(_testObject, instance2.Template);
        Assert.True(instance1.IsActive);
        Assert.True(instance2.IsActive);
    }
    
    /// <summary>
    /// Test that ObjectSpawner can spawn objects in rooms
    /// </summary>
    [Fact]
    public void ObjectSpawner_SpawnInRoom_PlacesObjectInCorrectRoom()
    {
        // Arrange
        var spawner = new ObjectSpawner();
        
        // Act
        var instance = spawner.SpawnInRoom(_testObject, _testRoom.VirtualNumber);
        
        // Assert
        Assert.NotNull(instance);
        Assert.Equal(ObjectLocation.InRoom, instance.Location);
        Assert.Equal(_testRoom.VirtualNumber, instance.LocationId);
        Assert.Equal(_testObject, instance.Template);
    }
    
    /// <summary>
    /// Test that ObjectSpawner can equip objects on mobiles
    /// </summary>
    [Fact]
    public void ObjectSpawner_EquipOnMobile_MovesToEquipment()
    {
        // Arrange
        var spawner = new ObjectSpawner();
        var mobileInstance = new MobileInstance { Template = _testMobile };
        var objectInstance = spawner.CreateInstance(_testObject);
        
        // Act
        spawner.EquipOnMobile(objectInstance, mobileInstance, WearPosition.Wield);
        
        // Assert
        Assert.Equal(ObjectLocation.EquippedOnMobile, objectInstance.Location);
        Assert.Equal(mobileInstance.InstanceId, objectInstance.LocationId);
        Assert.True(mobileInstance.Equipment.ContainsKey(WearPosition.Wield));
        Assert.Equal(objectInstance, mobileInstance.Equipment[WearPosition.Wield]);
    }
    
    /// <summary>
    /// Test that ObjectSpawner can give objects to mobile inventories
    /// </summary>
    [Fact]
    public void ObjectSpawner_GiveToMobile_MovesToInventory()
    {
        // Arrange
        var spawner = new ObjectSpawner();
        var mobileInstance = new MobileInstance { Template = _testMobile };
        var objectInstance = spawner.CreateInstance(_testObject);
        
        // Act
        spawner.GiveToMobile(objectInstance, mobileInstance);
        
        // Assert
        Assert.Equal(ObjectLocation.InMobileInventory, objectInstance.Location);
        Assert.Equal(mobileInstance.InstanceId, objectInstance.LocationId);
        Assert.Contains(objectInstance, mobileInstance.Inventory);
    }
    
    #endregion
    
    #region ObjectInstanceManager Tests
    
    /// <summary>
    /// Test that ObjectInstanceManager can track active objects
    /// </summary>
    [Fact]
    public void ObjectInstanceManager_TrackObject_AddsToActiveList()
    {
        // Arrange
        var manager = new ObjectInstanceManager();
        var spawner = new ObjectSpawner();
        var instance = spawner.CreateInstance(_testObject);
        
        // Act
        manager.TrackObject(instance);
        
        // Assert
        var activeObjects = manager.GetAllActiveObjects();
        Assert.Contains(instance, activeObjects);
        Assert.Single(activeObjects);
    }
    
    /// <summary>
    /// Test that ObjectInstanceManager can get objects in specific rooms
    /// </summary>
    [Fact]
    public void ObjectInstanceManager_GetObjectsInRoom_ReturnsCorrectObjects()
    {
        // Arrange
        var manager = new ObjectInstanceManager();
        var spawner = new ObjectSpawner();
        var instance1 = spawner.SpawnInRoom(_testObject, _testRoom.VirtualNumber);
        var instance2 = spawner.SpawnInRoom(_testObject, 9999); // Different room
        
        manager.TrackObject(instance1);
        manager.TrackObject(instance2);
        
        // Act
        var roomObjects = manager.GetObjectsInRoom(_testRoom.VirtualNumber);
        
        // Assert
        Assert.Single(roomObjects);
        Assert.Equal(instance1, roomObjects.First());
        Assert.DoesNotContain(instance2, roomObjects);
    }
    
    /// <summary>
    /// Test that ObjectInstanceManager can get objects on mobiles
    /// </summary>
    [Fact]
    public void ObjectInstanceManager_GetObjectsOnMobile_ReturnsEquipmentAndInventory()
    {
        // Arrange
        var manager = new ObjectInstanceManager();
        var spawner = new ObjectSpawner();
        var mobileInstance = new MobileInstance { Template = _testMobile };
        
        var weapon = spawner.CreateInstance(_testObject);
        var item = spawner.CreateInstance(_testObject);
        
        spawner.EquipOnMobile(weapon, mobileInstance, WearPosition.Wield);
        spawner.GiveToMobile(item, mobileInstance);
        
        manager.TrackObject(weapon);
        manager.TrackObject(item);
        
        // Act
        var mobileObjects = manager.GetObjectsOnMobile(mobileInstance.InstanceId);
        
        // Assert
        Assert.Equal(2, mobileObjects.Count());
        Assert.Contains(weapon, mobileObjects);
        Assert.Contains(item, mobileObjects);
    }
    
    /// <summary>
    /// Test that ObjectInstanceManager can count objects of specific templates
    /// </summary>
    [Fact]
    public void ObjectInstanceManager_CountObjectsOfTemplate_ReturnsCorrectCount()
    {
        // Arrange
        var manager = new ObjectInstanceManager();
        var spawner = new ObjectSpawner();
        
        var instance1 = spawner.CreateInstance(_testObject);
        var instance2 = spawner.CreateInstance(_testObject);
        var differentObject = new WorldObject { VirtualNumber = 9999, Name = "different" };
        var instance3 = spawner.CreateInstance(differentObject);
        
        manager.TrackObject(instance1);
        manager.TrackObject(instance2);
        manager.TrackObject(instance3);
        
        // Act
        var count = manager.CountObjectsOfTemplate(_testObject.VirtualNumber);
        var differentCount = manager.CountObjectsOfTemplate(9999);
        
        // Assert
        Assert.Equal(2, count);
        Assert.Equal(1, differentCount);
    }
    
    /// <summary>
    /// Test that ObjectInstanceManager can remove objects from tracking
    /// </summary>
    [Fact]
    public void ObjectInstanceManager_RemoveObject_RemovesFromTracking()
    {
        // Arrange
        var manager = new ObjectInstanceManager();
        var spawner = new ObjectSpawner();
        var instance = spawner.CreateInstance(_testObject);
        
        manager.TrackObject(instance);
        Assert.Single(manager.GetAllActiveObjects());
        
        // Act
        manager.RemoveObject(instance.InstanceId);
        
        // Assert
        Assert.Empty(manager.GetAllActiveObjects());
    }
    
    #endregion
    
    #region ObjectInstance Model Tests
    
    /// <summary>
    /// Test that ObjectInstance properties are set correctly
    /// </summary>
    [Fact]
    public void ObjectInstance_Properties_SetCorrectly()
    {
        // Arrange & Act
        var instance = new ObjectInstance
        {
            Template = _testObject,
            Location = ObjectLocation.InRoom,
            LocationId = _testRoom.VirtualNumber,
            Condition = 100
        };
        
        // Assert
        Assert.Equal(_testObject, instance.Template);
        Assert.Equal(ObjectLocation.InRoom, instance.Location);
        Assert.Equal(_testRoom.VirtualNumber, instance.LocationId);
        Assert.Equal(100, instance.Condition);
        Assert.NotEqual(Guid.Empty, instance.InstanceId);
        Assert.True(instance.IsActive);
        Assert.NotEqual(default(DateTime), instance.SpawnTime);
        Assert.NotNull(instance.ContainedObjects);
    }
    
    /// <summary>
    /// Test that container objects can contain other objects
    /// </summary>
    [Fact]
    public void ObjectInstance_Container_CanContainObjects()
    {
        // Arrange
        var container = new ObjectInstance { Template = _testObject };
        var item = new ObjectInstance { Template = _testObject };
        
        // Act
        container.ContainedObjects.Add(item);
        item.Location = ObjectLocation.InContainer;
        item.LocationId = container.InstanceId;
        
        // Assert
        Assert.Single(container.ContainedObjects);
        Assert.Equal(item, container.ContainedObjects.First());
        Assert.Equal(ObjectLocation.InContainer, item.Location);
        Assert.Equal(container.InstanceId, item.LocationId);
    }
    
    #endregion
    
    #region MobileInstance Equipment Tests
    
    /// <summary>
    /// Test that MobileInstance has equipment and inventory properties
    /// </summary>
    [Fact]
    public void MobileInstance_HasEquipmentAndInventory()
    {
        // Arrange & Act
        var mobileInstance = new MobileInstance { Template = _testMobile };
        
        // Assert
        Assert.NotNull(mobileInstance.Equipment);
        Assert.NotNull(mobileInstance.Inventory);
        Assert.Empty(mobileInstance.Equipment);
        Assert.Empty(mobileInstance.Inventory);
    }
    
    /// <summary>
    /// Test that MobileInstance can check if it can wear objects
    /// </summary>
    [Fact]
    public void MobileInstance_CanWear_ChecksWearFlags()
    {
        // Arrange
        var mobileInstance = new MobileInstance { Template = _testMobile };
        var objectInstance = new ObjectInstance { Template = _testObject };
        
        // Act & Assert
        Assert.True(mobileInstance.CanWear(objectInstance));
        
        // Test with non-wearable object
        var nonWearable = new WorldObject { WearFlags = 0 };
        var nonWearableInstance = new ObjectInstance { Template = nonWearable };
        Assert.False(mobileInstance.CanWear(nonWearableInstance));
    }
    
    /// <summary>
    /// Test that MobileInstance can check carrying capacity
    /// </summary>
    [Fact]
    public void MobileInstance_CanCarry_ChecksCapacity()
    {
        // Arrange
        var mobileInstance = new MobileInstance { Template = _testMobile };
        var objectInstance = new ObjectInstance { Template = _testObject };
        
        // Act & Assert
        // Basic test - should be able to carry at least one item
        Assert.True(mobileInstance.CanCarry(objectInstance));
    }
    
    #endregion
}