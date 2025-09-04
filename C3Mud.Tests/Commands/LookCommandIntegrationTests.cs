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
/// Tests for LookCommand integration with real world data
/// Iteration 3.1: Command Integration - Look Command (Day 7)
/// Following TDD Red Phase - these tests should fail initially
/// </summary>
public class LookCommandIntegrationTests
{
    private readonly Mock<IPlayer> _mockPlayer;
    private readonly Mock<IConnectionDescriptor> _mockConnection;
    private readonly Mock<IWorldDatabase> _mockWorldDatabase;
    private readonly List<string> _sentMessages;

    public LookCommandIntegrationTests()
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

        _mockWorldDatabase = new Mock<IWorldDatabase>();
    }

    [Fact]
    public async Task LookCommand_WithRealRoom_DisplaysCorrectRoomInfo()
    {
        // Arrange - Create real room data from 15Rooms.wld
        var testRoom = new Room
        {
            VirtualNumber = 20385,
            Name = "Path through the hills",
            Description = "The path leads north and south. South leads towards a temple while north leads to a larger road."
        };
        testRoom.Exits.Add(Direction.North, new Exit { TargetRoomVnum = 4938 });
        testRoom.Exits.Add(Direction.South, new Exit { TargetRoomVnum = 20386 });
        
        _mockWorldDatabase.Setup(w => w.GetRoom(20385)).Returns(testRoom);
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(20385);
        
        var lookCommand = new LookCommand(_mockWorldDatabase.Object);
        
        // Act
        await lookCommand.ExecuteAsync(_mockPlayer.Object, "", 15);
        
        // Assert
        var allMessages = string.Join("\n", _sentMessages);
        allMessages.Should().Contain("Path through the hills", "Should show real room name from world data");
        allMessages.Should().Contain("The path leads north and south", "Should show real room description");
        allMessages.Should().NotContain("placeholder", "Should not show placeholder content anymore");
        allMessages.Should().NotContain("basic testing room", "Should not show placeholder room content");
    }

    [Fact]
    public async Task LookCommand_WithExits_DisplaysAvailableExits()
    {
        // Arrange - Room with multiple exits
        var testRoom = new Room
        {
            VirtualNumber = 20385,
            Name = "Path through the hills",
            Description = "Test room with exits"
        };
        testRoom.Exits.Add(Direction.North, new Exit { TargetRoomVnum = 4938 });
        testRoom.Exits.Add(Direction.South, new Exit { TargetRoomVnum = 20386 });
        
        _mockWorldDatabase.Setup(w => w.GetRoom(20385)).Returns(testRoom);
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(20385);
        
        var lookCommand = new LookCommand(_mockWorldDatabase.Object);
        
        // Act
        await lookCommand.ExecuteAsync(_mockPlayer.Object, "", 15);
        
        // Assert
        var allMessages = string.Join("\n", _sentMessages);
        allMessages.Should().Contain("north", "Should show north exit");
        allMessages.Should().Contain("south", "Should show south exit");
        allMessages.Should().Contain("Exits:", "Should show exits section");
    }

    [Fact]
    public async Task LookCommand_InvalidRoom_HandlesGracefully()
    {
        // Arrange - Player in room that doesn't exist
        _mockWorldDatabase.Setup(w => w.GetRoom(99999)).Returns((Room?)null);
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(99999);
        
        var lookCommand = new LookCommand(_mockWorldDatabase.Object);
        
        // Act
        await lookCommand.ExecuteAsync(_mockPlayer.Object, "", 15);
        
        // Assert
        var allMessages = string.Join("\n", _sentMessages);
        allMessages.Should().Contain("floating in the void", "Should handle missing room gracefully");
    }

    [Fact]
    public async Task LookCommand_EmptyRoom_DisplaysBasicInfo()
    {
        // Arrange - Room with no exits
        var testRoom = new Room
        {
            VirtualNumber = 12345,
            Name = "Empty Test Room",
            Description = "A room with no exits."
        };
        // No exits added
        
        _mockWorldDatabase.Setup(w => w.GetRoom(12345)).Returns(testRoom);
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(12345);
        
        var lookCommand = new LookCommand(_mockWorldDatabase.Object);
        
        // Act
        await lookCommand.ExecuteAsync(_mockPlayer.Object, "", 15);
        
        // Assert
        var allMessages = string.Join("\n", _sentMessages);
        allMessages.Should().Contain("Empty Test Room", "Should show room name");
        allMessages.Should().Contain("A room with no exits", "Should show room description");
        allMessages.Should().Contain("None", "Should show 'None' for exits when no exits available");
    }

    [Fact]
    public void LookCommand_Constructor_RequiresWorldDatabase()
    {
        // This test verifies that LookCommand requires IWorldDatabase dependency
        // Currently will fail because LookCommand doesn't have constructor with IWorldDatabase
        
        // Arrange & Act & Assert
        var action = () => new LookCommand(_mockWorldDatabase.Object);
        
        // This should not throw once we implement the constructor
        action.Should().NotThrow("LookCommand should accept IWorldDatabase in constructor");
    }

    private string GetPlayerOutput()
    {
        return string.Join("\n", _sentMessages);
    }
}