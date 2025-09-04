using Xunit;
using C3Mud.Core.World.Services;
using C3Mud.Core.World.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;

namespace C3Mud.Tests.World;

public class ObjectDatabaseTests
{
    private WorldObject CreateTestObject(int vnum, string name)
    {
        return new WorldObject
        {
            VirtualNumber = vnum,
            Name = $"test object {name}",
            ShortDescription = $"A test object named {name}",
            LongDescription = $"A test object named {name} lies here.",
            ActionDescription = $"You examine {name} closely.",
            ObjectType = ObjectType.LIGHT,
            ExtraFlags = 0,
            WearFlags = 1, // TAKE
            Weight = 5,
            Cost = 100,
            RentPerDay = 10,
            Values = new int[] { 0, 0, 0, 0 }
        };
    }
    
    [Fact]
    public void ObjectDatabase_LoadObject_ShouldStoreCorrectly()
    {
        var objectDb = new ObjectDatabase();
        var obj = CreateTestObject(12345, "TestObject");
        
        objectDb.LoadObject(obj);
        
        var retrieved = objectDb.GetObject(12345);
        Assert.NotNull(retrieved);
        Assert.Equal(12345, retrieved.VirtualNumber);
        Assert.Equal("A test object named TestObject", retrieved.ShortDescription);
        Assert.Equal("test object TestObject", retrieved.Name);
    }
    
    [Fact]
    public void ObjectDatabase_GetObject_ShouldReturnCorrectObject()
    {
        var objectDb = new ObjectDatabase();
        var obj1 = CreateTestObject(1001, "FirstObject");
        var obj2 = CreateTestObject(1002, "SecondObject");
        
        objectDb.LoadObject(obj1);
        objectDb.LoadObject(obj2);
        
        var retrieved = objectDb.GetObject(1001);
        Assert.NotNull(retrieved);
        Assert.Equal("FirstObject", retrieved.Name.Split(' ').Last());
        
        var retrieved2 = objectDb.GetObject(1002);
        Assert.NotNull(retrieved2);
        Assert.Equal("SecondObject", retrieved2.Name.Split(' ').Last());
    }
    
    [Fact]
    public void ObjectDatabase_GetObject_NonExistent_ShouldReturnNull()
    {
        var objectDb = new ObjectDatabase();
        
        var result = objectDb.GetObject(99999);
        
        Assert.Null(result);
    }
    
    [Fact]
    public void ObjectDatabase_LoadMultipleObjects_ShouldHandleCorrectly()
    {
        var objectDb = new ObjectDatabase();
        var objects = Enumerable.Range(1, 100)
            .Select(i => CreateTestObject(i, $"Object{i}"))
            .ToArray();
        
        // Load all objects
        foreach (var obj in objects)
        {
            objectDb.LoadObject(obj);
        }
        
        // Verify all objects can be retrieved
        for (int i = 1; i <= 100; i++)
        {
            var retrieved = objectDb.GetObject(i);
            Assert.NotNull(retrieved);
            Assert.Equal(i, retrieved.VirtualNumber);
        }
        
        // Verify count
        Assert.Equal(100, objectDb.ObjectCount);
    }
    
    [Fact]
    public void ObjectDatabase_ThreadSafety_ShouldHandleConcurrentAccess()
    {
        var objectDb = new ObjectDatabase();
        var results = new ConcurrentBag<bool>();
        const int threadCount = 10;
        const int objectsPerThread = 50;
        
        // Create objects concurrently from multiple threads
        var loadTasks = Enumerable.Range(0, threadCount).Select(threadId =>
            Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < objectsPerThread; i++)
                    {
                        var vnum = threadId * objectsPerThread + i;
                        var obj = CreateTestObject(vnum, $"ThreadObject{vnum}");
                        objectDb.LoadObject(obj);
                    }
                    results.Add(true);
                }
                catch
                {
                    results.Add(false);
                }
            })
        ).ToArray();
        
        Task.WaitAll(loadTasks);
        
        // Verify all thread operations succeeded
        Assert.Equal(threadCount, results.Count(r => r));
        
        // Verify all objects can be retrieved concurrently
        var readTasks = Enumerable.Range(0, threadCount * objectsPerThread).Select(vnum =>
            Task.Run(() =>
            {
                try
                {
                    var obj = objectDb.GetObject(vnum);
                    return obj != null && obj.VirtualNumber == vnum;
                }
                catch
                {
                    return false;
                }
            })
        ).ToArray();
        
        Task.WaitAll(readTasks);
        
        // All reads should succeed
        Assert.True(readTasks.All(t => t.Result));
        
        // Verify final count
        Assert.Equal(threadCount * objectsPerThread, objectDb.ObjectCount);
    }
    
    [Fact]
    public void ObjectDatabase_LoadObject_ShouldOverwriteExistingObject()
    {
        var objectDb = new ObjectDatabase();
        var original = CreateTestObject(5000, "Original");
        var updated = CreateTestObject(5000, "Updated");
        
        objectDb.LoadObject(original);
        var first = objectDb.GetObject(5000);
        Assert.Equal("test object Original", first.Name);
        
        objectDb.LoadObject(updated);
        var second = objectDb.GetObject(5000);
        Assert.Equal("test object Updated", second.Name);
        
        // Count should still be 1
        Assert.Equal(1, objectDb.ObjectCount);
    }
    
    [Fact]
    public void ObjectDatabase_GetAllObjects_ShouldReturnAllStoredObjects()
    {
        var objectDb = new ObjectDatabase();
        var objects = new[]
        {
            CreateTestObject(100, "FirstObject"),
            CreateTestObject(200, "SecondObject"),
            CreateTestObject(300, "ThirdObject")
        };
        
        foreach (var obj in objects)
        {
            objectDb.LoadObject(obj);
        }
        
        var allObjects = objectDb.GetAllObjects().ToArray();
        
        Assert.Equal(3, allObjects.Length);
        Assert.Contains(allObjects, o => o.VirtualNumber == 100);
        Assert.Contains(allObjects, o => o.VirtualNumber == 200);
        Assert.Contains(allObjects, o => o.VirtualNumber == 300);
    }
    
    [Fact]
    public void ObjectDatabase_Performance_ShouldMeetRequirements()
    {
        var objectDb = new ObjectDatabase();
        const int objectCount = 1000;
        
        // Load many objects
        var loadStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < objectCount; i++)
        {
            var obj = CreateTestObject(i, $"PerfObject{i}");
            objectDb.LoadObject(obj);
        }
        loadStopwatch.Stop();
        
        // Test lookup performance
        var lookupStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < objectCount; i++)
        {
            var obj = objectDb.GetObject(i);
            Assert.NotNull(obj);
        }
        lookupStopwatch.Stop();
        
        // Each lookup should be under 1ms (target: O(1) Dictionary lookup)
        var averageLookupTime = lookupStopwatch.ElapsedMilliseconds / (double)objectCount;
        Assert.True(averageLookupTime < 1.0, $"Average lookup time was {averageLookupTime}ms, should be < 1ms");
        
        // Load time should be reasonable
        Assert.True(loadStopwatch.ElapsedMilliseconds < 1000, $"Load time was {loadStopwatch.ElapsedMilliseconds}ms, should be < 1000ms for {objectCount} objects");
    }
}