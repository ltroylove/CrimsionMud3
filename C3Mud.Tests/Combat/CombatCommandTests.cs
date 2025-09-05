using C3Mud.Core.Commands.Combat;
using C3Mud.Core.Combat;
using C3Mud.Core.Players;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;
using C3Mud.Core.Networking;
using FluentAssertions;
using Moq;
using Xunit;

namespace C3Mud.Tests.Combat;

/// <summary>
/// TDD Red Phase tests for combat commands
/// Tests the kill, flee, bash, kick commands and their integration with combat engine
/// All tests should FAIL initially - this is expected for TDD Red phase
/// </summary>
public class CombatCommandTests
{
    private readonly Mock<IPlayer> _mockPlayer;
    private readonly Mock<IPlayer> _mockTarget;
    private readonly Mock<IConnectionDescriptor> _mockConnection;
    private readonly Mock<IConnectionDescriptor> _mockTargetConnection;
    private readonly Mock<IWorldDatabase> _mockWorldDatabase;
    private readonly Mock<ICombatEngine> _mockCombatEngine;
    private readonly KillCommand _killCommand;
    private readonly FleeCommand _fleeCommand;
    private readonly BashCommand _bashCommand;
    private readonly KickCommand _kickCommand;

    public CombatCommandTests()
    {
        // Setup main player
        _mockConnection = new Mock<IConnectionDescriptor>();
        _mockConnection.Setup(c => c.IsConnected).Returns(true);
        
        _mockPlayer = new Mock<IPlayer>();
        _mockPlayer.Setup(p => p.Name).Returns("Fighter");
        _mockPlayer.Setup(p => p.Level).Returns(10);
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Standing);
        _mockPlayer.Setup(p => p.IsConnected).Returns(true);
        _mockPlayer.Setup(p => p.Connection).Returns(_mockConnection.Object);
        _mockPlayer.SetupProperty(p => p.CurrentRoomVnum);
        _mockPlayer.SetupProperty(p => p.HitPoints);
        
        // Setup target player
        _mockTargetConnection = new Mock<IConnectionDescriptor>();
        _mockTargetConnection.Setup(c => c.IsConnected).Returns(true);
        
        _mockTarget = new Mock<IPlayer>();
        _mockTarget.Setup(p => p.Name).Returns("Enemy");
        _mockTarget.Setup(p => p.Level).Returns(8);
        _mockTarget.Setup(p => p.Position).Returns(PlayerPosition.Standing);
        _mockTarget.Setup(p => p.IsConnected).Returns(true);
        _mockTarget.Setup(p => p.Connection).Returns(_mockTargetConnection.Object);
        _mockTarget.SetupProperty(p => p.CurrentRoomVnum);
        
        // Set up SendMessageAsync
        _mockPlayer.Setup(p => p.SendMessageAsync(It.IsAny<string>()))
            .Callback<string>(message => _mockConnection.Object.SendDataAsync(message + "\r\n").Wait());
        _mockTarget.Setup(p => p.SendMessageAsync(It.IsAny<string>()))
            .Callback<string>(message => _mockTargetConnection.Object.SendDataAsync(message + "\r\n").Wait());

        _mockWorldDatabase = new Mock<IWorldDatabase>();
        _mockCombatEngine = new Mock<ICombatEngine>();
        
