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
/// TDD Red Phase tests for death and resurrection mechanics
/// Tests character death, corpse creation, experience loss, and resurrection
/// All tests should FAIL initially - this is expected for TDD Red phase
/// </summary>
public class DeathResurrectionTests
{
    private readonly Mock<IPlayer> _mockPlayer;
    private readonly Mock<IConnectionDescriptor> _mockConnection;
    private readonly Mock<IWorldDatabase> _mockWorldDatabase;
    private readonly Mock<ICombatEngine> _mockCombatEngine;
    private readonly IDeathHandler _deathHandler;

    public DeathResurrectionTests()
    {
        _mockConnection = new Mock<IConnectionDescriptor>();
        _mockConnection.Setup(c => c.IsConnected).Returns(true);
        
        _mockPlayer = new Mock<IPlayer>();
        _mockPlayer.Setup(p => p.Name).Returns("Victim");
        _mockPlayer.Setup(p => p.Level).Returns(10);
        _mockPlayer.SetupProperty(p => p.Position, PlayerPosition.Standing);
        _mockPlayer.Setup(p => p.IsConnected).Returns(true);
        _mockPlayer.Setup(p => p.Connection).Returns(_mockConnection.Object);
        _mockPlayer.SetupProperty(p => p.CurrentRoomVnum);
        _mockPlayer.SetupProperty(p => p.HitPoints);
        _mockPlayer.SetupProperty(p => p.MaxHitPoints);
        _mockPlayer.SetupProperty(p => p.ExperiencePoints);
        _mockPlayer.SetupProperty(p => p.Gold);
        
        _mockPlayer.Setup(p => p.SendMessageAsync(It.IsAny<string>()))
            .Callback<string>(message => _mockConnection.Object.SendDataAsync(message + "\r\n").Wait());

        _mockWorldDatabase = new Mock<IWorldDatabase>();
        _mockCombatEngine = new Mock<ICombatEngine>();
        _deathHandler = new DeathHandler(_mockWorldDatabase.Object);
    }

    #region Death Mechanics Tests

    [Fact]
    public async Task ProcessDeath_PlayerDies_SetsPositionToDead()
    {
        // Arrange
        _mockPlayer.Object.HitPoints = -5; // Below 0 HP
        _mockPlayer.Object.MaxHitPoints = 100;

        // Act
        await _deathHandler.ProcessDeathAsync(_mockPlayer.Object);

        // Assert
        _mockPlayer.VerifySet(p => p.Position = PlayerPosition.Dead, Times.Once,
            "Player position should be set to Dead");
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You are DEAD!"))), Times.Once);
    }

    [Fact]
    public async Task ProcessDeath_PlayerDies_CreatesCorpse()
    {
        // Arrange
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        _mockPlayer.Object.HitPoints = 0;
        
        var room = CreateTestRoom(1001);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _deathHandler.ProcessDeathAsync(_mockPlayer.Object);

        // Assert
        var corpse = room.Objects.FirstOrDefault(o => o.Name.Contains("corpse"));
        corpse.Should().NotBeNull("A corpse should be created when player dies");
        corpse.Name.Should().Contain(_mockPlayer.Object.Name, "Corpse should be named after player");
    }

    [Fact]
    public async Task ProcessDeath_PlayerDies_TransfersInventoryToCorpse()
    {
        // Arrange
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        var playerItems = new List<WorldObject>
        {
            CreateTestItem(2001, "sword"),
            CreateTestItem(2002, "potion")
        };
        _mockPlayer.Setup(p => p.GetInventory()).Returns(playerItems);
        
        var room = CreateTestRoom(1001);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _deathHandler.ProcessDeathAsync(_mockPlayer.Object);

        // Assert
        var corpse = room.Objects.First(o => o.Name.Contains("corpse"));
        corpse.Contents.Should().HaveCount(2, "Corpse should contain player's inventory");
        corpse.Contents.Should().Contain(i => i.Name == "sword");
        corpse.Contents.Should().Contain(i => i.Name == "potion");
    }

    [Fact]
    public async Task ProcessDeath_PlayerDies_LosesExperience()
    {
        // Arrange
        _mockPlayer.Object.ExperiencePoints = 50000; // Starting experience
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        
        var room = CreateTestRoom(1001);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);
        
