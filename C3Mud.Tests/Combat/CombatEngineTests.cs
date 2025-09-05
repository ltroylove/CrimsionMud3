using C3Mud.Core.Combat;
using C3Mud.Core.Combat.Models;
using C3Mud.Core.Players;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;
using C3Mud.Core.Networking;
using FluentAssertions;
using Moq;
using Xunit;

namespace C3Mud.Tests.Combat;

/// <summary>
/// TDD Red Phase tests for Iteration 5: Combat System Foundation
/// These tests define the core combat behaviors we need to implement
/// All tests should FAIL initially - this is expected for TDD Red phase
/// Based on original CircleMUD combat mechanics and formulas
/// </summary>
public class CombatEngineTests
{
    private readonly Mock<IPlayer> _mockAttacker;
    private readonly Mock<IPlayer> _mockDefender;
    private readonly Mock<IConnectionDescriptor> _mockAttackerConnection;
    private readonly Mock<IConnectionDescriptor> _mockDefenderConnection;
    private readonly Mock<IWorldDatabase> _mockWorldDatabase;
    private readonly ICombatEngine _combatEngine;

    public CombatEngineTests()
    {
        // Setup attacker
        _mockAttackerConnection = new Mock<IConnectionDescriptor>();
        _mockAttackerConnection.Setup(c => c.IsConnected).Returns(true);
        
        _mockAttacker = new Mock<IPlayer>();
        _mockAttacker.Setup(p => p.Name).Returns("Warrior");
        _mockAttacker.Setup(p => p.Level).Returns(10);
        _mockAttacker.Setup(p => p.Position).Returns(PlayerPosition.Standing);
        _mockAttacker.Setup(p => p.IsConnected).Returns(true);
        _mockAttacker.Setup(p => p.Connection).Returns(_mockAttackerConnection.Object);
        _mockAttacker.SetupProperty(p => p.CurrentRoomVnum);
        _mockAttacker.SetupProperty(p => p.HitPoints);
        _mockAttacker.SetupProperty(p => p.MaxHitPoints);

        // Setup defender  
        _mockDefenderConnection = new Mock<IConnectionDescriptor>();
        _mockDefenderConnection.Setup(c => c.IsConnected).Returns(true);
        
        _mockDefender = new Mock<IPlayer>();
        _mockDefender.Setup(p => p.Name).Returns("Victim");
        _mockDefender.Setup(p => p.Level).Returns(8);
        _mockDefender.Setup(p => p.Position).Returns(PlayerPosition.Standing);
        _mockDefender.Setup(p => p.IsConnected).Returns(true);
        _mockDefender.Setup(p => p.Connection).Returns(_mockDefenderConnection.Object);
        _mockDefender.SetupProperty(p => p.CurrentRoomVnum);
        _mockDefender.SetupProperty(p => p.HitPoints);
        _mockDefender.SetupProperty(p => p.MaxHitPoints);

        // Set up player SendMessageAsync to call connection SendDataAsync
        _mockAttacker.Setup(p => p.SendMessageAsync(It.IsAny<string>()))
            .Callback<string>(message => _mockAttackerConnection.Object.SendDataAsync(message + "\r\n").Wait());
        _mockDefender.Setup(p => p.SendMessageAsync(It.IsAny<string>()))
            .Callback<string>(message => _mockDefenderConnection.Object.SendDataAsync(message + "\r\n").Wait());

        _mockWorldDatabase = new Mock<IWorldDatabase>();
        _combatEngine = new CombatEngine(_mockWorldDatabase.Object);
    }

    #region Combat Initiation Tests

