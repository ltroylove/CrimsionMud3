using Xunit;
using C3Mud.Core.World.Services;
using C3Mud.Core.World.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;

namespace C3Mud.Tests.World;
public class MobileDatabaseTests
{
    private Mobile CreateTestMobile(int vnum, string name)
    {
        return new Mobile
        {
            VirtualNumber = vnum,
            Keywords = $"test mobile {name}",
            ShortDescription = $"A test mobile named {name}",
            LongDescription = $"A test mobile named {name} stands here.",
            DetailedDescription = $"This is {name}, a test mobile for unit testing.",
            Level = 10,
            MaxHitPoints = 100,
            MaxMana = 50,
            ArmorClass = 5,
            DamageRoll = "2d4+2",
            BonusDamageRoll = "1d2+1",
            Experience = 1000,
            Gold = 100,
            Alignment = 0
        };
    }
    
    [Fact]
    public void MobileDatabase_LoadMobile_ShouldStoreCorrectly()
    {
        var mobileDb = new MobileDatabase();
        var mobile = CreateTestMobile(12345, "TestMobile");
        
        mobileDb.LoadMobile(mobile);
        
        var retrieved = mobileDb.GetMobile(12345);
        Assert.NotNull(retrieved);
        Assert.Equal(12345, retrieved.VirtualNumber);
        Assert.Equal("A test mobile named TestMobile", retrieved.ShortDescription);
    }
    
    [Fact]
    public void MobileDatabase_GetMobile_ShouldReturnCorrectMobile()
    {
        var mobileDb = new MobileDatabase();
        var mobile1 = CreateTestMobile(12345, "First");
        var mobile2 = CreateTestMobile(12346, "Second");
        
        mobileDb.LoadMobile(mobile1);
        mobileDb.LoadMobile(mobile2);
        
        var retrieved1 = mobileDb.GetMobile(12345);
        var retrieved2 = mobileDb.GetMobile(12346);
        
        Assert.NotNull(retrieved1);
        Assert.NotNull(retrieved2);
        Assert.Equal("A test mobile named First", retrieved1.ShortDescription);
        Assert.Equal("A test mobile named Second", retrieved2.ShortDescription);
    }
    
    [Fact]
    public void MobileDatabase_GetMobile_NonExistent_ShouldReturnNull()
    {
        var mobileDb = new MobileDatabase();
        
        var result = mobileDb.GetMobile(99999);
        
        Assert.Null(result);
    }
    
    [Fact]
    public void MobileDatabase_LoadMultipleMobiles_ShouldHandleCorrectly()
    {
        var mobileDb = new MobileDatabase();
        var mobiles = new[]
        {
            CreateTestMobile(1001, "Mob1"),
            CreateTestMobile(1002, "Mob2"),
            CreateTestMobile(1003, "Mob3"),
            CreateTestMobile(1004, "Mob4"),
            CreateTestMobile(1005, "Mob5")
        };
        
        foreach (var mobile in mobiles)
        {
            mobileDb.LoadMobile(mobile);
        }
        
        Assert.Equal(5, mobileDb.GetMobileCount());
        
        foreach (var mobile in mobiles)
        {
            var retrieved = mobileDb.GetMobile(mobile.VirtualNumber);
            Assert.NotNull(retrieved);
            Assert.Equal(mobile.ShortDescription, retrieved.ShortDescription);
        }
    }
    
