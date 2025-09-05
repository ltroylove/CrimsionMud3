using C3Mud.Core.Commands.Movement;
using C3Mud.Core.Networking;
using C3Mud.Core.Players;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace C3Mud.Tests.Commands;

/// <summary>
/// TDD Red Phase tests for Iteration 4: Advanced Movement & Room System features
/// These tests define the advanced movement behaviors we need to implement
/// All tests should FAIL initially - this is expected for TDD Red phase
/// </summary>
public class AdvancedMovementTests
{
    private readonly Mock<IPlayer> _mockPlayer;
    private readonly Mock<IConnectionDescriptor> _mockConnection;
    private readonly Mock<IWorldDatabase> _mockWorldDatabase;
    private readonly NorthCommand _northCommand;

    public AdvancedMovementTests()
    {
        _mockConnection = new Mock<IConnectionDescriptor>();
        _mockConnection.Setup(c => c.IsConnected).Returns(true);

        _mockPlayer = new Mock<IPlayer>();
        _mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        _mockPlayer.Setup(p => p.Level).Returns(10);
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Standing);
        _mockPlayer.Setup(p => p.IsConnected).Returns(true);
        _mockPlayer.Setup(p => p.Connection).Returns(_mockConnection.Object);
        _mockPlayer.SetupProperty(p => p.CurrentRoomVnum);
        
        // Set up the player's SendMessageAsync to call the connection's SendDataAsync
        _mockPlayer.Setup(p => p.SendMessageAsync(It.IsAny<string>()))
            .Callback<string>(message => _mockConnection.Object.SendDataAsync(message + "\r\n").Wait());
        
        _mockPlayer.Setup(p => p.SendFormattedMessageAsync(It.IsAny<string>()))
            .Callback<string>(message => _mockConnection.Object.SendDataAsync(message + "\r\n").Wait());

