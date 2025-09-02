using C3Mud.Core.Commands.Basic;
using C3Mud.Core.Networking;
using C3Mud.Core.Players;
using C3Mud.Core.World.Services;
using C3Mud.Core.World.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace C3Mud.Tests.Commands;

/// <summary>
/// Integration tests for LookCommand with actual 15Rooms.wld world data
/// Tests the complete flow from real world file to displayed room information
/// Iteration 3.1: Command Integration - Look Command (Day 7) - Real World Data Test
/// </summary>
public class LookCommandRealWorldIntegrationTests
{
    private readonly Mock<IPlayer> _mockPlayer;
    private readonly Mock<IConnectionDescriptor> _mockConnection;
    private readonly List<string> _sentMessages;

    public LookCommandRealWorldIntegrationTests()
    {
        _sentMessages = new List<string>();
        
        _mockConnection = new Mock<IConnectionDescriptor>();
        _mockConnection.Setup(c => c.IsConnected).Returns(true);

        _mockPlayer = new Mock<IPlayer>();
        _mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        _mockPlayer.Setup(p => p.Level).Returns(5);
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Standing);
        _mockPlayer.Setup(p => p.IsConnected).Returns(true);
        _mockPlayer.Setup(p => p.Connection).Returns(_mockConnection.Object);
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(20385);
        
        // Capture messages sent to player
        _mockPlayer.Setup(p => p.SendFormattedMessageAsync(It.IsAny<string>()))
                  .Callback<string>(msg => _sentMessages.Add(msg))
                  .Returns(Task.CompletedTask);
        _mockPlayer.Setup(p => p.SendMessageAsync(It.IsAny<string>()))
                  .Callback<string>(msg => _sentMessages.Add(msg))
                  .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task LookCommand_WithReal15RoomsData_ShowsCorrectRoom20385()
    {
        // Arrange - Load actual world database with 15Rooms.wld
        var worldDatabase = new WorldDatabase();
        var testDataPath = Path.Combine(Directory.GetCurrentDirectory(), "Original-Code", "dev", "lib", "areas");
        var filePath = Path.Combine(testDataPath, "15Rooms.wld");
        
        // Skip test if test data file doesn't exist
        if (!File.Exists(filePath))
        {
            Assert.True(true, "Skipping integration test - 15Rooms.wld not found in test data");
            return;
        }
        
        await worldDatabase.LoadRoomsAsync(filePath);
        
        var lookCommand = new LookCommand(worldDatabase);
        
        // Act
        await lookCommand.ExecuteAsync(_mockPlayer.Object, "", 15);
        
        // Assert - Verify room 20385 "Path through the hills" is displayed correctly
        var allMessages = string.Join("\n", _sentMessages);
        
        allMessages.Should().Contain("Path through the hills", 
            "Should show the actual room name from 15Rooms.wld");
        allMessages.Should().Contain("The path leads north and south", 
            "Should show the actual room description from 15Rooms.wld");
        allMessages.Should().Contain("north", "Should show north exit");
        allMessages.Should().Contain("south", "Should show south exit");
        allMessages.Should().Contain("Exits:", "Should show exits section");
        
        // Should NOT contain placeholder content
        allMessages.Should().NotContain("placeholder", "Should not show any placeholder content");
        allMessages.Should().NotContain("basic testing room", "Should not show placeholder room");
        allMessages.Should().NotContain("world system not yet implemented", "Should not show placeholder text");
    }

    [Fact] 
    public async Task LookCommand_WithReal15RoomsData_PerformanceUnder10ms()
    {
        // Arrange - Load actual world database
        var worldDatabase = new WorldDatabase();
        var testDataPath = Path.Combine(Directory.GetCurrentDirectory(), "Original-Code", "dev", "lib", "areas");
        var filePath = Path.Combine(testDataPath, "15Rooms.wld");
        
        // Skip test if test data file doesn't exist
        if (!File.Exists(filePath))
        {
            Assert.True(true, "Skipping performance test - 15Rooms.wld not found in test data");
            return;
        }
        
        await worldDatabase.LoadRoomsAsync(filePath);
        
        var lookCommand = new LookCommand(worldDatabase);
        
        // Act & Assert - Performance test
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        await lookCommand.ExecuteAsync(_mockPlayer.Object, "", 15);
        
        stopwatch.Stop();
        
        // Verify performance target < 10ms as specified in requirements
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10, 
            $"Look command should execute under 10ms, took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task LookCommand_NoRegression_ExistingFunctionalityStillWorks()
    {
        // Arrange - Test backwards compatibility without WorldDatabase
        var lookCommand = new LookCommand(); // No WorldDatabase injected
        
        // Act
        await lookCommand.ExecuteAsync(_mockPlayer.Object, "", 15);
        
        // Assert - Should still show placeholder room for backwards compatibility
        var allMessages = string.Join("\n", _sentMessages);
        
        allMessages.Should().Contain("A Simple Room", "Should show placeholder room name");
        allMessages.Should().Contain("basic testing room", "Should show placeholder description");
        allMessages.Should().Contain("None (world system not yet implemented)", "Should show placeholder exits");
    }

    private string GetPlayerOutput()
    {
        return string.Join("\n", _sentMessages);
    }
}