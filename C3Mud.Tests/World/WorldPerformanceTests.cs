using Xunit;
using C3Mud.Core.World.Services;
using C3Mud.Core.World.Models;
using System.Diagnostics;

namespace C3Mud.Tests.World;

/// <summary>
/// Comprehensive performance tests for the WorldDatabase
/// Verifies that performance targets are met under various load conditions
/// </summary>
public class WorldPerformanceTests
{
    [Fact]
    public async Task WorldDatabase_LoadLargeFile_ShouldMeetPerformanceTarget()
    {
        // Arrange
        var worldDatabase = new WorldDatabase();
        var testDataPath = Path.Combine(Directory.GetCurrentDirectory(), "Original-Code", "dev", "lib", "areas");
        var midgaardFile = Path.Combine(testDataPath, "Midgaard.wld");
        
        // Skip test if test data file doesn't exist
        if (!File.Exists(midgaardFile))
        {
            Assert.True(true, "Skipping performance test - Midgaard.wld not found");
            return;
        }
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        await worldDatabase.LoadRoomsAsync(midgaardFile);
        stopwatch.Stop();
        
        // Assert - Should load in under 5 seconds
        var elapsedSeconds = stopwatch.ElapsedMilliseconds / 1000.0;
        Assert.True(elapsedSeconds < 5.0, 
            $"Loading Midgaard.wld took {elapsedSeconds:F2} seconds, expected < 5 seconds");
        
        // Verify rooms were loaded
        Assert.True(worldDatabase.GetRoomCount() > 0, "Should have loaded some rooms");
        
        Console.WriteLine($"Loaded {worldDatabase.GetRoomCount()} rooms in {elapsedSeconds:F2} seconds");
        Console.WriteLine($"Loading rate: {worldDatabase.GetRoomCount() / elapsedSeconds:F0} rooms/second");
    }
    
    [Fact]
    public void WorldDatabase_MassiveLookupPerformance_ShouldMeetTarget()
    {
        // Arrange
        var worldDatabase = new WorldDatabase();
        const int numRooms = 10000;
        
        // Load many rooms
        for (int i = 1; i <= numRooms; i++)
        {
            worldDatabase.LoadRoom(new Room 
            { 
                VirtualNumber = i, 
                Name = $"Room {i}",
                Description = $"This is room {i} with a longer description to simulate real world data."
            });
        }
        
        var stopwatch = Stopwatch.StartNew();
        const int numLookups = 100000;
        var random = new Random(42); // Fixed seed for reproducible results
        
        // Act - Perform many random lookups
        for (int i = 0; i < numLookups; i++)
        {
            var vnum = random.Next(1, numRooms + 1);
            var room = worldDatabase.GetRoom(vnum);
            Assert.NotNull(room); // Ensure we found the room
        }
        
        stopwatch.Stop();
        
        // Assert - Should complete in under 100ms for O(1) performance
        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var avgLookupTime = elapsedMs / (double)numLookups;
        
        Assert.True(elapsedMs < 100, 
            $"{numLookups} lookups took {elapsedMs}ms, expected < 100ms");
        
        Console.WriteLine($"{numLookups} lookups took {elapsedMs}ms");
        Console.WriteLine($"Average lookup time: {avgLookupTime:F6}ms");
        Console.WriteLine($"Lookups per second: {numLookups / (elapsedMs / 1000.0):F0}");
    }
    
    [Fact]
    public void WorldDatabase_ConcurrentAccessStressTest_ShouldMaintainPerformance()
    {
        // Arrange
        var worldDatabase = new WorldDatabase();
        const int numRooms = 5000;
        const int numThreads = 20;
        const int operationsPerThread = 1000;
        
        // Load rooms
        for (int i = 1; i <= numRooms; i++)
        {
            worldDatabase.LoadRoom(new Room { VirtualNumber = i, Name = $"Room {i}" });
        }
        
        var stopwatch = Stopwatch.StartNew();
        var tasks = new Task[numThreads];
        var random = new Random();
        
        // Act - Concurrent mixed operations (reads and writes)
        for (int t = 0; t < numThreads; t++)
        {
            int threadId = t;
            tasks[t] = Task.Run(() =>
            {
                var threadRandom = new Random(threadId);
                
                for (int i = 0; i < operationsPerThread; i++)
                {
                    if (i % 10 == 0) // 10% writes, 90% reads
                    {
                        // Write operation
                        var newVnum = numRooms + (threadId * operationsPerThread) + i;
                        worldDatabase.LoadRoom(new Room 
                        { 
                            VirtualNumber = newVnum, 
                            Name = $"New Room {newVnum} Thread {threadId}"
                        });
                    }
                    else
                    {
                        // Read operation
                        var vnum = threadRandom.Next(1, numRooms + 1);
                        var room = worldDatabase.GetRoom(vnum);
                        Assert.NotNull(room);
                    }
                }
            });
        }
        
        Task.WaitAll(tasks);
        stopwatch.Stop();
        
        // Assert - Should complete in reasonable time
        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var totalOperations = numThreads * operationsPerThread;
        
        Assert.True(elapsedMs < 5000, 
            $"{totalOperations} concurrent operations took {elapsedMs}ms, expected < 5000ms");
        
        Console.WriteLine($"{totalOperations} concurrent operations completed in {elapsedMs}ms");
        Console.WriteLine($"Operations per second: {totalOperations / (elapsedMs / 1000.0):F0}");
        Console.WriteLine($"Final room count: {worldDatabase.GetRoomCount()}");
    }
    