        _mockWorldDatabase = new Mock<IWorldDatabase>();
        _northCommand = new NorthCommand(_mockWorldDatabase.Object);
    }

    #region Door and Exit Restriction Tests

    [Fact]
    public async Task MoveCommand_ClosedDoor_ShowsClosedDoorMessage()
    {
        // Arrange
        var currentRoom = CreateRoomWithClosedDoor(1001, 1002);
        var targetRoom = CreateBasicRoom(1002, "Target Room");

        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(currentRoom);
        _mockWorldDatabase.Setup(db => db.GetRoom(1002)).Returns(targetRoom);

        // Act
        await _northCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert - Should show door closed message and NOT move player
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("The door is closed"))), Times.Once);
        _mockPlayer.Object.CurrentRoomVnum.Should().Be(1001); // Should not move
    }

    [Fact]
    public async Task MoveCommand_LockedDoor_ShowsLockedDoorMessage()
    {
        // Arrange
        var currentRoom = CreateRoomWithLockedDoor(1001, 1002);
        var targetRoom = CreateBasicRoom(1002, "Target Room");

        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(currentRoom);
        _mockWorldDatabase.Setup(db => db.GetRoom(1002)).Returns(targetRoom);

        // Act
        await _northCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert - Should show locked message and NOT move player
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("The door is locked"))), Times.Once);
        _mockPlayer.Object.CurrentRoomVnum.Should().Be(1001); // Should not move
    }

    [Fact]
    public async Task MoveCommand_PlayerHasKey_UnlocksAndMoves()
    {
        // Arrange
        var currentRoom = CreateRoomWithLockedDoor(1001, 1002, keyVnum: 1050);
        var targetRoom = CreateBasicRoom(1002, "Target Room");
        
        // Player has the key
        _mockPlayer.Setup(p => p.HasItem(1050)).Returns(true);
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(currentRoom);
        _mockWorldDatabase.Setup(db => db.GetRoom(1002)).Returns(targetRoom);

        // Act
        await _northCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert - Should unlock, show unlock message, and move player
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("*Click*"))), Times.Once);
        _mockPlayer.Object.CurrentRoomVnum.Should().Be(1002); // Should move
    }

    [Fact]
    public async Task MoveCommand_NoFlyToAirRoom_ShowsCannotEnterMessage()
    {
        // Arrange
        var currentRoom = CreateBasicRoom(1001, "Ground Level");
        var airRoom = CreateAirRoom(1002, "In the Sky");

        _mockPlayer.Setup(p => p.CanFly).Returns(false); // Player cannot fly
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(currentRoom);
        _mockWorldDatabase.Setup(db => db.GetRoom(1002)).Returns(airRoom);

        // Act
        await _northCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You need to fly to go there"))), Times.Once);
        _mockPlayer.Object.CurrentRoomVnum.Should().Be(1001);
    }

    #endregion

    #region Room Capacity and Restrictions

    [Fact]
    public async Task MoveCommand_RoomAtCapacity_ShowsRoomFullMessage()
    {
        // Arrange
        var currentRoom = CreateBasicRoom(1001, "Current Room");
        var fullRoom = CreateRoomAtCapacity(1002, "Crowded Room");

        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(currentRoom);
        _mockWorldDatabase.Setup(db => db.GetRoom(1002)).Returns(fullRoom);

        // Act
        await _northCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("There's no room for you there"))), Times.Once);
        _mockPlayer.Object.CurrentRoomVnum.Should().Be(1001);
    }

    [Fact]
    public async Task MoveCommand_MinimumLevelRequired_ShowsLevelRestrictMessage()
    {
        // Arrange
        var currentRoom = CreateBasicRoom(1001, "Current Room");
        var restrictedRoom = CreateLevelRestrictedRoom(1002, "High Level Area", minLevel: 20);

        _mockPlayer.Setup(p => p.Level).Returns(10); // Below required level
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(currentRoom);
        _mockWorldDatabase.Setup(db => db.GetRoom(1002)).Returns(restrictedRoom);

        // Act
        await _northCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You are not experienced enough"))), Times.Once);
        _mockPlayer.Object.CurrentRoomVnum.Should().Be(1001);
    }

    #endregion

    #region Movement Messages and Coordination

    [Fact]
    public async Task MoveCommand_OtherPlayersInRoom_ShowsMovementMessage()
    {
        // Arrange
        var currentRoom = CreateRoomWithOtherPlayers(1001, "Start Room");
        var targetRoom = CreateBasicRoom(1002, "Target Room");
        var otherPlayer = CreateMockPlayer("OtherPlayer");

        currentRoom.Players.Add(otherPlayer);
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(currentRoom);
        _mockWorldDatabase.Setup(db => db.GetRoom(1002)).Returns(targetRoom);

        // Act
        await _northCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert - Other player should see movement message
        Mock.Get(otherPlayer.Connection).Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("TestPlayer leaves north"))), Times.Once);
    }

    [Fact]
    public async Task MoveCommand_EnterRoomWithPlayers_ShowsArrivalMessage()
    {
        // Arrange
        var currentRoom = CreateBasicRoom(1001, "Start Room");
        var targetRoom = CreateRoomWithOtherPlayers(1002, "Target Room");
        var otherPlayer = CreateMockPlayer("OtherPlayer");

        targetRoom.Players.Add(otherPlayer);
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(currentRoom);
        _mockWorldDatabase.Setup(db => db.GetRoom(1002)).Returns(targetRoom);

        // Act
        await _northCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert - Other player should see arrival message
        Mock.Get(otherPlayer.Connection).Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("TestPlayer has arrived from the south"))), Times.Once);
    }

    [Fact]
    public async Task MoveCommand_MovementDelay_PreventsFastMovement()
    {
        // Arrange
        var currentRoom = CreateBasicRoom(1001, "Start Room");
        var targetRoom = CreateBasicRoom(1002, "Target Room");

        _mockPlayer.Setup(p => p.LastMovementTime).Returns(DateTime.UtcNow.AddSeconds(-0.5)); // Moved recently
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(currentRoom);
        _mockWorldDatabase.Setup(db => db.GetRoom(1002)).Returns(targetRoom);

        // Act
        await _northCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You can't move that fast"))), Times.Once);
        _mockPlayer.Object.CurrentRoomVnum.Should().Be(1001);
    }

    #endregion

    #region Performance Requirements

    [Fact]
    public async Task MoveCommand_Performance_CompletesUnder50ms()
    {
        // Arrange
        var currentRoom = CreateBasicRoom(1001, "Start Room");
        var targetRoom = CreateBasicRoom(1002, "Target Room");

        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(currentRoom);
        _mockWorldDatabase.Setup(db => db.GetRoom(1002)).Returns(targetRoom);

        // Act & Assert
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _northCommand.ExecuteAsync(_mockPlayer.Object, "", 0);
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50, "Movement should complete within 50ms");
    }

    #endregion

    #region Helper Methods for Test Setup

    private Room CreateRoomWithClosedDoor(int fromVnum, int toVnum)
    {
        return new Room
        {
            VirtualNumber = fromVnum,
            Name = "Room with Closed Door",
            Description = "There is a closed door to the north.",
            Exits = new Dictionary<Direction, Exit>
            {
                {
                    Direction.North, new Exit 
                    { 
                        Direction = Direction.North, 
                        TargetRoomVnum = toVnum,
                        DoorFlags = 1, // EX_CLOSED
                        Name = "door"
                    }
                }
            }
        };
    }

    private Room CreateRoomWithLockedDoor(int fromVnum, int toVnum, int keyVnum = -1)
    {
        return new Room
        {
            VirtualNumber = fromVnum,
            Name = "Room with Locked Door",
            Description = "There is a locked door to the north.",
            Exits = new Dictionary<Direction, Exit>
            {
                {
                    Direction.North, new Exit 
                    { 
                        Direction = Direction.North, 
                        TargetRoomVnum = toVnum,
                        DoorFlags = 3, // EX_CLOSED | EX_LOCKED
                        KeyVnum = keyVnum,
                        Name = "door"
                    }
                }
            }
        };
    }

    private Room CreateAirRoom(int vnum, string name)
    {
        return new Room
        {
            VirtualNumber = vnum,
            Name = name,
            Description = "You are floating in the air.",
            SectorType = SectorType.Flying, // Requires fly to enter
            Exits = new Dictionary<Direction, Exit>()
        };
    }

    private Room CreateRoomAtCapacity(int vnum, string name)
    {
        return new Room
        {
            VirtualNumber = vnum,
            Name = name,
            Description = "This room is completely full of people.",
            MaxPlayers = 1, // Room is at capacity
            Players = new List<IPlayer> { CreateMockPlayer("ExistingPlayer") } // Already has max players
        };
    }

    private Room CreateLevelRestrictedRoom(int vnum, string name, int minLevel)
    {
        return new Room
        {
            VirtualNumber = vnum,
            Name = name,
            Description = "This is a high level area.",
            MinimumLevel = minLevel,
            Exits = new Dictionary<Direction, Exit>()
        };
    }

    private Room CreateRoomWithOtherPlayers(int vnum, string name)
    {
        return new Room
        {
            VirtualNumber = vnum,
            Name = name,
            Description = "A room with other players.",
            Exits = new Dictionary<Direction, Exit>
            {
                {
                    Direction.North, new Exit 
                    { 
                        Direction = Direction.North, 
                        TargetRoomVnum = vnum + 1
                    }
                }
            },
            Players = new List<IPlayer>()
        };
    }

    private Room CreateBasicRoom(int vnum, string name)
    {
        return new Room
        {
            VirtualNumber = vnum,
            Name = name,
            Description = $"This is {name}.",
            Exits = new Dictionary<Direction, Exit>
            {
                {
                    Direction.North, new Exit 
                    { 
                        Direction = Direction.North, 
                        TargetRoomVnum = vnum + 1
                    }
                }
            },
            Players = new List<IPlayer>()
        };
    }

    private IPlayer CreateMockPlayer(string name)
    {
        var mockConnection = new Mock<IConnectionDescriptor>();
        mockConnection.Setup(c => c.IsConnected).Returns(true);

        var mockPlayer = new Mock<IPlayer>();
        mockPlayer.Setup(p => p.Name).Returns(name);
        mockPlayer.Setup(p => p.Level).Returns(5);
        mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Standing);
        mockPlayer.Setup(p => p.IsConnected).Returns(true);
        mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
        mockPlayer.SetupProperty(p => p.CurrentRoomVnum);

        return mockPlayer.Object;
    }

    #endregion
}