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
/// TDD Red Phase tests for movement commands
/// These tests should fail initially until movement commands are implemented
/// Based on Iteration 3.2: Movement Commands (Day 8) requirements
/// </summary>
public class MovementCommandTests
{
    private readonly Mock<IPlayer> _mockPlayer;
    private readonly Mock<IConnectionDescriptor> _mockConnection;
    private readonly Mock<IWorldDatabase> _mockWorldDatabase;
    private readonly Room _room20385;
    private readonly Room _room20386;
    private readonly Room _room20387;
    private readonly Room _room4938;

    public MovementCommandTests()
    {
        _mockConnection = new Mock<IConnectionDescriptor>();
        _mockConnection.Setup(c => c.IsConnected).Returns(true);

        _mockPlayer = new Mock<IPlayer>();
        _mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        _mockPlayer.Setup(p => p.Level).Returns(5);
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Standing);
        _mockPlayer.Setup(p => p.IsConnected).Returns(true);
        _mockPlayer.Setup(p => p.Connection).Returns(_mockConnection.Object);
        _mockPlayer.SetupProperty(p => p.CurrentRoomVnum); // Allow property to be set and tracked

        _mockWorldDatabase = new Mock<IWorldDatabase>();

        // Set up test rooms based on 15Rooms.wld structure
        _room20385 = new Room
        {
            VirtualNumber = 20385,
            Name = "Path through the hills",
            Description = "The path leads north and south. South leads towards a temple while north leads to a larger road.",
            Exits = new Dictionary<Direction, Exit>
            {
                { Direction.North, new Exit { Direction = Direction.North, TargetRoomVnum = 4938 } },
                { Direction.South, new Exit { Direction = Direction.South, TargetRoomVnum = 20386 } }
            }
        };

        _room20386 = new Room
        {
            VirtualNumber = 20386,
            Name = "Path through the hills",
            Description = "The path leads north and south. South leads towards a temple while north leads to a larger road.\nThere is a small trail that leads to the east.",
            Exits = new Dictionary<Direction, Exit>
            {
                { Direction.North, new Exit { Direction = Direction.North, TargetRoomVnum = 20385 } },
                { Direction.East, new Exit { Direction = Direction.East, TargetRoomVnum = 9201, Name = "A dark winding road" } },
                { Direction.South, new Exit { Direction = Direction.South, TargetRoomVnum = 20387 } }
            }
        };

        _room20387 = new Room
        {
            VirtualNumber = 20387,
            Name = "Path through the hills",
            Description = "The path leads north and south. South leads towards a temple while north leads to a larger road.",
            Exits = new Dictionary<Direction, Exit>
            {
                { Direction.North, new Exit { Direction = Direction.North, TargetRoomVnum = 20386 } },
                { Direction.South, new Exit { Direction = Direction.South, TargetRoomVnum = 7100 } }
            }
        };

        _room4938 = new Room
        {
            VirtualNumber = 4938,
            Name = "A larger road",
            Description = "This is a larger road connecting to various paths.",
            Exits = new Dictionary<Direction, Exit>
            {
                { Direction.South, new Exit { Direction = Direction.South, TargetRoomVnum = 20385 } }
            }
        };

        // Set up a room for 9201 to test valid east movement
        var room9201 = new Room
        {
            VirtualNumber = 9201,
            Name = "A dark winding road",
            Description = "This is a dark winding road.",
            Exits = new Dictionary<Direction, Exit>()
        };