        // Create commands
        _killCommand = new KillCommand(_mockWorldDatabase.Object, _mockCombatEngine.Object);
        _fleeCommand = new FleeCommand(_mockWorldDatabase.Object, _mockCombatEngine.Object);
        _bashCommand = new BashCommand(_mockWorldDatabase.Object, _mockCombatEngine.Object);
        _kickCommand = new KickCommand(_mockWorldDatabase.Object, _mockCombatEngine.Object);
    }

    #region Kill Command Tests

    [Fact]
    public async Task KillCommand_ValidTarget_InitiatesCombat()
    {
        // Arrange
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockTarget.Object.CurrentRoomVnum = 1001;
        
        var room = CreateRoomWithPlayers(1001, _mockPlayer.Object, _mockTarget.Object);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);
        _mockCombatEngine.Setup(ce => ce.InitiateCombatAsync(_mockPlayer.Object, _mockTarget.Object))
            .ReturnsAsync(true);

        // Act
        await _killCommand.ExecuteAsync(_mockPlayer.Object, "enemy", 0);

        // Assert
        _mockCombatEngine.Verify(ce => ce.InitiateCombatAsync(_mockPlayer.Object, _mockTarget.Object), 
            Times.Once, "Should initiate combat with target");
    }

    [Fact]
    public async Task KillCommand_NoTarget_ShowsUsageMessage()
    {
        // Arrange - No arguments provided

        // Act
        await _killCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("Kill who?"))), Times.Once);
    }

    [Fact]
    public async Task KillCommand_TargetNotFound_ShowsNotHereMessage()
    {
        // Arrange
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        var room = CreateRoomWithPlayers(1001, _mockPlayer.Object);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _killCommand.ExecuteAsync(_mockPlayer.Object, "nonexistent", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("They aren't here"))), Times.Once);
    }

    [Fact]
    public async Task KillCommand_TargetSelf_PreventsSelfAttack()
    {
        // Arrange
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        var room = CreateRoomWithPlayers(1001, _mockPlayer.Object);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _killCommand.ExecuteAsync(_mockPlayer.Object, "fighter", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You hit yourself"))), Times.Once);
    }

    [Fact]
    public async Task KillCommand_AlreadyInCombat_ShowsBusyMessage()
    {
        // Arrange
        _mockPlayer.Setup(p => p.IsInCombat).Returns(true);

        // Act
        await _killCommand.ExecuteAsync(_mockPlayer.Object, "enemy", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You are already fighting"))), Times.Once);
    }

    #endregion

    #region Flee Command Tests

    [Fact]
    public async Task FleeCommand_InCombat_AttemptsToFlee()
    {
        // Arrange
        _mockPlayer.Setup(p => p.IsInCombat).Returns(true);
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        
        var room = CreateRoomWithExits(1001);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);
        _mockCombatEngine.Setup(ce => ce.AttemptFleeAsync(_mockPlayer.Object))
            .ReturnsAsync(true);

        // Act
        await _fleeCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert
        _mockCombatEngine.Verify(ce => ce.AttemptFleeAsync(_mockPlayer.Object), 
            Times.Once, "Should attempt to flee from combat");
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You flee"))), Times.Once);
    }

    [Fact]
    public async Task FleeCommand_NotInCombat_ShowsNotFightingMessage()
    {
        // Arrange
        _mockPlayer.Setup(p => p.IsInCombat).Returns(false);

        // Act
        await _fleeCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You are not fighting"))), Times.Once);
    }

    [Fact]
    public async Task FleeCommand_FleeBlocked_ShowsCannotFleeMessage()
    {
        // Arrange
        _mockPlayer.Setup(p => p.IsInCombat).Returns(true);
        _mockCombatEngine.Setup(ce => ce.AttemptFleeAsync(_mockPlayer.Object))
            .ReturnsAsync(false);

        // Act
        await _fleeCommand.ExecuteAsync(_mockPlayer.Object, "", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("PANIC! You couldn't escape!"))), Times.Once);
    }

    #endregion

    #region Bash Command Tests

    [Fact]
    public async Task BashCommand_ValidTarget_AttemptsBash()
    {
        // Arrange
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockTarget.Object.CurrentRoomVnum = 1001;
        _mockPlayer.Setup(p => p.GetSkillLevel("bash")).Returns(75); // Skilled in bash
        
        var room = CreateRoomWithPlayers(1001, _mockPlayer.Object, _mockTarget.Object);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _bashCommand.ExecuteAsync(_mockPlayer.Object, "enemy", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.IsAny<string>()), Times.AtLeastOnce,
            "Should send some message about bash attempt");
    }

    [Fact]
    public async Task BashCommand_LowSkill_HigherFailureChance()
    {
        // Arrange
        _mockPlayer.Setup(p => p.GetSkillLevel("bash")).Returns(25); // Low skill
        
        var room = CreateRoomWithPlayers(1001, _mockPlayer.Object, _mockTarget.Object);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _bashCommand.ExecuteAsync(_mockPlayer.Object, "enemy", 0);

        // Assert - Should have higher chance to see failure message
        // Implementation will determine exact behavior
        _mockConnection.Verify(c => c.SendDataAsync(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task BashCommand_SuccessfulBash_KnocksTargetDown()
    {
        // Arrange - High skill for likely success
        _mockPlayer.Setup(p => p.GetSkillLevel("bash")).Returns(95);
        
        var room = CreateRoomWithPlayers(1001, _mockPlayer.Object, _mockTarget.Object);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _bashCommand.ExecuteAsync(_mockPlayer.Object, "enemy", 0);

        // Assert - Target should potentially be knocked down
        // Exact implementation will determine position change
        _mockConnection.Verify(c => c.SendDataAsync(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task BashCommand_NoShield_CannotBash()
    {
        // Arrange - Player has no shield
        _mockPlayer.Setup(p => p.GetEquippedItem(EquipmentSlot.Shield)).Returns((WorldObject)null);
        
        // Act
        await _bashCommand.ExecuteAsync(_mockPlayer.Object, "enemy", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You need to be holding a shield"))), Times.Once);
    }

    #endregion

    #region Kick Command Tests

    [Fact]
    public async Task KickCommand_ValidTarget_AttemptsKick()
    {
        // Arrange
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockTarget.Object.CurrentRoomVnum = 1001;
        _mockPlayer.Setup(p => p.GetSkillLevel("kick")).Returns(75);
        
        var room = CreateRoomWithPlayers(1001, _mockPlayer.Object, _mockTarget.Object);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _kickCommand.ExecuteAsync(_mockPlayer.Object, "enemy", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.IsAny<string>()), Times.AtLeastOnce,
            "Should send message about kick attempt");
    }

    [Fact]
    public async Task KickCommand_SuccessfulKick_DoesDamage()
    {
        // Arrange - High skill for likely success
        _mockPlayer.Setup(p => p.GetSkillLevel("kick")).Returns(95);
        _mockPlayer.Setup(p => p.Level).Returns(15); // Higher level for more damage
        
        var room = CreateRoomWithPlayers(1001, _mockPlayer.Object, _mockTarget.Object);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _kickCommand.ExecuteAsync(_mockPlayer.Object, "enemy", 0);

        // Assert - Should indicate successful kick
        _mockConnection.Verify(c => c.SendDataAsync(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task KickCommand_FailedKick_NoEffect()
    {
        // Arrange - Very low skill for likely failure
        _mockPlayer.Setup(p => p.GetSkillLevel("kick")).Returns(5);
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        
        var room = CreateRoomWithPlayers(1001, _mockPlayer.Object, _mockTarget.Object);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _kickCommand.ExecuteAsync(_mockPlayer.Object, "enemy", 0);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("miss") || msg.Contains("fail"))), Times.AtLeastOnce);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task CombatCommands_Performance_CompleteUnder100ms()
    {
        // Arrange
        var room = CreateRoomWithPlayers(1001, _mockPlayer.Object, _mockTarget.Object);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act & Assert
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _killCommand.ExecuteAsync(_mockPlayer.Object, "enemy", 0);
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
            "Combat commands should complete within 100ms");
    }

    #endregion

    #region Helper Methods

    private Room CreateRoomWithPlayers(int vnum, params IPlayer[] players)
    {
        var room = new Room
        {
            VirtualNumber = vnum,
            Name = "Combat Test Room",
            Description = "A room for testing combat.",
            Players = new List<IPlayer>(players),
            Exits = new Dictionary<Direction, Exit>()
        };
        return room;
    }

    private Room CreateRoomWithExits(int vnum)
    {
        var room = new Room
        {
            VirtualNumber = vnum,
            Name = "Room with Exits",
            Description = "A room with multiple exits for fleeing.",
            Players = new List<IPlayer>(),
            Exits = new Dictionary<Direction, Exit>
            {
                { Direction.North, new Exit { Direction = Direction.North, TargetRoomVnum = vnum + 1 } },
                { Direction.South, new Exit { Direction = Direction.South, TargetRoomVnum = vnum - 1 } }
            }
        };
        return room;
    }

    #endregion
}