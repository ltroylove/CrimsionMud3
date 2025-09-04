using Xunit;
using Moq;
using C3Mud.Core.World.Services;
using C3Mud.Core.World.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace C3Mud.Tests.World;

public class ZoneResetManagerTests
{
    private readonly Mock<IWorldDatabase> _mockWorldDatabase;
    private readonly Mock<IMobileDatabase> _mockMobileDatabase;
    private readonly Mock<IObjectDatabase> _mockObjectDatabase;
    private readonly ZoneResetManager _resetManager;
    
    public ZoneResetManagerTests()
    {
        _mockWorldDatabase = new Mock<IWorldDatabase>();
        _mockMobileDatabase = new Mock<IMobileDatabase>();
        _mockObjectDatabase = new Mock<IObjectDatabase>();
        _resetManager = new ZoneResetManager(_mockWorldDatabase.Object, _mockMobileDatabase.Object, _mockObjectDatabase.Object);
    }
    
    [Fact]
    public void ZoneResetManager_ExecuteReset_SpawnsMobilesAndObjects()
    {
        // Test that zone reset executes mobile and object spawning commands
        var zone = new Zone
        {
            VirtualNumber = 100,
            Name = "Test Zone",
            ResetCommands = new List<ResetCommand>
            {
                new ResetCommand
                {
                    CommandType = ResetCommandType.Mobile,
                    Arg1 = 0,
                    Arg2 = 1001, // mobile vnum
                    Arg3 = 2,    // max existing
                    Arg4 = 1000  // room vnum
                },
                new ResetCommand
                {
                    CommandType = ResetCommandType.Object,
                    Arg1 = 0,
                    Arg2 = 2001, // object vnum
                    Arg3 = 1,    // max existing
                    Arg4 = 1000  // room vnum
                }
            }
        };
        
        var room = new Room { VirtualNumber = 1000, Name = "Test Room" };
        var mobile = new Mobile { VirtualNumber = 1001, ShortDescription = "test mobile" };
        var worldObject = new WorldObject { VirtualNumber = 2001, ShortDescription = "test object" };
        
        _mockWorldDatabase.Setup(x => x.GetRoom(1000)).Returns(room);
        _mockMobileDatabase.Setup(x => x.GetMobile(1001)).Returns(mobile);
        _mockObjectDatabase.Setup(x => x.GetObject(2001)).Returns(worldObject);
        _mockWorldDatabase.Setup(x => x.CountMobilesInZone(zone.VirtualNumber, 1001)).Returns(1);
        _mockWorldDatabase.Setup(x => x.CountObjectsInZone(zone.VirtualNumber, 2001)).Returns(0);
        
        var result = _resetManager.ExecuteReset(zone);
        
        Assert.True(result.Success);
        Assert.Equal(2, result.CommandsExecuted);
        _mockWorldDatabase.Verify(x => x.SpawnMobile(mobile, room), Times.Once);
        _mockWorldDatabase.Verify(x => x.SpawnObject(worldObject, room), Times.Once);
    }
    
    [Fact]
    public void ZoneResetManager_ExecuteReset_RespectsMaxExistingLimits()
    {
        // Test that reset respects maximum existing limits for spawns
        var zone = new Zone
        {
            VirtualNumber = 100,
            ResetCommands = new List<ResetCommand>
            {
                new ResetCommand
                {
                    CommandType = ResetCommandType.Mobile,
                    Arg1 = 0,
                    Arg2 = 1001, // mobile vnum
                    Arg3 = 2,    // max existing
                    Arg4 = 1000  // room vnum
                }
            }
        };
        
        var room = new Room { VirtualNumber = 1000, Name = "Test Room" };
        var mobile = new Mobile { VirtualNumber = 1001, ShortDescription = "test mobile" };
        
        _mockWorldDatabase.Setup(x => x.GetRoom(1000)).Returns(room);
        _mockMobileDatabase.Setup(x => x.GetMobile(1001)).Returns(mobile);
        _mockWorldDatabase.Setup(x => x.CountMobilesInZone(zone.VirtualNumber, 1001)).Returns(2); // Already at limit
        
        var result = _resetManager.ExecuteReset(zone);
        
        Assert.True(result.Success);
        Assert.Equal(0, result.CommandsExecuted); // No commands should execute due to limit
        _mockWorldDatabase.Verify(x => x.SpawnMobile(It.IsAny<Mobile>(), It.IsAny<Room>()), Times.Never);
    }
    