        // Setup WorldDatabase mock to return our test rooms
        _mockWorldDatabase.Setup(w => w.GetRoom(20385)).Returns(_room20385);
        _mockWorldDatabase.Setup(w => w.GetRoom(20386)).Returns(_room20386);
        _mockWorldDatabase.Setup(w => w.GetRoom(20387)).Returns(_room20387);
        _mockWorldDatabase.Setup(w => w.GetRoom(4938)).Returns(_room4938);
        _mockWorldDatabase.Setup(w => w.GetRoom(9201)).Returns(room9201); // Valid room for east movement
        _mockWorldDatabase.Setup(w => w.GetRoom(99999)).Returns((Room?)null); // Non-existent room for testing
    }

    #region North Command Tests

    [Fact]
    public async Task NorthCommand_ValidExit_ShouldMovePlayer()
    {
        // Arrange
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(20385);
        var northCommand = new NorthCommand(_mockWorldDatabase.Object);

        // Act
        await northCommand.ExecuteAsync(_mockPlayer.Object, "", 1);

        // Assert
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = 4938, Times.Once);
        _mockPlayer.Verify(p => p.SendMessageAsync("You head north."), Times.Once);
    }

    [Fact]
    public async Task NorthCommand_InvalidExit_ShouldShowMessage()
    {
        // Arrange - Create completely fresh mocks to avoid conflicts
        var freshMockConnection = new Mock<IConnectionDescriptor>();
        freshMockConnection.Setup(c => c.IsConnected).Returns(true);
        
        var freshMockWorldDatabase = new Mock<IWorldDatabase>();
        var freshMockPlayer = new Mock<IPlayer>();
        
        // Create a room with no north exit
        var roomNoNorthExit = new Room
        {
            VirtualNumber = 99998,
            Name = "Dead End Room",
            Description = "This room has no north exit.",
            Exits = new Dictionary<Direction, Exit>
            {
                { Direction.South, new Exit { Direction = Direction.South, TargetRoomVnum = 20385 } }
            }
        };
        
        // Setup fresh mocks
        freshMockWorldDatabase.Setup(w => w.GetRoom(99998)).Returns(roomNoNorthExit);
        freshMockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        freshMockPlayer.Setup(p => p.Level).Returns(5);
        freshMockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Standing);
        freshMockPlayer.Setup(p => p.IsConnected).Returns(true);
        freshMockPlayer.Setup(p => p.Connection).Returns(freshMockConnection.Object);
        freshMockPlayer.SetupProperty(p => p.CurrentRoomVnum, 99998);
        
        var northCommand = new NorthCommand(freshMockWorldDatabase.Object);

        // Act
        await northCommand.ExecuteAsync(freshMockPlayer.Object, "", 1);

        // Assert
        freshMockPlayer.VerifySet(p => p.CurrentRoomVnum = It.IsAny<int>(), Times.Never);
        freshMockPlayer.Verify(p => p.SendMessageAsync("You can't go that way."), Times.Once);
    }

    #endregion

    #region South Command Tests

    [Fact]
    public async Task SouthCommand_ValidExit_ShouldMovePlayer()
    {
        // Arrange
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(20385);
        var southCommand = new SouthCommand(_mockWorldDatabase.Object);

        // Act
        await southCommand.ExecuteAsync(_mockPlayer.Object, "", 3);

        // Assert
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = 20386, Times.Once);
        _mockPlayer.Verify(p => p.SendMessageAsync("You head south."), Times.Once);
    }

    [Fact]
    public async Task SouthCommand_InvalidExit_ShouldShowMessage()
    {
        // Arrange - Create a room with no south exit
        var roomNoSouthExit = new Room
        {
            VirtualNumber = 99997,
            Name = "Dead End Room",
            Description = "This room has no south exit.",
            Exits = new Dictionary<Direction, Exit>
            {
                { Direction.North, new Exit { Direction = Direction.North, TargetRoomVnum = 20385 } }
            }
        };
        _mockWorldDatabase.Setup(w => w.GetRoom(99997)).Returns(roomNoSouthExit);
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(99997);
        
        var southCommand = new SouthCommand(_mockWorldDatabase.Object);

        // Act
        await southCommand.ExecuteAsync(_mockPlayer.Object, "", 3);

        // Assert
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = It.IsAny<int>(), Times.Never);
        _mockPlayer.Verify(p => p.SendMessageAsync("You can't go that way."), Times.Once);
    }

    #endregion

    #region East Command Tests

    [Fact]
    public async Task EastCommand_ValidExit_ShouldMovePlayer()
    {
        // Arrange
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(20386);
        var eastCommand = new EastCommand(_mockWorldDatabase.Object);

        // Act
        await eastCommand.ExecuteAsync(_mockPlayer.Object, "", 2);

        // Assert
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = 9201, Times.Once);
        _mockPlayer.Verify(p => p.SendMessageAsync("You head east."), Times.Once);
    }

    [Fact]
    public async Task EastCommand_InvalidExit_ShouldShowMessage()
    {
        // Arrange - player in room 20385 which has no east exit
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(20385);
        var eastCommand = new EastCommand(_mockWorldDatabase.Object);

        // Act
        await eastCommand.ExecuteAsync(_mockPlayer.Object, "", 2);

        // Assert
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = It.IsAny<int>(), Times.Never);
        _mockPlayer.Verify(p => p.SendMessageAsync("You can't go that way."), Times.Once);
    }

    #endregion

    #region West Command Tests

    [Fact]
    public async Task WestCommand_ValidExit_ShouldMovePlayer()
    {
        // Arrange - create a room with west exit for testing
        var roomWithWestExit = new Room
        {
            VirtualNumber = 99999,
            Name = "Test Room",
            Description = "A test room with west exit",
            Exits = new Dictionary<Direction, Exit>
            {
                { Direction.West, new Exit { Direction = Direction.West, TargetRoomVnum = 20385 } }
            }
        };
        _mockWorldDatabase.Setup(w => w.GetRoom(99999)).Returns(roomWithWestExit);
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(99999);
        
        var westCommand = new WestCommand(_mockWorldDatabase.Object);

        // Act
        await westCommand.ExecuteAsync(_mockPlayer.Object, "", 4);

        // Assert
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = 20385, Times.Once);
        _mockPlayer.Verify(p => p.SendMessageAsync("You head west."), Times.Once);
    }

    [Fact]
    public async Task WestCommand_InvalidExit_ShouldShowMessage()
    {
        // Arrange - player in room 20385 which has no west exit
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(20385);
        var westCommand = new WestCommand(_mockWorldDatabase.Object);

        // Act
        await westCommand.ExecuteAsync(_mockPlayer.Object, "", 4);

        // Assert
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = It.IsAny<int>(), Times.Never);
        _mockPlayer.Verify(p => p.SendMessageAsync("You can't go that way."), Times.Once);
    }

    #endregion

    #region Up Command Tests

    [Fact]
    public async Task UpCommand_ValidExit_ShouldMovePlayer()
    {
        // Arrange - create a room with up exit for testing
        var roomWithUpExit = new Room
        {
            VirtualNumber = 99998,
            Name = "Test Room",
            Description = "A test room with up exit",
            Exits = new Dictionary<Direction, Exit>
            {
                { Direction.Up, new Exit { Direction = Direction.Up, TargetRoomVnum = 20385 } }
            }
        };
        _mockWorldDatabase.Setup(w => w.GetRoom(99998)).Returns(roomWithUpExit);
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(99998);
        
        var upCommand = new UpCommand(_mockWorldDatabase.Object);

        // Act
        await upCommand.ExecuteAsync(_mockPlayer.Object, "", 5);

        // Assert
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = 20385, Times.Once);
        _mockPlayer.Verify(p => p.SendMessageAsync("You head up."), Times.Once);
    }

    [Fact]
    public async Task UpCommand_InvalidExit_ShouldShowMessage()
    {
        // Arrange - player in room 20385 which has no up exit
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(20385);
        var upCommand = new UpCommand(_mockWorldDatabase.Object);

        // Act
        await upCommand.ExecuteAsync(_mockPlayer.Object, "", 5);

        // Assert
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = It.IsAny<int>(), Times.Never);
        _mockPlayer.Verify(p => p.SendMessageAsync("You can't go that way."), Times.Once);
    }

    #endregion

    #region Down Command Tests

    [Fact]
    public async Task DownCommand_ValidExit_ShouldMovePlayer()
    {
        // Arrange - create a room with down exit for testing
        var roomWithDownExit = new Room
        {
            VirtualNumber = 99997,
            Name = "Test Room",
            Description = "A test room with down exit",
            Exits = new Dictionary<Direction, Exit>
            {
                { Direction.Down, new Exit { Direction = Direction.Down, TargetRoomVnum = 20385 } }
            }
        };
        _mockWorldDatabase.Setup(w => w.GetRoom(99997)).Returns(roomWithDownExit);
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(99997);
        
        var downCommand = new DownCommand(_mockWorldDatabase.Object);

        // Act
        await downCommand.ExecuteAsync(_mockPlayer.Object, "", 6);

        // Assert
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = 20385, Times.Once);
        _mockPlayer.Verify(p => p.SendMessageAsync("You head down."), Times.Once);
    }

    [Fact]
    public async Task DownCommand_InvalidExit_ShouldShowMessage()
    {
        // Arrange - player in room 20385 which has no down exit
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(20385);
        var downCommand = new DownCommand(_mockWorldDatabase.Object);

        // Act
        await downCommand.ExecuteAsync(_mockPlayer.Object, "", 6);

        // Assert
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = It.IsAny<int>(), Times.Never);
        _mockPlayer.Verify(p => p.SendMessageAsync("You can't go that way."), Times.Once);
    }

    #endregion

    #region Movement Integration Tests

    [Fact]
    public async Task MovementCommand_UpdatesPlayerLocation()
    {
        // Arrange
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(20385);
        var northCommand = new NorthCommand(_mockWorldDatabase.Object);

        // Act
        await northCommand.ExecuteAsync(_mockPlayer.Object, "", 1);

        // Assert - Player location should be updated
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = 4938, Times.Once);
    }

    [Fact]
    public async Task MovementCommand_ShowsNewRoomAfterMovement()
    {
        // Arrange
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(20385);
        var northCommand = new NorthCommand(_mockWorldDatabase.Object);

        // Act
        await northCommand.ExecuteAsync(_mockPlayer.Object, "", 1);

        // Assert - Should show movement message and room description
        _mockPlayer.Verify(p => p.SendMessageAsync("You head north."), Times.Once);
        // The command should trigger a look at the new room - showing the room name
        _mockPlayer.Verify(p => p.SendFormattedMessageAsync(It.Is<string>(s => s.Contains("&WA larger road&N"))), Times.Once);
        // Should also show the room description
        _mockPlayer.Verify(p => p.SendFormattedMessageAsync(It.Is<string>(s => s.Contains("This is a larger road connecting to various paths."))), Times.Once);
    }

    [Fact]
    public async Task MovementCommand_TargetRoomNotExists_ShouldShowError()
    {
        // Arrange - set up a room with an exit to a non-existent room
        var roomWithBadExit = new Room
        {
            VirtualNumber = 99996,
            Name = "Test Room",
            Description = "A test room with bad exit",
            Exits = new Dictionary<Direction, Exit>
            {
                { Direction.North, new Exit { Direction = Direction.North, TargetRoomVnum = 99999 } } // Non-existent room
            }
        };
        _mockWorldDatabase.Setup(w => w.GetRoom(99996)).Returns(roomWithBadExit);
        _mockWorldDatabase.Setup(w => w.GetRoom(99999)).Returns((Room?)null);
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(99996);
        
        var northCommand = new NorthCommand(_mockWorldDatabase.Object);

        // Act
        await northCommand.ExecuteAsync(_mockPlayer.Object, "", 1);

        // Assert
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = It.IsAny<int>(), Times.Never);
        _mockPlayer.Verify(p => p.SendMessageAsync("That way leads nowhere."), Times.Once);
    }

    #endregion

    #region Command Properties Tests

    [Fact]
    public void NorthCommand_Properties_ShouldBeCorrect()
    {
        // Arrange
        var command = new NorthCommand(_mockWorldDatabase.Object);

        // Assert
        command.Name.Should().Be("north");
        command.Aliases.Should().ContainSingle("n");
        command.MinimumPosition.Should().Be(PlayerPosition.Standing);
        command.MinimumLevel.Should().Be(1);
        command.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void SouthCommand_Properties_ShouldBeCorrect()
    {
        // Arrange
        var command = new SouthCommand(_mockWorldDatabase.Object);

        // Assert
        command.Name.Should().Be("south");
        command.Aliases.Should().ContainSingle("s");
        command.MinimumPosition.Should().Be(PlayerPosition.Standing);
        command.MinimumLevel.Should().Be(1);
        command.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void EastCommand_Properties_ShouldBeCorrect()
    {
        // Arrange
        var command = new EastCommand(_mockWorldDatabase.Object);

        // Assert
        command.Name.Should().Be("east");
        command.Aliases.Should().ContainSingle("e");
        command.MinimumPosition.Should().Be(PlayerPosition.Standing);
        command.MinimumLevel.Should().Be(1);
        command.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void WestCommand_Properties_ShouldBeCorrect()
    {
        // Arrange
        var command = new WestCommand(_mockWorldDatabase.Object);

        // Assert
        command.Name.Should().Be("west");
        command.Aliases.Should().ContainSingle("w");
        command.MinimumPosition.Should().Be(PlayerPosition.Standing);
        command.MinimumLevel.Should().Be(1);
        command.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void UpCommand_Properties_ShouldBeCorrect()
    {
        // Arrange
        var command = new UpCommand(_mockWorldDatabase.Object);

        // Assert
        command.Name.Should().Be("up");
        command.Aliases.Should().ContainSingle("u");
        command.MinimumPosition.Should().Be(PlayerPosition.Standing);
        command.MinimumLevel.Should().Be(1);
        command.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void DownCommand_Properties_ShouldBeCorrect()
    {
        // Arrange
        var command = new DownCommand(_mockWorldDatabase.Object);

        // Assert
        command.Name.Should().Be("down");
        command.Aliases.Should().ContainSingle("d");
        command.MinimumPosition.Should().Be(PlayerPosition.Standing);
        command.MinimumLevel.Should().Be(1);
        command.IsEnabled.Should().BeTrue();
    }

    #endregion
}