    [Fact]
    public async Task WorldDatabase_MultipleFilesLoadingPerformance_ShouldBeEfficient()
    {
        // Arrange
        var worldDatabase = new WorldDatabase();
        var mobileDatabase = new MobileDatabase();
        var objectDatabase = new ObjectDatabase();
        var worldLoader = new WorldLoader(worldDatabase, mobileDatabase, objectDatabase);
        var testDataPath = Path.Combine(Directory.GetCurrentDirectory(), "Original-Code", "dev", "lib", "areas");
        
        // Skip test if test data directory doesn't exist
        if (!Directory.Exists(testDataPath))
        {
            Assert.True(true, "Skipping performance test - test data directory not found");
            return;
        }
        
        var worldFiles = Directory.GetFiles(testDataPath, "*.wld").Take(10).ToList();
        
        if (!worldFiles.Any())
        {
            Assert.True(true, "Skipping performance test - no .wld files found");
            return;
        }
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Load multiple world files
        foreach (var worldFile in worldFiles)
        {
            try
            {
                await worldLoader.LoadWorldFileAsync(worldFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Skipped {Path.GetFileName(worldFile)}: {ex.Message}");
            }
        }
        
        stopwatch.Stop();
        
        // Assert
        var elapsedSeconds = stopwatch.ElapsedMilliseconds / 1000.0;
        var roomCount = worldDatabase.GetRoomCount();
        
        Assert.True(roomCount > 0, "Should have loaded some rooms from the world files");
        
        Console.WriteLine($"Loaded {roomCount} rooms from {worldFiles.Count} files in {elapsedSeconds:F2} seconds");
        Console.WriteLine($"Average loading rate: {roomCount / elapsedSeconds:F0} rooms/second");
        
        // Performance should be reasonable for multiple files
        if (roomCount > 0)
        {
            var roomsPerSecond = roomCount / elapsedSeconds;
            Assert.True(roomsPerSecond > 10, 
                $"Loading rate of {roomsPerSecond:F0} rooms/second is too slow, expected > 10 rooms/second");
        }
    }
    
    [Fact]
    public void WorldDatabase_MemoryUsageTest_ShouldBeReasonable()
    {
        // Arrange
        var worldDatabase = new WorldDatabase();
        const int numRooms = 10000;
        
        // Measure initial memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(false);
        
        // Act - Load many rooms
        for (int i = 1; i <= numRooms; i++)
        {
            worldDatabase.LoadRoom(new Room 
            { 
                VirtualNumber = i, 
                Name = $"Test Room {i}",
                Description = $"This is a test room number {i} with some description text to simulate real data.",
                Zone = i / 100,
                SectorType = (SectorType)(i % 10),
                LightLevel = i % 3,
                ManaRegen = 100,
                HpRegen = 1
            });
        }
        
        // Measure final memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(false);
        
        // Assert
        var memoryUsedMB = (finalMemory - initialMemory) / (1024.0 * 1024.0);
        var bytesPerRoom = (finalMemory - initialMemory) / numRooms;
        
        Console.WriteLine($"Memory used: {memoryUsedMB:F2} MB for {numRooms} rooms");
        Console.WriteLine($"Memory per room: {bytesPerRoom} bytes");
        
        // Should use less than 100MB for 10,000 rooms (reasonable for in-memory storage)
        Assert.True(memoryUsedMB < 100, 
            $"Memory usage of {memoryUsedMB:F2} MB is too high, expected < 100 MB");
        
        // Verify all rooms are still accessible
        Assert.Equal(numRooms, worldDatabase.GetRoomCount());
        
        // Test random access still works
        var testRoom = worldDatabase.GetRoom(5000);
        Assert.NotNull(testRoom);
        Assert.Equal("Test Room 5000", testRoom.Name);
    }
}