using C3Mud.Core.Commands.Movement;
using C3Mud.Core.Networking;
using C3Mud.Core.Players;
using C3Mud.Core.World.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace C3Mud.Tests.Integration;

/// <summary>
/// Integration tests for movement commands with real world data
/// Tests the complete TDD cycle implementation of Iteration 3.2: Movement Commands
/// Uses actual 15Rooms.wld data to validate room-to-room navigation
/// </summary>
public class MovementCommandsIntegrationTests
{
    private readonly Mock<IConnectionDescriptor> _mockConnection;
    private readonly Mock<IPlayer> _mockPlayer;
    private readonly WorldDatabase _worldDatabase;

    public MovementCommandsIntegrationTests()
    {
        _mockConnection = new Mock<IConnectionDescriptor>();
        _mockConnection.Setup(c => c.IsConnected).Returns(true);

        _mockPlayer = new Mock<IPlayer>();
        _mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        _mockPlayer.Setup(p => p.Level).Returns(5);
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Standing);
        _mockPlayer.Setup(p => p.IsConnected).Returns(true);
        _mockPlayer.Setup(p => p.Connection).Returns(_mockConnection.Object);
        _mockPlayer.SetupProperty(p => p.CurrentRoomVnum, 20385); // Default starting room

        _worldDatabase = new WorldDatabase();
        LoadTestWorldData();
    }

    private void LoadTestWorldData()
    {
        // Load the actual 15Rooms.wld data structure for testing
        // Based on the real file structure from C:\Projects\C3Mud\Original-Code\dev\lib\areas\15Rooms.wld
        
        // Room 20385: Path through the hills - North to 4938, South to 20386
        var room20385 = new Core.World.Models.Room
        {
            VirtualNumber = 20385,
            Name = "Path through the hills",
            Description = "The path leads north and south. South leads towards a temple while north leads to a larger road.",
            Exits = new Dictionary<Core.World.Models.Direction, Core.World.Models.Exit>
            {
                {
                    Core.World.Models.Direction.North,
                    new Core.World.Models.Exit
                    {
                        Direction = Core.World.Models.Direction.North,
                        TargetRoomVnum = 4938
                    }
                },
                {
                    Core.World.Models.Direction.South,
                    new Core.World.Models.Exit
                    {
                        Direction = Core.World.Models.Direction.South,
                        TargetRoomVnum = 20386
                    }
                }
            }
        };

        // Room 20386: Path through the hills - North to 20385, East to 9201, South to 20387
        var room20386 = new Core.World.Models.Room
        {
            VirtualNumber = 20386,
            Name = "Path through the hills",
            Description = "The path leads north and south. South leads towards a temple while north leads to a larger road.\nThere is a small trail that leads to the east.",
            Exits = new Dictionary<Core.World.Models.Direction, Core.World.Models.Exit>
            {
                {
                    Core.World.Models.Direction.North,
                    new Core.World.Models.Exit
                    {
                        Direction = Core.World.Models.Direction.North,
                        TargetRoomVnum = 20385
                    }
                },
                {
                    Core.World.Models.Direction.East,
                    new Core.World.Models.Exit
                    {
                        Direction = Core.World.Models.Direction.East,
                        Name = "A dark winding road",
                        TargetRoomVnum = 9201
                    }
                },
                {
                    Core.World.Models.Direction.South,
                    new Core.World.Models.Exit
                    {
                        Direction = Core.World.Models.Direction.South,
                        TargetRoomVnum = 20387
                    }
                }
            }
        };

        // Room 20387: Path through the hills - North to 20386, South to 7100
        var room20387 = new Core.World.Models.Room
        {
            VirtualNumber = 20387,
            Name = "Path through the hills",
            Description = "The path leads north and south. South leads towards a temple while north leads to a larger road.",
            Exits = new Dictionary<Core.World.Models.Direction, Core.World.Models.Exit>
            {
                {
                    Core.World.Models.Direction.North,
                    new Core.World.Models.Exit
                    {
                        Direction = Core.World.Models.Direction.North,
                        TargetRoomVnum = 20386
                    }
                },
                {
                    Core.World.Models.Direction.South,
                    new Core.World.Models.Exit
                    {
                        Direction = Core.World.Models.Direction.South,
                        TargetRoomVnum = 7100
                    }
                }
            }
        };

        // Room 4938: A larger road - South to 20385
        var room4938 = new Core.World.Models.Room
        {
            VirtualNumber = 4938,
            Name = "A larger road",
            Description = "This is a larger road connecting to various paths.",
            Exits = new Dictionary<Core.World.Models.Direction, Core.World.Models.Exit>
            {
                {
                    Core.World.Models.Direction.South,
                    new Core.World.Models.Exit
                    {
                        Direction = Core.World.Models.Direction.South,
                        TargetRoomVnum = 20385
                    }
                }
            }
        };

        // Room 9201: A dark winding road (target of east from 20386)
        var room9201 = new Core.World.Models.Room
        {
            VirtualNumber = 9201,
            Name = "A dark winding road",
            Description = "This is a dark winding road.",
            Exits = new Dictionary<Core.World.Models.Direction, Core.World.Models.Exit>()
        };

        // Load all rooms into the database
        _worldDatabase.LoadRoom(room20385);
        _worldDatabase.LoadRoom(room20386);
        _worldDatabase.LoadRoom(room20387);
        _worldDatabase.LoadRoom(room4938);
        _worldDatabase.LoadRoom(room9201);
    }

    [Fact]
    public async Task MovementCommands_With15RoomsData_ShouldNavigateCorrectly()
    {
        // Arrange - Player starts in room 20385
        _mockPlayer.Object.CurrentRoomVnum = 20385;

        // Act & Assert - Test north movement from 20385 to 4938
        var northCommand = new NorthCommand(_worldDatabase);
        await northCommand.ExecuteAsync(_mockPlayer.Object, "", 1);
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = 4938, Times.Once);

        // Act & Assert - Test south movement from 4938 to 20385
        _mockPlayer.Object.CurrentRoomVnum = 4938;
        var southCommand = new SouthCommand(_worldDatabase);
        await southCommand.ExecuteAsync(_mockPlayer.Object, "", 3);
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = 20385, Times.AtLeast(1));

        // Act & Assert - Test south movement from 20385 to 20386
        _mockPlayer.Object.CurrentRoomVnum = 20385;
        await southCommand.ExecuteAsync(_mockPlayer.Object, "", 3);
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = 20386, Times.Once);

        // Act & Assert - Test east movement from 20386 to 9201
        _mockPlayer.Object.CurrentRoomVnum = 20386;
        var eastCommand = new EastCommand(_worldDatabase);
        await eastCommand.ExecuteAsync(_mockPlayer.Object, "", 2);
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = 9201, Times.Once);
    }

    [Fact]
    public async Task MovementCommands_InvalidDirection_ShouldShowErrorMessage()
    {
        // Arrange - Player starts in room 20385 (no west exit)
        _mockPlayer.SetupGet(p => p.CurrentRoomVnum).Returns(20385);

        // Act - Try to move west (invalid direction)  
        var westCommand = new WestCommand(_worldDatabase);
        await westCommand.ExecuteAsync(_mockPlayer.Object, "", 4);

        // Assert - Should show error message and not move
        _mockPlayer.Verify(p => p.SendMessageAsync("You can't go that way."), Times.Once);
        _mockPlayer.VerifySet(p => p.CurrentRoomVnum = It.IsAny<int>(), Times.Never);
    }

    [Fact]
    public void WorldDatabase_Contains15RoomsData()
    {
        // Assert - Verify all test rooms are loaded
        var room20385 = _worldDatabase.GetRoom(20385);
        var room20386 = _worldDatabase.GetRoom(20386);
        var room20387 = _worldDatabase.GetRoom(20387);
        var room4938 = _worldDatabase.GetRoom(4938);
        var room9201 = _worldDatabase.GetRoom(9201);

        room20385.Should().NotBeNull();
        room20385!.Name.Should().Be("Path through the hills");
        room20385.Exits.Should().HaveCount(2); // North and South

        room20386.Should().NotBeNull();
        room20386!.Exits.Should().HaveCount(3); // North, East, South

        room20387.Should().NotBeNull();
        room20387!.Exits.Should().HaveCount(2); // North and South

        room4938.Should().NotBeNull();
        room4938!.Name.Should().Be("A larger road");
        room4938.Exits.Should().HaveCount(1); // South only

        room9201.Should().NotBeNull();
        room9201!.Name.Should().Be("A dark winding road");
    }

    [Fact]
    public async Task MovementCommands_ShowNewRoomDescription()
    {
        // Arrange - Player starts in room 20385
        _mockPlayer.Object.CurrentRoomVnum = 20385;

        // Act - Move north to room 4938
        var northCommand = new NorthCommand(_worldDatabase);
        await northCommand.ExecuteAsync(_mockPlayer.Object, "", 1);

        // Assert - Should show movement message
        _mockPlayer.Verify(p => p.SendMessageAsync("You head north."), Times.Once);

        // Assert - Should show new room name and description
        _mockPlayer.Verify(p => p.SendFormattedMessageAsync(
            It.Is<string>(s => s.Contains("&WA larger road&N"))), Times.Once);
        _mockPlayer.Verify(p => p.SendFormattedMessageAsync(
            It.Is<string>(s => s.Contains("This is a larger road connecting to various paths."))), Times.Once);
    }

    [Theory]
    [InlineData("north", "n")]
    [InlineData("south", "s")]
    [InlineData("east", "e")]
    [InlineData("west", "w")]
    [InlineData("up", "u")]
    [InlineData("down", "d")]
    public void MovementCommands_ShouldHaveCorrectAliases(string commandName, string expectedAlias)
    {
        // Arrange - Create command instance based on name
        MovementCommand command = commandName.ToLowerInvariant() switch
        {
            "north" => new NorthCommand(_worldDatabase),
            "south" => new SouthCommand(_worldDatabase),
            "east" => new EastCommand(_worldDatabase),
            "west" => new WestCommand(_worldDatabase),
            "up" => new UpCommand(_worldDatabase),
            "down" => new DownCommand(_worldDatabase),
            _ => throw new ArgumentException($"Unknown command: {commandName}")
        };

        // Assert
        command.Name.Should().Be(commandName);
        command.Aliases.Should().ContainSingle(expectedAlias);
    }

    [Fact]
    public void MovementCommands_ShouldRequireStandingPosition()
    {
        // Arrange
        var commands = new List<MovementCommand>
        {
            new NorthCommand(_worldDatabase),
            new SouthCommand(_worldDatabase),
            new EastCommand(_worldDatabase),
            new WestCommand(_worldDatabase),
            new UpCommand(_worldDatabase),
            new DownCommand(_worldDatabase)
        };

        // Assert - All movement commands should require standing position
        foreach (var command in commands)
        {
            command.MinimumPosition.Should().Be(PlayerPosition.Standing);
            command.MinimumLevel.Should().Be(1);
            command.IsEnabled.Should().BeTrue();
        }
    }
}