    [Fact]
    public void MobileDatabase_ThreadSafety_ShouldHandleConcurrentAccess()
    {
        var mobileDb = new MobileDatabase();
        var exceptions = new ConcurrentBag<Exception>();
        
        // Create test data
        var testMobiles = Enumerable.Range(1, 100)
            .Select(i => CreateTestMobile(i, $"Mobile{i}"))
            .ToList();
        
        // Test concurrent loading
        Parallel.ForEach(testMobiles, mobile =>
        {
            try
            {
                mobileDb.LoadMobile(mobile);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });
        
        Assert.Equal(0, exceptions.Count);
        Assert.Equal(100, mobileDb.GetMobileCount());
        
        // Test concurrent reading
        Parallel.ForEach(testMobiles, mobile =>
        {
            try
            {
                var retrieved = mobileDb.GetMobile(mobile.VirtualNumber);
                Assert.NotNull(retrieved);
                Assert.Equal(mobile.VirtualNumber, retrieved.VirtualNumber);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });
        
        Assert.Equal(0, exceptions.Count);
    }
    
    [Fact]
    public void MobileDatabase_PerformanceTest_LookupUnder1ms()
    {
        var mobileDb = new MobileDatabase();
        
        // Add 1000 test mobiles
        for (int i = 1; i <= 1000; i++)
        {
            mobileDb.LoadMobile(CreateTestMobile(i, $"Mobile{i}"));
        }
        
        var random = new Random();
        var stopwatch = Stopwatch.StartNew();
        
        // Perform 100 random lookups
        for (int i = 0; i < 100; i++)
        {
            var vnum = random.Next(1, 1001);
            var mobile = mobileDb.GetMobile(vnum);
            Assert.NotNull(mobile);
        }
        
        stopwatch.Stop();
        var avgTime = stopwatch.ElapsedMilliseconds / 100.0;
        
        Assert.True(avgTime < 1.0, $"Average lookup time {avgTime}ms exceeds 1ms target");
    }
    
    [Fact]
    public void MobileDatabase_GetAllMobiles_ReturnsAllLoadedMobiles()
    {
        var mobileDb = new MobileDatabase();
        var testMobiles = new[]
        {
            CreateTestMobile(2001, "Alpha"),
            CreateTestMobile(2002, "Beta"),
            CreateTestMobile(2003, "Gamma")
        };
        
        foreach (var mobile in testMobiles)
        {
            mobileDb.LoadMobile(mobile);
        }
        
        var allMobiles = mobileDb.GetAllMobiles().ToList();
        
        Assert.Equal(3, allMobiles.Count);
        Assert.True(allMobiles.Any(m => m.VirtualNumber == 2001));
        Assert.True(allMobiles.Any(m => m.VirtualNumber == 2002));
        Assert.True(allMobiles.Any(m => m.VirtualNumber == 2003));
    }
    
    [Fact]
    public void MobileDatabase_IsMobileLoaded_ReturnsCorrectStatus()
    {
        var mobileDb = new MobileDatabase();
        var mobile = CreateTestMobile(3001, "TestExists");
        
        Assert.False(mobileDb.IsMobileLoaded(3001));
        
        mobileDb.LoadMobile(mobile);
        
        Assert.True(mobileDb.IsMobileLoaded(3001));
        Assert.False(mobileDb.IsMobileLoaded(3002));
    }
    
    [Fact]
    public async Task MobileDatabase_LoadMobilesAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        // Test that LoadMobilesAsync throws FileNotFoundException for non-existent files
        var mobileDb = new MobileDatabase();
        
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            mobileDb.LoadMobilesAsync("test/path/test.mob"));
    }
    
    [Fact]
    public void MobileDatabase_CreateMobileInstance_ReturnsNewInstance()
    {
        var mobileDb = new MobileDatabase();
        var template = CreateTestMobile(4001, "Template");
        template.MaxHitPoints = 200;
        template.Gold = 500;
        
        mobileDb.LoadMobile(template);
        
        var instance = mobileDb.CreateMobileInstance(4001);
        
        Assert.NotNull(instance);
        Assert.Equal(template.VirtualNumber, instance.VirtualNumber);
        Assert.Equal(template.MaxHitPoints, instance.MaxHitPoints);
        Assert.Equal(template.Gold, instance.Gold);
        
        // Verify it's a different object (not the same reference)
        Assert.NotSame(template, instance);
    }
    
    [Fact]
    public void MobileDatabase_CreateMobileInstance_NonExistent_ReturnsNull()
    {
        var mobileDb = new MobileDatabase();
        
        var instance = mobileDb.CreateMobileInstance(99999);
        
        Assert.Null(instance);
    }
}