    [Fact]
    public void ZoneResetManager_ExecuteReset_HandlesEquipmentCommands()
    {
        // Test that equipment commands equip items on the last spawned mobile
        var zone = new Zone
        {
            VirtualNumber = 100,
            ResetCommands = new List<ResetCommand>
            {
                new ResetCommand
                {
                    CommandType = ResetCommandType.Mobile,
                    Arg1 = 0,
                    Arg2 = 1001, // mobile vnum
                    Arg3 = 1,    // max existing
                    Arg4 = 1000  // room vnum
                },
                new ResetCommand
                {
                    CommandType = ResetCommandType.Equip,
                    Arg1 = 1,
                    Arg2 = 2001, // object vnum
                    Arg3 = 0,    // max existing
                    Arg4 = 16    // wear position (held)
                }
            }
        };
        
        var room = new Room { VirtualNumber = 1000, Name = "Test Room" };
        var mobile = new Mobile { VirtualNumber = 1001, ShortDescription = "test mobile" };
        var equipment = new WorldObject { VirtualNumber = 2001, ShortDescription = "test weapon" };
        var spawnedMobile = new Mobile { VirtualNumber = 1001, ShortDescription = "test mobile" };
        
        _mockWorldDatabase.Setup(x => x.GetRoom(1000)).Returns(room);
        _mockMobileDatabase.Setup(x => x.GetMobile(1001)).Returns(mobile);
        _mockObjectDatabase.Setup(x => x.GetObject(2001)).Returns(equipment);
        _mockWorldDatabase.Setup(x => x.CountMobilesInZone(zone.VirtualNumber, 1001)).Returns(0);
        _mockWorldDatabase.Setup(x => x.SpawnMobile(mobile, room)).Returns(spawnedMobile);
        
        var result = _resetManager.ExecuteReset(zone);
        
        Assert.True(result.Success);
        Assert.Equal(2, result.CommandsExecuted);
        _mockWorldDatabase.Verify(x => x.EquipObjectOnMobile(equipment, spawnedMobile, 16), Times.Once);
    }
    
    [Fact]
    public void ZoneResetManager_ExecuteReset_HandlesGiveCommands()
    {
        // Test that give commands add items to mobile inventory
        var zone = new Zone
        {
            VirtualNumber = 100,
            ResetCommands = new List<ResetCommand>
            {
                new ResetCommand
                {
                    CommandType = ResetCommandType.Mobile,
                    Arg1 = 0,
                    Arg2 = 1001, // mobile vnum
                    Arg3 = 1,    // max existing
                    Arg4 = 1000  // room vnum
                },
                new ResetCommand
                {
                    CommandType = ResetCommandType.Give,
                    Arg1 = 1,
                    Arg2 = 2001, // object vnum
                    Arg3 = 1     // max existing
                }
            }
        };
        
        var room = new Room { VirtualNumber = 1000, Name = "Test Room" };
        var mobile = new Mobile { VirtualNumber = 1001, ShortDescription = "test mobile" };
        var item = new WorldObject { VirtualNumber = 2001, ShortDescription = "test item" };
        var spawnedMobile = new Mobile { VirtualNumber = 1001, ShortDescription = "test mobile" };
        
        _mockWorldDatabase.Setup(x => x.GetRoom(1000)).Returns(room);
        _mockMobileDatabase.Setup(x => x.GetMobile(1001)).Returns(mobile);
        _mockObjectDatabase.Setup(x => x.GetObject(2001)).Returns(item);
        _mockWorldDatabase.Setup(x => x.CountMobilesInZone(zone.VirtualNumber, 1001)).Returns(0);
        _mockWorldDatabase.Setup(x => x.SpawnMobile(mobile, room)).Returns(spawnedMobile);
        
        var result = _resetManager.ExecuteReset(zone);
        
        Assert.True(result.Success);
        Assert.Equal(2, result.CommandsExecuted);
        _mockWorldDatabase.Verify(x => x.GiveObjectToMobile(item, spawnedMobile), Times.Once);
    }
    
    [Fact]
    public void ZoneResetManager_ExecuteReset_HandlesDoorCommands()
    {
        // Test that door commands set door states correctly
        var zone = new Zone
        {
            VirtualNumber = 100,
            ResetCommands = new List<ResetCommand>
            {
                new ResetCommand
                {
                    CommandType = ResetCommandType.Door,
                    Arg1 = 0,
                    Arg2 = 1000, // room vnum
                    Arg3 = 1,    // direction (east)
                    Arg4 = 2     // state (locked)
                }
            }
        };
        
        var room = new Room { VirtualNumber = 1000, Name = "Test Room" };
        
        _mockWorldDatabase.Setup(x => x.GetRoom(1000)).Returns(room);
        
        var result = _resetManager.ExecuteReset(zone);
        
        Assert.True(result.Success);
        Assert.Equal(1, result.CommandsExecuted);
        _mockWorldDatabase.Verify(x => x.SetDoorState(room, Direction.East, DoorState.Locked), Times.Once);
    }
    
