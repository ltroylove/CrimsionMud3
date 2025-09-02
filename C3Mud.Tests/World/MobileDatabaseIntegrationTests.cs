using Xunit;
using C3Mud.Core.World.Services;
using C3Mud.Core.World.Models;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace C3Mud.Tests.World;

/// <summary>
/// Integration tests for MobileDatabase using real .mob files
/// These tests verify that the mobile database can handle actual CircleMUD mobile files
/// </summary>
public class MobileDatabaseIntegrationTests
{
    private const string TestMobFile = @"Original-Code\dev\lib\areas\Aerie.mob";
    
    [Fact]
    public async Task MobileDatabase_LoadAerieMobFile_ShouldLoadCorrectly()
    {
        // Skip test if the file doesn't exist (for CI environments)
        if (!File.Exists(TestMobFile))
        {
            return; // Skip test
        }
        
        var mobileDb = new MobileDatabase();
        
        await mobileDb.LoadMobilesAsync(TestMobFile);
        
        // Verify that some mobiles were loaded
        Assert.True(mobileDb.GetMobileCount() > 0);
        
        // Test specific mobiles from the Aerie.mob file
        var jotun = mobileDb.GetMobile(7750);
        Assert.NotNull(jotun);
        Assert.Equal("mob jotun storm giant", jotun.Keywords);
        Assert.Equal(42, jotun.Level);
        Assert.Equal(1000000, jotun.Experience);
        Assert.Equal(400000, jotun.Gold);
        
        var jerial = mobileDb.GetMobile(7751);
        Assert.NotNull(jerial);
        Assert.Equal("mob jerial elven representative elf", jerial.Keywords);
        Assert.Equal(36, jerial.Level);
        
        var viana = mobileDb.GetMobile(7753);
        Assert.NotNull(viana);
        Assert.Equal("mob viana divine light", viana.Keywords);
        Assert.Equal(48, viana.Level);
        Assert.True(viana.Skills.Count > 0);
        Assert.Contains("SPELL_ELSEWHERE", viana.Skills.Keys);
    }
    
    [Fact]
    public async Task MobileDatabase_CreateInstanceFromAerie_ShouldWorkCorrectly()
    {
        // Skip test if the file doesn't exist (for CI environments)
        if (!File.Exists(TestMobFile))
        {
            return; // Skip test
        }
        
        var mobileDb = new MobileDatabase();
        await mobileDb.LoadMobilesAsync(TestMobFile);
        
        // Test creating instances from templates
        var jotunTemplate = mobileDb.GetMobile(7750);
        var jotunInstance1 = mobileDb.CreateMobileInstance(7750);
        var jotunInstance2 = mobileDb.CreateMobileInstance(7750);
        
        Assert.NotNull(jotunTemplate);
        Assert.NotNull(jotunInstance1);
        Assert.NotNull(jotunInstance2);
        
        // Verify instances have same stats as template
        Assert.Equal(jotunTemplate.VirtualNumber, jotunInstance1.VirtualNumber);
        Assert.Equal(jotunTemplate.Level, jotunInstance1.Level);
        Assert.Equal(jotunTemplate.MaxHitPoints, jotunInstance1.MaxHitPoints);
        
        // Verify instances are different objects
        Assert.NotSame(jotunTemplate, jotunInstance1);
        Assert.NotSame(jotunInstance1, jotunInstance2);
        
        // Verify skills are copied correctly
        Assert.Equal(jotunTemplate.Skills.Count, jotunInstance1.Skills.Count);
        if (jotunTemplate.Skills.Any())
        {
            var firstSkill = jotunTemplate.Skills.First();
            Assert.Equal(firstSkill.Value, jotunInstance1.Skills[firstSkill.Key]);
        }
    }
    
    [Fact]
    public async Task MobileDatabase_LoadMultipleMobFiles_ShouldHandleCorrectly()
    {
        var mobileDb = new MobileDatabase();
        var loadedCount = 0;
        
        // Try to load multiple .mob files if they exist
        var mobFiles = new[]
        {
            @"Original-Code\dev\lib\areas\Aerie.mob",
            @"Original-Code\dev\lib\areas\Midgaard.mob",
            @"Original-Code\dev\lib\areas\NewbieVillage.mob"
        };
        
        foreach (var mobFile in mobFiles)
        {
            if (File.Exists(mobFile))
            {
                await mobileDb.LoadMobilesAsync(mobFile);
                loadedCount++;
            }
        }
        
        if (loadedCount > 0)
        {
            // Should have loaded some mobiles
            Assert.True(mobileDb.GetMobileCount() > 0);
            
            // All loaded mobiles should be accessible
            var allMobiles = mobileDb.GetAllMobiles().ToList();
            Assert.True(allMobiles.Count > 0);
            
            // Test that we can create instances from loaded mobiles
            foreach (var mobile in allMobiles.Take(5)) // Test first 5 mobiles
            {
                var instance = mobileDb.CreateMobileInstance(mobile.VirtualNumber);
                Assert.NotNull(instance);
                Assert.Equal(mobile.VirtualNumber, instance.VirtualNumber);
            }
        }
    }
    
    [Fact]
    public async Task MobileDatabase_PerformanceWithRealData_ShouldMeetTargets()
    {
        // Skip test if the file doesn't exist (for CI environments)
        if (!File.Exists(TestMobFile))
        {
            return; // Skip test
        }
        
        var mobileDb = new MobileDatabase();
        
        // Test loading performance
        var loadStart = System.Diagnostics.Stopwatch.StartNew();
        await mobileDb.LoadMobilesAsync(TestMobFile);
        loadStart.Stop();
        
        // Loading should be reasonably fast (under 5 seconds for a single file)
        Assert.True(loadStart.ElapsedMilliseconds < 5000, 
            $"Loading took {loadStart.ElapsedMilliseconds}ms, expected under 5000ms");
        
        // Test lookup performance
        var lookupStart = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            var mobile = mobileDb.GetMobile(7750); // Jotun
            Assert.NotNull(mobile);
        }
        lookupStart.Stop();
        
        var avgLookupTime = lookupStart.ElapsedMilliseconds / 100.0;
        Assert.True(avgLookupTime < 1.0, 
            $"Average lookup time {avgLookupTime}ms exceeds 1ms target");
    }
}