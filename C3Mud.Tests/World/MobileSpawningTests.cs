using Xunit;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;
using C3Mud.Core.Players;
using System;
using System.Linq;

namespace C3Mud.Tests.World;

public class MobileSpawningTests
{
    private readonly Mobile _testMobileTemplate;
    private readonly Room _testRoom;
    
    public MobileSpawningTests()
    {
        _testMobileTemplate = new Mobile
        {
            VirtualNumber = 1001,
            Keywords = "test mobile mob",
            ShortDescription = "a test mobile",
            LongDescription = "A test mobile stands here.",
            Level = 10,
            MaxHitPoints = 100,
            MaxMana = 50,
            ArmorClass = 0,
            Position = 8, // Standing
            DefaultPosition = 8
        };
        
        _testRoom = new Room
        {
            VirtualNumber = 3001,
            Name = "Test Room",
            Description = "This is a test room."
        };
    }
    
    [Fact]
    public void MobileSpawner_CreateInstance_ReturnsUniqueMobileInstance()
    {
        // Arrange
        var spawner = new MobileSpawner();
        
        // Act
        var instance1 = spawner.CreateInstance(_testMobileTemplate);
        var instance2 = spawner.CreateInstance(_testMobileTemplate);
        
        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.NotEqual(instance1.InstanceId, instance2.InstanceId);
        Assert.Equal(_testMobileTemplate.VirtualNumber, instance1.Template.VirtualNumber);
        Assert.Equal(_testMobileTemplate.MaxHitPoints, instance1.CurrentHitPoints);
        Assert.Equal(_testMobileTemplate.MaxMana, instance1.CurrentMana);
        Assert.True(instance1.IsActive);
        Assert.Equal(PlayerPosition.Standing, instance1.Position);
    }
    
    [Fact]
    public void MobileSpawner_SpawnInRoom_PlacesMobileInCorrectRoom()
    {
        // Arrange
        var spawner = new MobileSpawner();
        
        // Act
        var instance = spawner.SpawnInRoom(_testMobileTemplate, _testRoom.VirtualNumber);
        
        // Assert
        Assert.NotNull(instance);
        Assert.Equal(_testRoom.VirtualNumber, instance.CurrentRoomVnum);
        Assert.Equal(_testMobileTemplate.VirtualNumber, instance.Template.VirtualNumber);
        Assert.True(instance.IsActive);
    }
    
    [Fact]
    public void MobileInstanceManager_TrackMobile_AddsToActiveList()
    {
        // Arrange
        var manager = new MobileInstanceManager();
        var spawner = new MobileSpawner();
        var instance = spawner.CreateInstance(_testMobileTemplate);
        
        // Act
        manager.TrackMobile(instance);
        
        // Assert
        var trackedMobiles = manager.GetAllActiveMobiles();
        Assert.Equal(1, trackedMobiles.Count());
        Assert.True(trackedMobiles.Contains(instance));
    }
    
    [Fact]
    public void MobileInstanceManager_GetMobilesInRoom_ReturnsCorrectMobiles()
    {
        // Arrange
        var manager = new MobileInstanceManager();
        var spawner = new MobileSpawner();
        var instance1 = spawner.SpawnInRoom(_testMobileTemplate, _testRoom.VirtualNumber);
        var instance2 = spawner.SpawnInRoom(_testMobileTemplate, _testRoom.VirtualNumber);
        var instance3 = spawner.SpawnInRoom(_testMobileTemplate, 9999); // Different room
        
        manager.TrackMobile(instance1);
        manager.TrackMobile(instance2);
        manager.TrackMobile(instance3);
        
        // Act
        var mobilesInRoom = manager.GetMobilesInRoom(_testRoom.VirtualNumber);
        
        // Assert
        Assert.Equal(2, mobilesInRoom.Count());
        Assert.True(mobilesInRoom.Contains(instance1));
        Assert.True(mobilesInRoom.Contains(instance2));
        Assert.False(mobilesInRoom.Contains(instance3));
    }
    