    [Fact]
    public void ZoneResetManager_ExecuteReset_HandlesPutCommands()
    {
        // Test that put commands place objects in containers
        var zone = new Zone
        {
            VirtualNumber = 100,
            ResetCommands = new List<ResetCommand>
            {
                new ResetCommand
                {
                    CommandType = ResetCommandType.Object,
                    Arg1 = 0,
                    Arg2 = 3001, // container vnum
                    Arg3 = 1,    // max existing
                    Arg4 = 1000  // room vnum
                },
                new ResetCommand
                {
                    CommandType = ResetCommandType.Put,
                    Arg1 = 1,
                    Arg2 = 3002, // object vnum
                    Arg3 = 2,    // max existing
                    Arg4 = 3001  // container vnum
                }
            }
        };
        
        var room = new Room { VirtualNumber = 1000, Name = "Test Room" };
        var container = new WorldObject { VirtualNumber = 3001, ShortDescription = "test container" };
        var item = new WorldObject { VirtualNumber = 3002, ShortDescription = "test item" };
        var spawnedContainer = new WorldObject { VirtualNumber = 3001, ShortDescription = "test container" };
        
        _mockWorldDatabase.Setup(x => x.GetRoom(1000)).Returns(room);
        _mockObjectDatabase.Setup(x => x.GetObject(3001)).Returns(container);
        _mockObjectDatabase.Setup(x => x.GetObject(3002)).Returns(item);
        _mockWorldDatabase.Setup(x => x.CountObjectsInZone(zone.VirtualNumber, 3001)).Returns(0);
        _mockWorldDatabase.Setup(x => x.SpawnObject(container, room)).Returns(spawnedContainer);
        
        var result = _resetManager.ExecuteReset(zone);
        
        Assert.True(result.Success);
        Assert.Equal(2, result.CommandsExecuted);
        _mockWorldDatabase.Verify(x => x.PutObjectInContainer(item, spawnedContainer), Times.Once);
    }
    
    [Fact]
    public void ZoneResetManager_ShouldReset_RespectsResetMode()
    {
        // Test that reset mode is properly respected
        var neverResetZone = new Zone
        {
            VirtualNumber = 100,
            ResetMode = ResetMode.Never,
            ResetTime = 30,
            Age = 60 // Older than reset time
        };
        
        var emptyResetZone = new Zone
        {
            VirtualNumber = 101,
            ResetMode = ResetMode.WhenEmpty,
            ResetTime = 30,
            Age = 60
        };
        
        var alwaysResetZone = new Zone
        {
            VirtualNumber = 102,
            ResetMode = ResetMode.Always,
            ResetTime = 30,
            Age = 60
        };
        
        _mockWorldDatabase.Setup(x => x.CountPlayersInZone(101)).Returns(0); // Empty
        _mockWorldDatabase.Setup(x => x.CountPlayersInZone(102)).Returns(5); // Has players
        
        Assert.False(_resetManager.ShouldReset(neverResetZone));
        Assert.True(_resetManager.ShouldReset(emptyResetZone));
        Assert.True(_resetManager.ShouldReset(alwaysResetZone));
    }
    
    [Fact]
    public void ZoneResetManager_ShouldReset_RespectsResetTime()
    {
        // Test that reset time is properly respected
        var youngZone = new Zone
        {
            VirtualNumber = 100,
            ResetMode = ResetMode.Always,
            ResetTime = 30,
            Age = 15 // Younger than reset time
        };
        
        var oldZone = new Zone
        {
            VirtualNumber = 101,
            ResetMode = ResetMode.Always,
            ResetTime = 30,
            Age = 45 // Older than reset time
        };
        
        Assert.False(_resetManager.ShouldReset(youngZone));
        Assert.True(_resetManager.ShouldReset(oldZone));
    }
    
    [Fact]
    public void ZoneResetManager_ExecuteReset_HandlesErrorsGracefully()
    {
        // Test that errors during reset are handled gracefully
        var zone = new Zone
        {
            VirtualNumber = 100,
            ResetCommands = new List<ResetCommand>
            {
                new ResetCommand
                {
                    CommandType = ResetCommandType.Mobile,
                    Arg1 = 1, // Don't check limits
                    Arg2 = 9999, // Non-existent mobile
                    Arg3 = 1, // Max existing (irrelevant since Arg1 != 0)
                    Arg4 = 1000 // Non-existent room
                }
            }
        };
        
        _mockWorldDatabase.Setup(x => x.GetRoom(1000)).Returns((Room)null); // Room doesn't exist
        _mockMobileDatabase.Setup(x => x.GetMobile(9999)).Returns((Mobile)null); // Mobile doesn't exist
        _mockWorldDatabase.Setup(x => x.CountMobilesInZone(100, 9999)).Returns(0); // No existing mobiles
        
        var result = _resetManager.ExecuteReset(zone);
        
        // Debug output
        Console.WriteLine($"Result Success: {result.Success}");
        Console.WriteLine($"Error Message: '{result.ErrorMessage}'");
        Console.WriteLine($"Commands Executed: {result.CommandsExecuted}");
        
        Assert.False(result.Success);
        Assert.Contains("Room 1000 not found", result.ErrorMessage);
    }
}