using C3Mud.Core.Commands.Basic;
using C3Mud.Core.Networking;
using C3Mud.Core.Players;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace C3Mud.Tests.Commands;

/// <summary>
/// TDD Red Phase tests for Iteration 4: Room Interaction System
/// Tests for enhanced look command, examining exits, room details, etc.
/// All tests should FAIL initially - this is expected for TDD Red phase
/// </summary>
public class RoomInteractionTests
{
    private readonly Mock<IPlayer> _mockPlayer;
    private readonly Mock<IConnectionDescriptor> _mockConnection;
    private readonly Mock<IWorldDatabase> _mockWorldDatabase;
    private readonly LookCommand _lookCommand;

    public RoomInteractionTests()
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
        _lookCommand = new LookCommand(_mockWorldDatabase.Object);
    }

    #region Looking at Specific Exits

    [Fact]
    public async Task LookCommand_LookNorth_ShowsNorthExitDetails()
    {
        // Arrange
        var room = CreateRoomWithDetailedExits();
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _lookCommand.ExecuteAsync(_mockPlayer.Object, "north", 0);

        // Assert - Should show detailed exit description
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("A sturdy wooden door"))), Times.Once);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You can see faint light"))), Times.Once);
    }

    [Fact]
    public async Task LookCommand_LookAtClosedDoor_ShowsDoorState()
    {
        // Arrange
        var room = CreateRoomWithClosedDoor();
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _lookCommand.ExecuteAsync(_mockPlayer.Object, "door", 0);

        // Assert - Should show door is closed
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("The door is closed"))), Times.Once);
    }

    [Fact]
    public async Task LookCommand_LookAtOpenDoor_ShowsThroughDoor()
    {
        // Arrange
        var currentRoom = CreateRoomWithOpenDoor();
        var targetRoom = CreateTargetRoom();
        
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(currentRoom);
        _mockWorldDatabase.Setup(db => db.GetRoom(1002)).Returns(targetRoom);

        // Act
        await _lookCommand.ExecuteAsync(_mockPlayer.Object, "door", 0);

        // Assert - Should show what's beyond the open door
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("Through the door you can see"))), Times.Once);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("Target Room"))), Times.Once);
    }

    [Fact]
    public async Task LookCommand_LookInvalidDirection_ShowsNotFound()
    {
        // Arrange
        var room = CreateBasicRoom();
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _lookCommand.ExecuteAsync(_mockPlayer.Object, "southwest", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You don't see anything special in that direction"))), Times.Once);
    }

    #endregion

    #region Room Environmental Details

    [Fact]
    public async Task LookCommand_RoomWithLighting_ShowsLightingEffects()
    {
        // Arrange
        var room = CreateDarkRoom();
        _mockPlayer.Setup(p => p.HasLight).Returns(false); // No light source
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _lookCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert - Should show darkness message
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("It is pitch black"))), Times.Once);
    }

    [Fact]
    public async Task LookCommand_RoomWithLight_ShowsNormalDescription()
    {
        // Arrange
        var room = CreateDarkRoom();
        _mockPlayer.Setup(p => p.HasLight).Returns(true); // Player has light
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _lookCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert - Should show normal description with light
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("Dark Room"))), Times.Once);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("This room is very dark"))), Times.Once);
    }

    [Fact]
    public async Task LookCommand_WeatherEffects_ShowsWeatherInDescription()
    {
        // Arrange
        var room = CreateOutdoorRoom();
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);
        
        // Mock weather system showing rain
        _mockWorldDatabase.Setup(db => db.GetCurrentWeather()).Returns("It is raining heavily.");

        // Act
        await _lookCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert - Should include weather in room description
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("It is raining heavily"))), Times.Once);
    }

    #endregion

    #region Enhanced Room Search

    [Fact]
    public async Task SearchCommand_SearchRoom_FindsHiddenExit()
    {
        // Arrange
        var room = CreateRoomWithHiddenExit();
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act - Player searches the room
        await ExecuteSearchCommand();

        // Assert - Should find hidden exit
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You discover a hidden passage to the east"))), Times.Once);
    }

    [Fact]
    public async Task SearchCommand_SearchRoom_FindsHiddenObject()
    {
        // Arrange  
        var room = CreateRoomWithHiddenObject();
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await ExecuteSearchCommand();

        // Assert - Should find hidden object
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You find a hidden key"))), Times.Once);
    }

    [Fact]
    public async Task LookCommand_ExamineWalls_ShowsWallDetails()
    {
        // Arrange
        var room = CreateRoomWithExaminableFeatures();
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _lookCommand.ExecuteAsync(_mockPlayer.Object, "walls", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("ancient stone walls"))), Times.Once);
    }

    #endregion

    #region Helper Methods

    private Room CreateRoomWithDetailedExits()
    {
        return new Room
        {
            VirtualNumber = 1001,
            Name = "Room with Detailed Exits",
            Description = "A room with well-described exits.",
            Exits = new Dictionary<Direction, Exit>
            {
                {
                    Direction.North, new Exit
                    {
                        Direction = Direction.North,
                        TargetRoomVnum = 1002,
                        Name = "door",
                        Description = "A sturdy wooden door. You can see faint light coming from underneath it."
                    }
                }
            }
        };
    }

    private Room CreateRoomWithClosedDoor()
    {
        return new Room
        {
            VirtualNumber = 1001,
            Name = "Room with Closed Door",
            Description = "A room with a closed door.",
            Exits = new Dictionary<Direction, Exit>
            {
                {
                    Direction.North, new Exit
                    {
                        Direction = Direction.North,
                        TargetRoomVnum = 1002,
                        Name = "door",
                        Description = "A heavy wooden door.",
                        DoorFlags = 1 // Closed
                    }
                }
            }
        };
    }

    private Room CreateRoomWithOpenDoor()
    {
        return new Room
        {
            VirtualNumber = 1001,
            Name = "Room with Open Door",
            Description = "A room with an open door.",
            Exits = new Dictionary<Direction, Exit>
            {
                {
                    Direction.North, new Exit
                    {
                        Direction = Direction.North,
                        TargetRoomVnum = 1002,
                        Name = "door",
                        Description = "An open doorway.",
                        DoorFlags = 0 // Open
                    }
                }
            }
        };
    }

    private Room CreateTargetRoom()
    {
        return new Room
        {
            VirtualNumber = 1002,
            Name = "Target Room",
            Description = "This is the target room beyond the door."
        };
    }

    private Room CreateBasicRoom()
    {
        return new Room
        {
            VirtualNumber = 1001,
            Name = "Basic Room",
            Description = "A simple room with no special features.",
            Exits = new Dictionary<Direction, Exit>
            {
                {
                    Direction.North, new Exit
                    {
                        Direction = Direction.North,
                        TargetRoomVnum = 1002
                    }
                }
            }
        };
    }

    private Room CreateDarkRoom()
    {
        return new Room
        {
            VirtualNumber = 1001,
            Name = "Dark Room",
            Description = "This room is very dark without a light source.",
            RoomFlags = RoomFlags.Dark // Room is dark
        };
    }

    private Room CreateOutdoorRoom()
    {
        return new Room
        {
            VirtualNumber = 1001,
            Name = "Outdoor Room",
            Description = "An outdoor area affected by weather.",
            SectorType = SectorType.Field // Outdoor sector
        };
    }

    private Room CreateRoomWithHiddenExit()
    {
        return new Room
        {
            VirtualNumber = 1001,
            Name = "Room with Secrets",
            Description = "This room might have hidden passages.",
            Exits = new Dictionary<Direction, Exit>
            {
                {
                    Direction.East, new Exit
                    {
                        Direction = Direction.East,
                        TargetRoomVnum = 1003,
                        ExitFlags = ExitFlags.Hidden // Hidden exit
                    }
                }
            }
        };
    }

    private Room CreateRoomWithHiddenObject()
    {
        return new Room
        {
            VirtualNumber = 1001,
            Name = "Room with Hidden Object",
            Description = "This room has something hidden.",
            HiddenObjects = new List<WorldObject> 
            { 
                new WorldObject { VirtualNumber = 2001, ShortDescription = "a hidden key" } 
            }
        };
    }

    private Room CreateRoomWithExaminableFeatures()
    {
        return new Room
        {
            VirtualNumber = 1001,
            Name = "Ancient Chamber",
            Description = "An ancient stone chamber.",
            ExaminableFeatures = new Dictionary<string, string>
            {
                { "walls", "The ancient stone walls are covered in mysterious runes." },
                { "floor", "The floor is worn smooth by countless footsteps." },
                { "ceiling", "The ceiling disappears into darkness above." }
            }
        };
    }

    private async Task ExecuteSearchCommand()
    {
        // This would be implemented as a SearchCommand class
        // For now, simulate the search behavior in tests
        await Task.CompletedTask;
    }

    private async Task ExecuteExamineCommand(string target)
    {
        // This would be implemented as an ExamineCommand class  
        // For now, simulate the examine behavior in tests
        await Task.CompletedTask;
    }

    #endregion
}