    [Fact]
    public void MobileInstanceManager_CleanupMobiles_RemovesInactiveMobiles()
    {
        // Arrange
        var manager = new MobileInstanceManager();
        var spawner = new MobileSpawner();
        var activeInstance = spawner.CreateInstance(_testMobileTemplate);
        var inactiveInstance = spawner.CreateInstance(_testMobileTemplate);
        inactiveInstance.IsActive = false;
        
        manager.TrackMobile(activeInstance);
        manager.TrackMobile(inactiveInstance);
        
        // Act
        var cleanedCount = manager.CleanupInactiveMobiles();
        
        // Assert
        Assert.Equal(1, cleanedCount);
        var trackedMobiles = manager.GetAllActiveMobiles();
        Assert.Equal(1, trackedMobiles.Count());
        Assert.True(trackedMobiles.Contains(activeInstance));
        Assert.False(trackedMobiles.Contains(inactiveInstance));
    }
    
    [Fact]
    public void MobileInstanceManager_CountMobilesOfTemplate_ReturnsCorrectCount()
    {
        // Arrange
        var manager = new MobileInstanceManager();
        var spawner = new MobileSpawner();
        
        var template1001 = new Mobile { VirtualNumber = 1001 };
        var template1002 = new Mobile { VirtualNumber = 1002 };
        
        var instance1 = spawner.CreateInstance(template1001);
        var instance2 = spawner.CreateInstance(template1001);
        var instance3 = spawner.CreateInstance(template1002);
        
        manager.TrackMobile(instance1);
        manager.TrackMobile(instance2);
        manager.TrackMobile(instance3);
        
        // Act
        var count1001 = manager.CountMobilesOfTemplate(1001);
        var count1002 = manager.CountMobilesOfTemplate(1002);
        var count9999 = manager.CountMobilesOfTemplate(9999);
        
        // Assert
        Assert.Equal(2, count1001);
        Assert.Equal(1, count1002);
        Assert.Equal(0, count9999);
    }
    
    [Fact]
    public void MobileInstanceManager_GetMobilesInZone_ReturnsCorrectMobiles()
    {
        // Arrange
        var manager = new MobileInstanceManager();
        var spawner = new MobileSpawner();
        
        // Zone 30 has rooms 3000-3099
        var instance1 = spawner.SpawnInRoom(_testMobileTemplate, 3001); // Zone 30
        var instance2 = spawner.SpawnInRoom(_testMobileTemplate, 3050); // Zone 30
        var instance3 = spawner.SpawnInRoom(_testMobileTemplate, 4001); // Zone 40
        
        manager.TrackMobile(instance1);
        manager.TrackMobile(instance2);
        manager.TrackMobile(instance3);
        
        // Act
        var mobilesInZone30 = manager.GetMobilesInZone(30);
        var mobilesInZone40 = manager.GetMobilesInZone(40);
        
        // Assert
        Assert.Equal(2, mobilesInZone30.Count());
        Assert.Equal(1, mobilesInZone40.Count());
        Assert.True(mobilesInZone30.Contains(instance1));
        Assert.True(mobilesInZone30.Contains(instance2));
        Assert.True(mobilesInZone40.Contains(instance3));
    }
    
    [Fact]
    public void MobileInstanceManager_RemoveMobile_RemovesFromTracking()
    {
        // Arrange
        var manager = new MobileInstanceManager();
        var spawner = new MobileSpawner();
        var instance = spawner.CreateInstance(_testMobileTemplate);
        
        manager.TrackMobile(instance);
        Assert.Equal(1, manager.GetAllActiveMobiles().Count());
        
        // Act
        var removed = manager.RemoveMobile(instance.InstanceId);
        
        // Assert
        Assert.True(removed);
        Assert.Equal(0, manager.GetAllActiveMobiles().Count());
        
        // Try to remove again
        var removedAgain = manager.RemoveMobile(instance.InstanceId);
        Assert.False(removedAgain);
    }
    
    [Fact]
    public void MobileInstance_SpawnTime_IsSetOnCreation()
    {
        // Arrange
        var spawner = new MobileSpawner();
        var beforeSpawn = DateTime.UtcNow;
        
        // Act
        var instance = spawner.CreateInstance(_testMobileTemplate);
        
        // Assert
        var afterSpawn = DateTime.UtcNow;
        Assert.True(instance.SpawnTime >= beforeSpawn);
        Assert.True(instance.SpawnTime <= afterSpawn);
    }
}