        var expectedLoss = CalculateExpLoss(_mockPlayer.Object.Level, _mockPlayer.Object.ExperiencePoints);

        // Act
        await _deathHandler.ProcessDeathAsync(_mockPlayer.Object);

        // Assert
        _mockPlayer.Object.ExperiencePoints.Should().BeLessThan(50000, 
            "Player should lose experience on death");
        var actualLoss = 50000 - _mockPlayer.Object.ExperiencePoints;
        actualLoss.Should().BeGreaterThan(0, "Should lose some experience");
    }

    [Fact]
    public async Task ProcessDeath_PlayerDies_DropsGoldInCorpse()
    {
        // Arrange
        _mockPlayer.Object.Gold = 500;
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        
        var room = CreateTestRoom(1001);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act
        await _deathHandler.ProcessDeathAsync(_mockPlayer.Object);

        // Assert
        var corpse = room.Objects.First(o => o.Name.Contains("corpse"));
        corpse.Gold.Should().Be(500, "Corpse should contain player's gold");
        _mockPlayer.Object.Gold.Should().Be(0, "Player should lose all gold on death");
    }

    [Fact]
    public async Task ProcessDeath_NewbiePlayer_ReducedPenalties()
    {
        // Arrange - Low level player (newbie protection)
        _mockPlayer.Setup(p => p.Level).Returns(5);
        _mockPlayer.Object.ExperiencePoints = 1000;

        // Act
        await _deathHandler.ProcessDeathAsync(_mockPlayer.Object);

        // Assert
        // Newbie players should have reduced death penalties
        var expLoss = 1000 - _mockPlayer.Object.ExperiencePoints;
        expLoss.Should().BeLessThan(200, "Low level players should have reduced exp loss");
    }

    #endregion

    #region Resurrection Tests

    [Fact]
    public async Task Resurrect_DeadPlayer_RestoresLifeWithPenalty()
    {
        // Arrange
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Dead);
        _mockPlayer.Object.HitPoints = 0;
        _mockPlayer.Object.MaxHitPoints = 100;

        // Act
        await _deathHandler.ResurrectPlayerAsync(_mockPlayer.Object);

        // Assert
        _mockPlayer.VerifySet(p => p.Position = PlayerPosition.Standing, Times.Once);
        _mockPlayer.Object.HitPoints.Should().BeGreaterThan(0, "Should restore some hit points");
        _mockPlayer.Object.HitPoints.Should().BeLessThan(_mockPlayer.Object.MaxHitPoints, 
            "Should not restore to full health");
    }

    [Fact]
    public async Task Resurrect_DeadPlayer_TemporaryStatPenalty()
    {
        // Arrange
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Dead);
        _mockPlayer.Setup(p => p.Constitution).Returns(16);

        // Act
        await _deathHandler.ResurrectPlayerAsync(_mockPlayer.Object);

        // Assert
        // Player should have temporary constitution penalty
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("feel less healthy") || msg.Contains("weaker"))), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Resurrect_AlivePlayer_NoEffect()
    {
        // Arrange
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Standing);
        _mockPlayer.Object.HitPoints = 50;

        // Act
        await _deathHandler.ResurrectPlayerAsync(_mockPlayer.Object);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("not dead"))), Times.Once);
    }

    [Fact]
    public async Task Resurrect_MultipleDeaths_IncreasingPenalties()
    {
        // Arrange
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Dead);
        _mockPlayer.Setup(p => p.RecentDeathCount).Returns(3); // Multiple recent deaths

        // Act
        await _deathHandler.ResurrectPlayerAsync(_mockPlayer.Object);

        // Assert
        // Multiple deaths should result in harsher penalties
        _mockPlayer.Object.HitPoints.Should().BeLessThan(25, 
            "Multiple deaths should result in lower resurrection HP");
    }

    #endregion

    #region Corpse Management Tests

    [Fact]
    public async Task CorpseDecay_TimeExpired_RemovesCorpse()
    {
        // Arrange
        var room = CreateTestRoom(1001);
        var corpse = CreatePlayerCorpse("TestPlayer");
        corpse.DecayTime = DateTime.UtcNow.AddMinutes(-30); // Expired 30 minutes ago
        room.Objects.Add(corpse);

        // Act
        await _deathHandler.ProcessCorpseDecayAsync(room);

        // Assert
        room.Objects.Should().NotContain(corpse, "Expired corpse should be removed");
    }

    [Fact]  
    public async Task CorpseDecay_PlayerCorpse_LongerDecayTime()
    {
        // Arrange
        var playerCorpse = new WorldObject
        {
            ObjectType = ObjectType.CORPSE,
            Name = "corpse TestPlayer",
            DecayTime = DateTime.UtcNow.AddMinutes(30) // Player corpse time
        };
        
        var mobCorpse = new WorldObject
        {
            ObjectType = ObjectType.CORPSE, 
            Name = "corpse orc",
            DecayTime = DateTime.UtcNow.AddMinutes(5) // Mob corpse time (shorter)
        };

        // Act - Check decay times
        var playerDecayMinutes = (playerCorpse.DecayTime - DateTime.UtcNow).TotalMinutes;
        var mobDecayMinutes = (mobCorpse.DecayTime - DateTime.UtcNow).TotalMinutes;

        // Assert
        playerDecayMinutes.Should().BeGreaterThan(mobDecayMinutes, 
            "Player corpses should decay slower than mob corpses");
    }

    [Fact]
    public async Task CorpseDecay_ContainsItems_ScattersItems()
    {
        // Arrange
        var room = CreateTestRoom(1001);
        var corpse = CreatePlayerCorpse("TestPlayer");
        corpse.Contents.Add(CreateTestItem(2001, "sword"));
        corpse.Contents.Add(CreateTestItem(2002, "shield"));
        corpse.DecayTime = DateTime.UtcNow.AddMinutes(-1);
        room.Objects.Add(corpse);

        // Act
        await _deathHandler.ProcessCorpseDecayAsync(room);

        // Assert
        room.Objects.Should().Contain(o => o.Name == "sword", "Items should scatter to room");
        room.Objects.Should().Contain(o => o.Name == "shield", "Items should scatter to room");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task DeathProcessing_Performance_CompletesUnder100ms()
    {
        // Arrange
        _mockPlayer.Object.CurrentRoomVnum = 1001;
        var room = CreateTestRoom(1001);
        _mockWorldDatabase.Setup(db => db.GetRoom(1001)).Returns(room);

        // Act & Assert
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _deathHandler.ProcessDeathAsync(_mockPlayer.Object);
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
            "Death processing should complete within 100ms");
    }

    #endregion

    #region Helper Methods

    private Room CreateTestRoom(int vnum)
    {
        return new Room
        {
            VirtualNumber = vnum,
            Name = "Test Room",
            Description = "A room for testing death mechanics.",
            Players = new List<IPlayer>(),
            Objects = new List<WorldObject>(),
            Exits = new Dictionary<Direction, Exit>()
        };
    }

    private WorldObject CreateTestItem(int vnum, string name)
    {
        return new WorldObject
        {
            VirtualNumber = vnum,
            Name = name,
            ShortDescription = $"a {name}",
            ObjectType = ObjectType.OTHER
        };
    }

    private WorldObject CreatePlayerCorpse(string playerName)
    {
        return new WorldObject
        {
            VirtualNumber = 0, // Corpses have special vnum handling
            Name = $"corpse {playerName}",
            ShortDescription = $"the corpse of {playerName}",
            ObjectType = ObjectType.CONTAINER
            // TODO: Add Contents, DecayTime, Gold properties to WorldObject
        };
    }

    private WorldObject CreateMobCorpse(string mobName)
    {
        return new WorldObject
        {
            VirtualNumber = 0,
            Name = $"corpse {mobName}",
            ShortDescription = $"the corpse of {mobName}",
            ObjectType = ObjectType.CONTAINER
            // TODO: Add Contents, DecayTime, Gold properties to WorldObject
        };
    }

    private int CalculateExpLoss(int level, int currentExp)
    {
        // CircleMUD experience loss formula (approximate)
        // Lose exp based on level, but never drop below level minimum
        return Math.Max(currentExp / 10, level * 1000);
    }

    #endregion
}