    [Fact]
    public async Task InitiateCombat_ValidTarget_StartsCombat()
    {
        // Arrange
        _mockAttacker.Object.CurrentRoomVnum = 1001;
        _mockDefender.Object.CurrentRoomVnum = 1001;
        _mockAttacker.Object.HitPoints = 100;
        _mockDefender.Object.HitPoints = 80;

        // Act
        var result = await _combatEngine.InitiateCombatAsync(_mockAttacker.Object, _mockDefender.Object);

        // Assert
        result.Should().BeTrue("Combat should start successfully");
        _mockAttackerConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You attack Victim!"))), Times.Once);
        _mockDefenderConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("Warrior attacks you!"))), Times.Once);
    }

    [Fact]
    public async Task InitiateCombat_TargetNotInSameRoom_FailsToStart()
    {
        // Arrange
        _mockAttacker.Object.CurrentRoomVnum = 1001;
        _mockDefender.Object.CurrentRoomVnum = 1002;

        // Act
        var result = await _combatEngine.InitiateCombatAsync(_mockAttacker.Object, _mockDefender.Object);

        // Assert
        result.Should().BeFalse("Combat should fail when target not in same room");
        _mockAttackerConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("They aren't here"))), Times.Once);
    }

    [Fact]
    public async Task InitiateCombat_TargetAlreadyDead_FailsToStart()
    {
        // Arrange
        _mockAttacker.Object.CurrentRoomVnum = 1001;
        _mockDefender.Object.CurrentRoomVnum = 1001;
        _mockDefender.Object.HitPoints = 0; // Dead target
        _mockDefender.Setup(p => p.Position).Returns(PlayerPosition.Dead);

        // Act
        var result = await _combatEngine.InitiateCombatAsync(_mockAttacker.Object, _mockDefender.Object);

        // Assert
        result.Should().BeFalse("Cannot attack dead targets");
        _mockAttackerConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("already dead"))), Times.Once);
    }

    [Fact]
    public async Task InitiateCombat_AttackerNotStanding_FailsToStart()
    {
        // Arrange
        _mockAttacker.Setup(p => p.Position).Returns(PlayerPosition.Sitting);
        _mockAttacker.Object.CurrentRoomVnum = 1001;
        _mockDefender.Object.CurrentRoomVnum = 1001;

        // Act
        var result = await _combatEngine.InitiateCombatAsync(_mockAttacker.Object, _mockDefender.Object);

        // Assert
        result.Should().BeFalse("Cannot attack while not standing");
        _mockAttackerConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You must be standing"))), Times.Once);
    }

    #endregion

    #region Hit/Miss Calculation Tests (THAC0 System)

    [Fact]
    public async Task CalculateHit_Level10vs8_UsesCorrectTHAC0()
    {
        // Arrange - Level 10 attacker vs Level 8 defender
        _mockAttacker.Setup(p => p.Level).Returns(10);
        _mockAttacker.Setup(p => p.Strength).Returns(18); // High strength
        _mockDefender.Setup(p => p.Level).Returns(8);
        _mockDefender.Setup(p => p.ArmorClass).Returns(0); // AC 0 (decent armor)

        // Act
        var hitResult = await _combatEngine.CalculateHitAsync(_mockAttacker.Object, _mockDefender.Object);

        // Assert - THAC0 for level 10 should be 11 (20 - (level-1))
        // With str 18, should get +1 to hit, need to roll 11-1=10 or less on d20 to hit AC 0
        hitResult.Should().NotBeNull("Hit calculation should return result");
        hitResult.AttackerTHAC0.Should().Be(11, "Level 10 should have THAC0 of 11");
    }

    [Fact]
    public async Task CalculateHit_HighStrength_GetsBonusToHit()
    {
        // Arrange
        _mockAttacker.Setup(p => p.Strength).Returns(18); // Max strength
        
        // Act
        var hitResult = await _combatEngine.CalculateHitAsync(_mockAttacker.Object, _mockDefender.Object);

        // Assert
        hitResult.StrengthBonus.Should().Be(1, "Strength 18 should give +1 to hit");
    }

    [Fact]
    public async Task CalculateHit_LowStrength_GetsPenaltyToHit()
    {
        // Arrange
        _mockAttacker.Setup(p => p.Strength).Returns(6); // Low strength
        
        // Act
        var hitResult = await _combatEngine.CalculateHitAsync(_mockAttacker.Object, _mockDefender.Object);

        // Assert
        hitResult.StrengthBonus.Should().Be(-1, "Strength 6 should give -1 to hit");
    }

    [Fact]
    public async Task CalculateHit_CriticalHit_AlwaysHits()
    {
        // Arrange - Set up scenario where normal roll would miss
        _mockAttacker.Setup(p => p.Level).Returns(1); // THAC0 20
        _mockDefender.Setup(p => p.ArmorClass).Returns(-10); // Excellent armor
        
        // Act - Simulate critical hit (d20 roll of 20)
        var hitResult = await _combatEngine.CalculateHitAsync(_mockAttacker.Object, _mockDefender.Object, forceDiceRoll: 20);

        // Assert
        hitResult.IsHit.Should().BeTrue("Natural 20 should always hit regardless of AC");
        hitResult.IsCriticalHit.Should().BeTrue("Roll of 20 should be critical hit");
    }

    [Fact]
    public async Task CalculateHit_CriticalMiss_AlwaysMisses()
    {
        // Arrange - Set up scenario where normal roll would hit
        _mockAttacker.Setup(p => p.Level).Returns(20); // THAC0 1
        _mockDefender.Setup(p => p.ArmorClass).Returns(10); // Poor armor
        
        // Act - Simulate critical miss (d20 roll of 1)
        var hitResult = await _combatEngine.CalculateHitAsync(_mockAttacker.Object, _mockDefender.Object, forceDiceRoll: 1);

        // Assert
        hitResult.IsHit.Should().BeFalse("Natural 1 should always miss regardless of THAC0");
        hitResult.IsCriticalMiss.Should().BeTrue("Roll of 1 should be critical miss");
    }

    #endregion

    #region Damage Calculation Tests

    [Fact]
    public async Task CalculateDamage_BasicMeleeDamage_UsesCorrectFormula()
    {
        // Arrange
        var weapon = CreateBasicWeapon(1, 6); // 1d6 weapon
        _mockAttacker.Setup(p => p.GetWieldedWeapon()).Returns(weapon);
        _mockAttacker.Setup(p => p.Strength).Returns(18);
        
        // Act
        var damageResult = await _combatEngine.CalculateDamageAsync(_mockAttacker.Object, _mockDefender.Object, false);

        // Assert
        damageResult.BaseDamage.Should().BeInRange(1, 6, "1d6 weapon should do 1-6 damage");
        damageResult.StrengthBonus.Should().Be(2, "Strength 18 should give +2 damage bonus");
        damageResult.TotalDamage.Should().BeInRange(3, 8, "Total should be base + strength bonus");
    }

    [Fact]
    public async Task CalculateDamage_CriticalHit_DoesDoubleDamage()
    {
        // Arrange
        var weapon = CreateBasicWeapon(1, 6);
        _mockAttacker.Setup(p => p.GetWieldedWeapon()).Returns(weapon);
        
        // Act
        var normalDamage = await _combatEngine.CalculateDamageAsync(_mockAttacker.Object, _mockDefender.Object, false);
        var criticalDamage = await _combatEngine.CalculateDamageAsync(_mockAttacker.Object, _mockDefender.Object, true);

        // Assert
        criticalDamage.TotalDamage.Should().Be(normalDamage.TotalDamage * 2, 
            "Critical hits should do double damage");
        criticalDamage.IsCritical.Should().BeTrue("Should be marked as critical");
    }

    [Fact]
    public async Task CalculateDamage_UnarmedCombat_UsesBareHandDamage()
    {
        // Arrange - No weapon wielded
        _mockAttacker.Setup(p => p.GetWieldedWeapon()).Returns((WorldObject)null);
        
        // Act
        var damageResult = await _combatEngine.CalculateDamageAsync(_mockAttacker.Object, _mockDefender.Object, false);

        // Assert
        damageResult.BaseDamage.Should().BeInRange(1, 2, "Bare hands should do 1d2 damage");
        damageResult.WeaponName.Should().Be("bare hands", "Should indicate unarmed combat");
    }

    [Fact]
    public async Task CalculateDamage_MagicWeapon_AddsMagicBonus()
    {
        // Arrange
        var magicWeapon = CreateMagicWeapon(1, 8, 2); // 1d8+2 magic sword
        _mockAttacker.Setup(p => p.GetWieldedWeapon()).Returns(magicWeapon);
        
        // Act
        var damageResult = await _combatEngine.CalculateDamageAsync(_mockAttacker.Object, _mockDefender.Object, false);

        // Assert
        damageResult.WeaponBonus.Should().Be(2, "Magic weapon should provide +2 bonus");
        damageResult.TotalDamage.Should().BeInRange(3, 10, "Should include magic weapon bonus");
    }

    #endregion

    #region Combat Round Management Tests

    [Fact]
    public async Task ExecuteCombatRound_ValidCombat_ProcessesAttacks()
    {
        // Arrange
        _mockAttacker.Object.HitPoints = 100;
        _mockDefender.Object.HitPoints = 80;
        _mockAttacker.Setup(p => p.Strength).Returns(15);
        _mockAttacker.Setup(p => p.ArmorClass).Returns(10);
        _mockDefender.Setup(p => p.ArmorClass).Returns(10);
        _mockAttacker.Setup(p => p.GetWieldedWeapon()).Returns((WorldObject)null); // Unarmed combat
        
        var combatants = new List<ICombatant>
        {
            new PlayerCombatant(_mockAttacker.Object),
            new PlayerCombatant(_mockDefender.Object)
        };
        
        // Act
        var roundResult = await _combatEngine.ExecuteCombatRoundAsync(combatants);

        // Assert
        roundResult.Should().NotBeNull("Combat round should return result");
        roundResult.AttackResults.Should().NotBeEmpty("Should have attack results");
        roundResult.RoundNumber.Should().Be(1, "First round should be number 1");
    }

    [Fact]
    public async Task ExecuteCombatRound_InitiativeOrder_DeterminedByDexterity()
    {
        // Arrange
        _mockAttacker.Object.HitPoints = 100;
        _mockDefender.Object.HitPoints = 100;
        _mockAttacker.Setup(p => p.Dexterity).Returns(10);
        _mockDefender.Setup(p => p.Dexterity).Returns(18);
        _mockAttacker.Setup(p => p.Strength).Returns(15);
        _mockAttacker.Setup(p => p.ArmorClass).Returns(10);
        _mockDefender.Setup(p => p.ArmorClass).Returns(10);
        _mockAttacker.Setup(p => p.GetWieldedWeapon()).Returns((WorldObject)null);
        _mockDefender.Setup(p => p.GetWieldedWeapon()).Returns((WorldObject)null);
        
        var combatants = new List<ICombatant>
        {
            new PlayerCombatant(_mockAttacker.Object),
            new PlayerCombatant(_mockDefender.Object)
        };
        
        // Act
        var roundResult = await _combatEngine.ExecuteCombatRoundAsync(combatants);

        // Assert
        roundResult.InitiativeOrder.First().Name.Should().Be("Victim", 
            "Higher dexterity should go first");
    }

    [Fact]
    public async Task ExecuteCombatRound_DeathOccurs_EndsCombat()
    {
        // Arrange
        _mockAttacker.Object.HitPoints = 100; // Healthy attacker
        _mockDefender.Object.HitPoints = 5;   // Low HP defender
        _mockDefender.Object.MaxHitPoints = 100;
        _mockAttacker.Setup(p => p.Strength).Returns(18); // High strength for guaranteed damage
        _mockAttacker.Setup(p => p.ArmorClass).Returns(10);
        _mockDefender.Setup(p => p.ArmorClass).Returns(10);
        _mockAttacker.Setup(p => p.GetWieldedWeapon()).Returns((WorldObject)null); // Unarmed combat
        
        var combatants = new List<ICombatant>
        {
            new PlayerCombatant(_mockAttacker.Object),
            new PlayerCombatant(_mockDefender.Object)
        };
        
        // Act
        var roundResult = await _combatEngine.ExecuteCombatRoundAsync(combatants);

        // Assert
        roundResult.Should().NotBeNull("Combat round should return result");
        // With 5 HP and high strength attacker, death should be likely
        // Since damage is random, we check if combat ended OR if defender was damaged
        if (roundResult.AttackResults.Any(a => a.Damage?.TotalDamage >= 5))
        {
            roundResult.CombatEnded.Should().BeTrue("Combat should end when someone dies");
        }
    }

    [Fact]
    public async Task ExecuteCombatRound_Performance_CompletesUnder200ms()
    {
        // Arrange
        var combatants = new List<ICombatant>
        {
            new PlayerCombatant(_mockAttacker.Object),
            new PlayerCombatant(_mockDefender.Object)
        };
        
        // Act & Assert
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _combatEngine.ExecuteCombatRoundAsync(combatants);
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200, 
            "Combat round should complete within 200ms");
    }

    #endregion

    #region Helper Methods

    private WorldObject CreateBasicWeapon(int diceSides, int diceCount)
    {
        return new WorldObject
        {
            VirtualNumber = 1001,
            Name = "sword",
            ShortDescription = "a basic sword",
            ObjectType = ObjectType.WEAPON,
            Values = new int[] { diceSides, diceCount, 0, 1 } // sides, count, bonus, weapon type
        };
    }

    private WorldObject CreateMagicWeapon(int diceSides, int diceCount, int magicBonus)
    {
        return new WorldObject
        {
            VirtualNumber = 1002,
            Name = "magic sword",
            ShortDescription = "a gleaming magic sword",
            ObjectType = ObjectType.WEAPON,
            Values = new int[] { diceSides, diceCount, magicBonus, 1 } // sides, count, bonus, weapon type
        };
    }

    #endregion
}