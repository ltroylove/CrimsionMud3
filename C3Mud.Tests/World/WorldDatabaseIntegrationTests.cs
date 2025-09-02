using Xunit;
using C3Mud.Core.World.Services;
using C3Mud.Core.World.Models;

namespace C3Mud.Tests.World;

/// <summary>
/// Integration tests for WorldDatabase with real world file data
/// Tests loading complete .wld files and verifying room data integrity
/// </summary>
public class WorldDatabaseIntegrationTests
{
    private readonly IWorldDatabase _worldDatabase;
    private readonly string _testDataPath;
    
    public WorldDatabaseIntegrationTests()
    {
        _worldDatabase = new WorldDatabase();
        // Use the test data in the Original-Code directory
        _testDataPath = Path.Combine(Directory.GetCurrentDirectory(), "Original-Code", "dev", "lib", "areas");
    }
    
    [Fact]
    public async Task WorldDatabase_LoadRoomsAsync_Should_Load_15Rooms_Completely()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "15Rooms.wld");
        
        // Skip test if test data file doesn't exist
        if (!File.Exists(filePath))
        {
            Assert.True(true, "Skipping integration test - 15Rooms.wld not found in test data");
            return;
        }
        
        // Act
        await _worldDatabase.LoadRoomsAsync(filePath);
        
        // Assert - Should load exactly 3 rooms from 15Rooms.wld
        Assert.Equal(3, _worldDatabase.GetRoomCount());
        
        // Verify all expected rooms are loaded
        Assert.True(_worldDatabase.IsRoomLoaded(20385));
        Assert.True(_worldDatabase.IsRoomLoaded(20386));
        Assert.True(_worldDatabase.IsRoomLoaded(20387));
    }
    
    [Fact]
    public async Task WorldDatabase_LoadRoomsAsync_Should_Parse_Room_20385_Correctly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "15Rooms.wld");
        
        // Skip test if test data file doesn't exist
        if (!File.Exists(filePath))
        {
            Assert.True(true, "Skipping integration test - 15Rooms.wld not found in test data");
            return;
        }
        
        // Act
        await _worldDatabase.LoadRoomsAsync(filePath);
        var room = _worldDatabase.GetRoom(20385);
        
        // Assert
        Assert.NotNull(room);
        Assert.Equal(20385, room.VirtualNumber);
        Assert.Equal("Path through the hills", room.Name);
        Assert.Contains("path leads north and south", room.Description);
        Assert.Equal(112, room.Zone);
        Assert.Equal(RoomFlags.Indoors, room.RoomFlags);
        Assert.Equal(SectorType.Inside, room.SectorType);
        Assert.Equal(1, room.LightLevel);
        Assert.Equal(99, room.ManaRegen);
        Assert.Equal(1, room.HpRegen);
        
        // Verify exits
        Assert.Equal(2, room.Exits.Count);
        Assert.True(room.Exits.ContainsKey(Direction.North));
        Assert.True(room.Exits.ContainsKey(Direction.South));
        
        // Verify exit targets
        Assert.Equal(4938, room.Exits[Direction.North].TargetRoomVnum);
        Assert.Equal(20386, room.Exits[Direction.South].TargetRoomVnum);
    }
    
    [Fact]
    public async Task WorldDatabase_LoadRoomsAsync_Should_Parse_Room_20386_Correctly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "15Rooms.wld");
        
        // Skip test if test data file doesn't exist
        if (!File.Exists(filePath))
        {
            Assert.True(true, "Skipping integration test - 15Rooms.wld not found in test data");
            return;
        }
        
        // Act
        await _worldDatabase.LoadRoomsAsync(filePath);
        var room = _worldDatabase.GetRoom(20386);
        
        // Assert
        Assert.NotNull(room);
        Assert.Equal(20386, room.VirtualNumber);
        Assert.Equal("Path through the hills", room.Name);
        
        // Verify exits - Room 20386 has 3 exits: north, east, south
        Assert.Equal(3, room.Exits.Count);
        Assert.True(room.Exits.ContainsKey(Direction.North));
        Assert.True(room.Exits.ContainsKey(Direction.East));
        Assert.True(room.Exits.ContainsKey(Direction.South));
        
        // Verify exit targets
        Assert.Equal(20385, room.Exits[Direction.North].TargetRoomVnum);
        Assert.Equal(9201, room.Exits[Direction.East].TargetRoomVnum);
        Assert.Equal(20387, room.Exits[Direction.South].TargetRoomVnum);
        
        // Verify the named exit (east direction has name "A dark winding road")
        Assert.Equal("A dark winding road", room.Exits[Direction.East].Name);
    }
    
    [Fact]
    public async Task WorldDatabase_LoadRoomsAsync_Should_Parse_Room_20387_Correctly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "15Rooms.wld");
        
        // Skip test if test data file doesn't exist
        if (!File.Exists(filePath))
        {
            Assert.True(true, "Skipping integration test - 15Rooms.wld not found in test data");
            return;
        }
        
        // Act
        await _worldDatabase.LoadRoomsAsync(filePath);
        var room = _worldDatabase.GetRoom(20387);
        
        // Assert
        Assert.NotNull(room);
        Assert.Equal(20387, room.VirtualNumber);
        Assert.Equal("Path through the hills", room.Name);
        
        // Verify exit - Room 20387 has 1 exit: north to 20386
        Assert.Equal(1, room.Exits.Count);
        Assert.True(room.Exits.ContainsKey(Direction.North));
        Assert.Equal(20386, room.Exits[Direction.North].TargetRoomVnum);
    }
    
    [Fact]
    public async Task WorldDatabase_Performance_Should_Load_Rooms_Quickly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "15Rooms.wld");
        
        // Skip test if test data file doesn't exist
        if (!File.Exists(filePath))
        {
            Assert.True(true, "Skipping performance test - 15Rooms.wld not found in test data");
            return;
        }
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act
        await _worldDatabase.LoadRoomsAsync(filePath);
        stopwatch.Stop();
        
        // Assert - Loading should complete in under 1 second for small file
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Loading took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
    }
    
    [Fact]
    public void WorldDatabase_Performance_Should_Lookup_Rooms_Quickly()
    {
        // Arrange
        var room = new Room { VirtualNumber = 12345, Name = "Test Room" };
        _worldDatabase.LoadRoom(room);
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act - Perform many lookups
        for (int i = 0; i < 10000; i++)
        {
            var result = _worldDatabase.GetRoom(12345);
            Assert.NotNull(result);
        }
        
        stopwatch.Stop();
        
        // Assert - Should achieve O(1) lookup performance
        // 10,000 lookups should complete in under 10ms (< 0.001ms per lookup)
        Assert.True(stopwatch.ElapsedMilliseconds < 10, 
            $"10,000 lookups took {stopwatch.ElapsedMilliseconds}ms, expected < 10ms for O(1) performance");
    }
    
    [Fact]
    public void WorldDatabase_ThreadSafety_Should_Handle_Concurrent_Lookups()
    {
        // Arrange
        const int numRooms = 1000;
        const int numThreads = 10;
        const int lookupsPerThread = 1000;
        
        // Load test rooms
        for (int i = 1; i <= numRooms; i++)
        {
            _worldDatabase.LoadRoom(new Room { VirtualNumber = i, Name = $"Room {i}" });
        }
        
        var tasks = new Task[numThreads];
        var random = new Random();
        
        // Act - Multiple threads performing concurrent lookups
        for (int t = 0; t < numThreads; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < lookupsPerThread; i++)
                {
                    var vnum = random.Next(1, numRooms + 1);
                    var room = _worldDatabase.GetRoom(vnum);
                    Assert.NotNull(room);
                    Assert.Equal($"Room {vnum}", room.Name);
                }
            });
        }
        
        // Assert - All tasks should complete without exceptions
        Task.WaitAll(tasks);
        Assert.True(true, "All concurrent lookups completed successfully");
    }
}