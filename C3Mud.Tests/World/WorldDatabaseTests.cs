using Xunit;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;
using System.Collections.Concurrent;

namespace C3Mud.Tests.World;

/// <summary>
/// TDD tests for the WorldDatabase service - Red Phase (failing tests first)
/// These tests define the expected behavior for in-memory world database operations
/// </summary>
public class WorldDatabaseTests
{
    private readonly IWorldDatabase _worldDatabase;
    
    public WorldDatabaseTests()
    {
        // This will fail initially since IWorldDatabase doesn't exist yet
        _worldDatabase = new WorldDatabase();
    }
    
    [Fact]
    public void WorldDatabase_LoadRoom_ShouldStoreCorrectly()
    {
        // Arrange
        var room = new Room
        {
            VirtualNumber = 12345,
            Name = "Test Room",
            Description = "A test room for unit testing",
            Zone = 123,
            SectorType = SectorType.Inside,
            LightLevel = 1
        };
        
        // Act
        _worldDatabase.LoadRoom(room);
        
        // Assert
        var retrievedRoom = _worldDatabase.GetRoom(12345);
        Assert.NotNull(retrievedRoom);
        Assert.Equal(12345, retrievedRoom.VirtualNumber);
        Assert.Equal("Test Room", retrievedRoom.Name);
        Assert.Equal("A test room for unit testing", retrievedRoom.Description);
        Assert.Equal(123, retrievedRoom.Zone);
        Assert.Equal(SectorType.Inside, retrievedRoom.SectorType);
        Assert.Equal(1, retrievedRoom.LightLevel);
    }
    
    [Fact]
    public void WorldDatabase_GetRoom_ShouldReturnCorrectRoom()
    {
        // Arrange
        var room1 = new Room { VirtualNumber = 100, Name = "Room 100" };
        var room2 = new Room { VirtualNumber = 200, Name = "Room 200" };
        _worldDatabase.LoadRoom(room1);
        _worldDatabase.LoadRoom(room2);
        
        // Act
        var retrievedRoom100 = _worldDatabase.GetRoom(100);
        var retrievedRoom200 = _worldDatabase.GetRoom(200);
        
        // Assert
        Assert.NotNull(retrievedRoom100);
        Assert.NotNull(retrievedRoom200);
        Assert.Equal("Room 100", retrievedRoom100.Name);
        Assert.Equal("Room 200", retrievedRoom200.Name);
        Assert.Equal(100, retrievedRoom100.VirtualNumber);
        Assert.Equal(200, retrievedRoom200.VirtualNumber);
    }
    
    [Fact]
    public void WorldDatabase_GetRoom_NonExistent_ShouldReturnNull()
    {
        // Arrange - empty database
        
        // Act
        var nonExistentRoom = _worldDatabase.GetRoom(99999);
        
        // Assert
        Assert.Null(nonExistentRoom);
    }
    
    [Fact]
    public void WorldDatabase_LoadMultipleRooms_ShouldHandleCorrectly()
    {
        // Arrange
        var rooms = new List<Room>();
        for (int i = 1; i <= 100; i++)
        {
            rooms.Add(new Room 
            { 
                VirtualNumber = i, 
                Name = $"Room {i}",
                Description = $"Description for room {i}"
            });
        }
        
        // Act
        foreach (var room in rooms)
        {
            _worldDatabase.LoadRoom(room);
        }
        
        // Assert
        Assert.Equal(100, _worldDatabase.GetRoomCount());
        
        // Test random access
        var room50 = _worldDatabase.GetRoom(50);
        Assert.NotNull(room50);
        Assert.Equal("Room 50", room50.Name);
        
        var room1 = _worldDatabase.GetRoom(1);
        Assert.NotNull(room1);
        Assert.Equal("Room 1", room1.Name);
        
        var room100 = _worldDatabase.GetRoom(100);
        Assert.NotNull(room100);
        Assert.Equal("Room 100", room100.Name);
    }
    
    [Fact]
    public void WorldDatabase_ThreadSafety_ShouldHandleConcurrentAccess()
    {
        // Arrange
        const int numThreads = 10;
        const int roomsPerThread = 100;
        var tasks = new Task[numThreads];
        var errors = new ConcurrentBag<Exception>();
        
        // Act - Multiple threads loading rooms concurrently
        for (int t = 0; t < numThreads; t++)
        {
            int threadId = t;
            tasks[t] = Task.Run(() =>
            {
                try
                {
                    for (int r = 0; r < roomsPerThread; r++)
                    {
                        var vnum = (threadId * roomsPerThread) + r;
                        var room = new Room
                        {
                            VirtualNumber = vnum,
                            Name = $"Room {vnum} Thread {threadId}",
                            Description = $"Room {vnum} created by thread {threadId}"
                        };
                        _worldDatabase.LoadRoom(room);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            });
        }
        
        Task.WaitAll(tasks);
        
        // Assert - No exceptions and correct room count
        Assert.Equal(0, errors.Count);
        Assert.Equal(numThreads * roomsPerThread, _worldDatabase.GetRoomCount());
        
        // Test concurrent reads
        var readTasks = new Task[numThreads];
        for (int t = 0; t < numThreads; t++)
        {
            int threadId = t;
            readTasks[t] = Task.Run(() =>
            {
                try
                {
                    for (int r = 0; r < roomsPerThread; r++)
                    {
                        var vnum = (threadId * roomsPerThread) + r;
                        var room = _worldDatabase.GetRoom(vnum);
                        Assert.NotNull(room);
                        Assert.Equal(vnum, room.VirtualNumber);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            });
        }
        
        Task.WaitAll(readTasks);
        Assert.Equal(0, errors.Count);
    }
    
    [Fact]
    public void WorldDatabase_GetAllRooms_ShouldReturnAllLoadedRooms()
    {
        // Arrange
        var room1 = new Room { VirtualNumber = 1, Name = "Room 1" };
        var room2 = new Room { VirtualNumber = 2, Name = "Room 2" };
        var room3 = new Room { VirtualNumber = 3, Name = "Room 3" };
        
        _worldDatabase.LoadRoom(room1);
        _worldDatabase.LoadRoom(room2);
        _worldDatabase.LoadRoom(room3);
        
        // Act
        var allRooms = _worldDatabase.GetAllRooms().ToList();
        
        // Assert
        Assert.Equal(3, allRooms.Count);
        Assert.True(allRooms.Any(r => r.VirtualNumber == 1));
        Assert.True(allRooms.Any(r => r.VirtualNumber == 2));
        Assert.True(allRooms.Any(r => r.VirtualNumber == 3));
    }
    
    [Fact]
    public void WorldDatabase_IsRoomLoaded_ShouldReturnCorrectStatus()
    {
        // Arrange
        var room = new Room { VirtualNumber = 12345 };
        
        // Act & Assert - Before loading
        Assert.False(_worldDatabase.IsRoomLoaded(12345));
        
        // Load room
        _worldDatabase.LoadRoom(room);
        
        // Act & Assert - After loading
        Assert.True(_worldDatabase.IsRoomLoaded(12345));
        Assert.False(_worldDatabase.IsRoomLoaded(99999)); // Non-existent room
    }
    
    [Fact]
    public void WorldDatabase_GetRoomCount_EmptyDatabase_ShouldReturnZero()
    {
        // Act
        var count = _worldDatabase.GetRoomCount();
        
        // Assert
        Assert.Equal(0, count);
    }
    
    [Fact]
    public void WorldDatabase_LoadRoom_DuplicateVNum_ShouldOverwrite()
    {
        // Arrange
        var room1 = new Room { VirtualNumber = 100, Name = "Original Room" };
        var room2 = new Room { VirtualNumber = 100, Name = "Updated Room" };
        
        // Act
        _worldDatabase.LoadRoom(room1);
        _worldDatabase.LoadRoom(room2); // Should overwrite
        
        // Assert
        Assert.Equal(1, _worldDatabase.GetRoomCount());
        var retrievedRoom = _worldDatabase.GetRoom(100);
        Assert.NotNull(retrievedRoom);
        Assert.Equal("Updated Room", retrievedRoom.Name);
    }
}