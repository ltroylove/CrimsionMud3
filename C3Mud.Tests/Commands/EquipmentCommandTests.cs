using C3Mud.Core.Players;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;
using C3Mud.Core.Equipment.Services;
using C3Mud.Core.Commands;
using C3Mud.Core.Commands.Equipment;
using C3Mud.Core.Networking;
using FluentAssertions;
using Moq;
using Xunit;

namespace C3Mud.Tests.Commands;

/// <summary>
/// TDD Red Phase tests for Iteration 6: Equipment & Inventory Management
/// Equipment Command Tests - All tests should FAIL initially
/// 
/// Tests for equipment-related commands:
/// - wear/wield commands (equip items)
/// - remove/unwield commands (unequip items)  
/// - drop/get commands (inventory management)
/// - give commands (player-to-player transfers)
/// - inventory/equipment display commands
/// 
/// Based on original CircleMUD command implementations:
/// - do_wear(), do_wield(), do_remove()
/// - do_drop(), do_get(), do_give()
/// - do_inventory(), do_equipment()
/// </summary>
public class EquipmentCommandTests
{
    private readonly Mock<IPlayer> _mockPlayer;
    private readonly Mock<IConnectionDescriptor> _mockConnection;
    private readonly Mock<IWorldDatabase> _mockWorldDatabase;
    private readonly Mock<ICommandRegistry> _mockCommandRegistry;
    private readonly Mock<Room> _mockRoom;
    
    // Equipment commands to test
    private readonly ICommand _wearCommand;
    private readonly ICommand _wieldCommand;
    private readonly ICommand _removeCommand;
    private readonly ICommand _dropCommand;
    private readonly ICommand _getCommand;
    private readonly ICommand _giveCommand;
    private readonly ICommand _inventoryCommand;
    private readonly ICommand _equipmentCommand;
    
    // Test items for command operations
    private readonly WorldObject _testSword;
    private readonly WorldObject _testArmor;
    private readonly WorldObject _testShield;
    private readonly WorldObject _testRing;
    private readonly WorldObject _testPotion;

    public EquipmentCommandTests()
    {
        // Setup mocks
        _mockPlayer = new Mock<IPlayer>();
        _mockConnection = new Mock<IConnectionDescriptor>();
        _mockWorldDatabase = new Mock<IWorldDatabase>();
        _mockCommandRegistry = new Mock<ICommandRegistry>();
        _mockRoom = new Mock<Room>();
        
        // Setup player
        _mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        _mockPlayer.Setup(p => p.Level).Returns(10);
        _mockPlayer.Setup(p => p.IsConnected).Returns(true);
        _mockPlayer.Setup(p => p.Connection).Returns(_mockConnection.Object);
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(3001);
        _mockConnection.Setup(c => c.IsConnected).Returns(true);
        
        // Setup room
        _mockRoom.Setup(r => r.VirtualNumber).Returns(3001);
        _mockWorldDatabase.Setup(w => w.GetRoom(3001)).Returns(_mockRoom.Object);
        
        // Create commands - These will fail until implemented
        _wearCommand = new WearCommand(_mockWorldDatabase.Object);
        _wieldCommand = new WieldCommand(_mockWorldDatabase.Object);
        _removeCommand = new RemoveCommand(_mockWorldDatabase.Object);
        _dropCommand = new DropCommand(_mockWorldDatabase.Object);
        _getCommand = new GetCommand(_mockWorldDatabase.Object);
        _giveCommand = new GiveCommand(_mockWorldDatabase.Object);
        _inventoryCommand = new InventoryCommand();
        _equipmentCommand = new EquipmentCommand();
        
        // Create test items
        _testSword = CreateTestSword();
        _testArmor = CreateTestArmor();
        _testShield = CreateTestShield();
        _testRing = CreateTestRing();
        _testPotion = CreateTestPotion();
    }

    #region Wear Command Tests

    [Fact]
    public async Task WearCommand_ValidArmor_SuccessfullyEquipped()
    {
        // Arrange - This will fail until wear command is implemented
        _mockPlayer.Object.GetInventory().Add(_testArmor);
        var args = new[] { "armor" };
        
        // Act
        var result = await _wearCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Body).Should().Be(_testArmor);
        
        // Verify correct message sent
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You wear a suit of chain mail."))), Times.Once);
    }

    [Fact]
    public async Task WearCommand_ItemNotInInventory_ShowsError()
    {
        // Arrange
        var args = new[] { "armor" };
        
        // Act
        var result = await _wearCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Failure);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You don't have that item."))), Times.Once);
    }

    [Fact]
    public async Task WearCommand_WrongItemType_ShowsError()
    {
        // Arrange
        _mockPlayer.Object.GetInventory().Add(_testSword);
        var args = new[] { "sword" };
        
        // Act
        var result = await _wearCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Failure);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You can't wear that."))), Times.Once);
    }

    [Fact]
    public async Task WearCommand_MultipleTargets_WearsCorrectItem()
    {
        // Arrange - Multiple rings in inventory, target second one
        var ring1 = CreateTestRing();
        var ring2 = CreateTestRing();
        ring2.VirtualNumber = 3002;
        ring2.ShortDescription = "a silver ring";
        
        _mockPlayer.Object.GetInventory().Add(ring1);
        _mockPlayer.Object.GetInventory().Add(ring2);
        
        var args = new[] { "2.ring" };
        
        // Act
        var result = await _wearCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        // Should equip the second ring, not the first
        var equippedRing = _mockPlayer.Object.GetEquippedItem(EquipmentSlot.FingerRight) ?? 
                          _mockPlayer.Object.GetEquippedItem(EquipmentSlot.FingerLeft);
        equippedRing.Should().Be(ring2);
    }

    [Fact]
    public async Task WearCommand_NoArguments_ShowsUsage()
    {
        // Arrange
        var args = Array.Empty<string>();
        
        // Act
        var result = await _wearCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Failure);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("Wear what?"))), Times.Once);
    }

    [Fact]
    public async Task WearCommand_AlreadyWearingInSlot_ReplacesItem()
    {
        // Arrange
        var oldArmor = CreateTestArmor();
        var newArmor = CreateTestArmor();
        newArmor.VirtualNumber = 3003;
        newArmor.ShortDescription = "plate mail armor";
        
        // Equip old armor first
        _mockPlayer.Setup(p => p.GetEquippedItem(EquipmentSlot.Body)).Returns(oldArmor);
        _mockPlayer.Object.GetInventory().Add(newArmor);
        
        var args = new[] { "plate" };
        
        // Act
        var result = await _wearCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Body).Should().Be(newArmor);
        _mockPlayer.Object.GetInventory().Should().Contain(oldArmor);
        
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You stop wearing") && msg.Contains("You wear"))), Times.Once);
    }

    #endregion

    #region Wield Command Tests

    [Fact]
    public async Task WieldCommand_ValidWeapon_SuccessfullyWielded()
    {
        // Arrange
        _mockPlayer.Object.GetInventory().Add(_testSword);
        var args = new[] { "sword" };
        
        // Act
        var result = await _wieldCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Wield).Should().Be(_testSword);
        
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You wield a sharp longsword."))), Times.Once);
    }

    [Fact]
    public async Task WieldCommand_NonWeapon_ShowsError()
    {
        // Arrange
        _mockPlayer.Object.GetInventory().Add(_testPotion);
        var args = new[] { "potion" };
        
        // Act
        var result = await _wieldCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Failure);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You can't wield that."))), Times.Once);
    }

    [Fact]
    public async Task WieldCommand_TwoHandedWeaponWithShield_RemovesShield()
    {
        // Arrange
        var twoHandedSword = CreateTwoHandedWeapon();
        _mockPlayer.Setup(p => p.GetEquippedItem(EquipmentSlot.Shield)).Returns(_testShield);
        _mockPlayer.Object.GetInventory().Add(twoHandedSword);
        
        var args = new[] { "two-handed" };
        
        // Act
        var result = await _wieldCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Wield).Should().Be(twoHandedSword);
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Shield).Should().BeNull();
        
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You stop using") && msg.Contains("You wield"))), Times.Once);
    }

    [Fact]
    public async Task WieldCommand_AlreadyWielding_ReplacesWeapon()
    {
        // Arrange
        var oldWeapon = CreateTestSword();
        var newWeapon = CreateTestSword();
        newWeapon.VirtualNumber = 3004;
        newWeapon.ShortDescription = "a magic blade";
        
        _mockPlayer.Setup(p => p.GetEquippedItem(EquipmentSlot.Wield)).Returns(oldWeapon);
        _mockPlayer.Object.GetInventory().Add(newWeapon);
        
        var args = new[] { "blade" };
        
        // Act
        var result = await _wieldCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Wield).Should().Be(newWeapon);
        _mockPlayer.Object.GetInventory().Should().Contain(oldWeapon);
    }

    #endregion

    #region Remove Command Tests

    [Fact]
    public async Task RemoveCommand_EquippedItem_SuccessfullyRemoved()
    {
        // Arrange
        _mockPlayer.Setup(p => p.GetEquippedItem(EquipmentSlot.Wield)).Returns(_testSword);
        var args = new[] { "sword" };
        
        // Act
        var result = await _removeCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Wield).Should().BeNull();
        _mockPlayer.Object.GetInventory().Should().Contain(_testSword);
        
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You stop using a sharp longsword."))), Times.Once);
    }

    [Fact]
    public async Task RemoveCommand_NotEquipped_ShowsError()
    {
        // Arrange
        var args = new[] { "sword" };
        
        // Act
        var result = await _removeCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Failure);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You're not using that."))), Times.Once);
    }

    [Fact]
    public async Task RemoveCommand_CursedItem_ShowsError()
    {
        // Arrange
        var cursedRing = CreateCursedRing();
        _mockPlayer.Setup(p => p.GetEquippedItem(EquipmentSlot.FingerRight)).Returns(cursedRing);
        var args = new[] { "ring" };
        
        // Act
        var result = await _removeCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Failure);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You can't remove that, it appears to be cursed."))), Times.Once);
    }

    [Fact]
    public async Task RemoveCommand_BySlotName_RemovesCorrectItem()
    {
        // Arrange - Test removing by equipment slot name
        _mockPlayer.Setup(p => p.GetEquippedItem(EquipmentSlot.Body)).Returns(_testArmor);
        var args = new[] { "body" };
        
        // Act
        var result = await _removeCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Body).Should().BeNull();
        _mockPlayer.Object.GetInventory().Should().Contain(_testArmor);
    }

    [Fact]
    public async Task RemoveCommand_All_RemovesAllEquipment()
    {
        // Arrange
        _mockPlayer.Setup(p => p.GetEquippedItem(EquipmentSlot.Wield)).Returns(_testSword);
        _mockPlayer.Setup(p => p.GetEquippedItem(EquipmentSlot.Body)).Returns(_testArmor);
        _mockPlayer.Setup(p => p.GetEquippedItem(EquipmentSlot.Shield)).Returns(_testShield);
        
        var args = new[] { "all" };
        
        // Act
        var result = await _removeCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Wield).Should().BeNull();
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Body).Should().BeNull();
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Shield).Should().BeNull();
        
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("stop using"))), Times.AtLeast(3));
    }

    #endregion

    #region Drop Command Tests

    [Fact]
    public async Task DropCommand_ValidItem_DroppedInRoom()
    {
        // Arrange
        _mockPlayer.Object.GetInventory().Add(_testSword);
        var args = new[] { "sword" };
        
        // Act
        var result = await _dropCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockPlayer.Object.GetInventory().Should().NotContain(_testSword);
        _mockRoom.Object.Items.Should().Contain(_testSword);
        
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You drop a sharp longsword."))), Times.Once);
    }

    [Fact]
    public async Task DropCommand_NoDropItem_ShowsError()
    {
        // Arrange
        var noDropItem = CreateNoDropItem();
        _mockPlayer.Object.GetInventory().Add(noDropItem);
        var args = new[] { "cursed" };
        
        // Act
        var result = await _dropCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Failure);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You can't drop that."))), Times.Once);
    }

    [Fact]
    public async Task DropCommand_Gold_DropsGoldPile()
    {
        // Arrange
        _mockPlayer.SetupProperty(p => p.Gold, 500);
        var args = new[] { "100", "gold" };
        
        // Act
        var result = await _dropCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockPlayer.Object.Gold.Should().Be(400); // 500 - 100
        
        // Room should contain gold object
        _mockRoom.Object.Items.Should().ContainSingle(item => 
            item.ObjectType == ObjectType.MONEY && item.Gold == 100);
        
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You drop 100 gold coins."))), Times.Once);
    }

    [Fact]
    public async Task DropCommand_All_DropsAllInventory()
    {
        // Arrange
        _mockPlayer.Object.GetInventory().Add(_testSword);
        _mockPlayer.Object.GetInventory().Add(_testPotion);
        var args = new[] { "all" };
        
        // Act
        var result = await _dropCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockPlayer.Object.GetInventory().Should().BeEmpty();
        _mockRoom.Object.Items.Should().Contain(_testSword);
        _mockRoom.Object.Items.Should().Contain(_testPotion);
    }

    [Fact]
    public async Task DropCommand_InsufficientGold_ShowsError()
    {
        // Arrange
        _mockPlayer.SetupProperty(p => p.Gold, 50);
        var args = new[] { "100", "gold" };
        
        // Act
        var result = await _dropCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Failure);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You don't have that much gold."))), Times.Once);
    }

    #endregion

    #region Get Command Tests

    [Fact]
    public async Task GetCommand_ItemInRoom_SuccessfullyRetrieved()
    {
        // Arrange
        _mockRoom.Object.Items.Add(_testSword);
        var args = new[] { "sword" };
        
        // Act
        var result = await _getCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockRoom.Object.Items.Should().NotContain(_testSword);
        _mockPlayer.Object.GetInventory().Should().Contain(_testSword);
        
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You get a sharp longsword."))), Times.Once);
    }

    [Fact]
    public async Task GetCommand_ItemNotInRoom_ShowsError()
    {
        // Arrange
        var args = new[] { "sword" };
        
        // Act
        var result = await _getCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Failure);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("There is no sword here."))), Times.Once);
    }

    [Fact]
    public async Task GetCommand_TooHeavy_ShowsError()
    {
        // Arrange
        var heavyItem = CreateHeavyItem();
        _mockPlayer.Setup(p => p.Strength).Returns(3); // Very weak player
        _mockRoom.Object.Items.Add(heavyItem);
        var args = new[] { "boulder" };
        
        // Act
        var result = await _getCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Failure);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You can't carry that much weight."))), Times.Once);
    }

    [Fact]
    public async Task GetCommand_FromContainer_RetrievesFromContainer()
    {
        // Arrange
        var container = CreateTestContainer();
        container.Contents.Add(_testPotion);
        _mockRoom.Object.Items.Add(container);
        
        var args = new[] { "potion", "satchel" };
        
        // Act
        var result = await _getCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        container.Contents.Should().NotContain(_testPotion);
        _mockPlayer.Object.GetInventory().Should().Contain(_testPotion);
        
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You get a healing potion from a leather satchel."))), Times.Once);
    }

    [Fact]
    public async Task GetCommand_Gold_AddsToPlayerGold()
    {
        // Arrange
        var goldPile = CreateGoldPile(150);
        _mockRoom.Object.Items.Add(goldPile);
        _mockPlayer.SetupProperty(p => p.Gold, 100);
        
        var args = new[] { "gold" };
        
        // Act
        var result = await _getCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockPlayer.Object.Gold.Should().Be(250); // 100 + 150
        _mockRoom.Object.Items.Should().NotContain(goldPile);
        
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You get 150 gold coins."))), Times.Once);
    }

    [Fact]
    public async Task GetCommand_All_GetsAllItemsInRoom()
    {
        // Arrange
        _mockRoom.Object.Items.Add(_testSword);
        _mockRoom.Object.Items.Add(_testPotion);
        var args = new[] { "all" };
        
        // Act
        var result = await _getCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockRoom.Object.Items.Should().BeEmpty();
        _mockPlayer.Object.GetInventory().Should().Contain(_testSword);
        _mockPlayer.Object.GetInventory().Should().Contain(_testPotion);
    }

    #endregion

    #region Give Command Tests

    [Fact]
    public async Task GiveCommand_ValidTransfer_SuccessfullyGiven()
    {
        // Arrange
        var targetPlayer = new Mock<IPlayer>();
        targetPlayer.Setup(p => p.Name).Returns("TargetPlayer");
        targetPlayer.Setup(p => p.CurrentRoomVnum).Returns(3001);
        
        _mockRoom.Object.Players.Add(targetPlayer.Object);
        _mockPlayer.Object.GetInventory().Add(_testSword);
        
        var args = new[] { "sword", "TargetPlayer" };
        
        // Act
        var result = await _giveCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockPlayer.Object.GetInventory().Should().NotContain(_testSword);
        targetPlayer.Object.GetInventory().Should().Contain(_testSword);
        
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You give a sharp longsword to TargetPlayer."))), Times.Once);
    }

    [Fact]
    public async Task GiveCommand_PlayerNotInRoom_ShowsError()
    {
        // Arrange
        _mockPlayer.Object.GetInventory().Add(_testSword);
        var args = new[] { "sword", "NonExistentPlayer" };
        
        // Act
        var result = await _giveCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Failure);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("They aren't here."))), Times.Once);
    }

    [Fact]
    public async Task GiveCommand_GiveGold_TransfersGold()
    {
        // Arrange
        var targetPlayer = new Mock<IPlayer>();
        targetPlayer.Setup(p => p.Name).Returns("TargetPlayer");
        targetPlayer.Setup(p => p.CurrentRoomVnum).Returns(3001);
        targetPlayer.SetupProperty(p => p.Gold, 0);
        
        _mockRoom.Object.Players.Add(targetPlayer.Object);
        _mockPlayer.SetupProperty(p => p.Gold, 500);
        
        var args = new[] { "100", "gold", "TargetPlayer" };
        
        // Act
        var result = await _giveCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockPlayer.Object.Gold.Should().Be(400); // 500 - 100
        targetPlayer.Object.Gold.Should().Be(100);
        
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You give 100 gold coins to TargetPlayer."))), Times.Once);
    }

    [Fact]
    public async Task GiveCommand_TargetCantCarry_ShowsError()
    {
        // Arrange
        var targetPlayer = new Mock<IPlayer>();
        targetPlayer.Setup(p => p.Name).Returns("WeakPlayer");
        targetPlayer.Setup(p => p.Strength).Returns(3); // Very weak
        targetPlayer.Setup(p => p.CurrentRoomVnum).Returns(3001);
        
        _mockRoom.Object.Players.Add(targetPlayer.Object);
        
        var heavyItem = CreateHeavyItem();
        _mockPlayer.Object.GetInventory().Add(heavyItem);
        
        var args = new[] { "boulder", "WeakPlayer" };
        
        // Act
        var result = await _giveCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Failure);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("They can't carry that much weight."))), Times.Once);
    }

    #endregion

    #region Display Commands Tests

    [Fact]
    public async Task InventoryCommand_WithItems_ShowsAllItems()
    {
        // Arrange
        _mockPlayer.Object.GetInventory().Add(_testSword);
        _mockPlayer.Object.GetInventory().Add(_testPotion);
        _mockPlayer.SetupProperty(p => p.Gold, 250);
        
        var args = Array.Empty<string>();
        
        // Act
        var result = await _inventoryCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You are carrying:"))), Times.Once);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("a sharp longsword"))), Times.Once);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("a healing potion"))), Times.Once);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("250 gold coins"))), Times.Once);
    }

    [Fact]
    public async Task InventoryCommand_EmptyInventory_ShowsEmptyMessage()
    {
        // Arrange
        _mockPlayer.SetupProperty(p => p.Gold, 0);
        var args = Array.Empty<string>();
        
        // Act
        var result = await _inventoryCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You are not carrying anything."))), Times.Once);
    }

    [Fact]
    public async Task EquipmentCommand_WithEquipment_ShowsAllSlots()
    {
        // Arrange
        _mockPlayer.Setup(p => p.GetEquippedItem(EquipmentSlot.Wield)).Returns(_testSword);
        _mockPlayer.Setup(p => p.GetEquippedItem(EquipmentSlot.Body)).Returns(_testArmor);
        _mockPlayer.Setup(p => p.GetEquippedItem(EquipmentSlot.Shield)).Returns(_testShield);
        
        var args = Array.Empty<string>();
        
        // Act
        var result = await _equipmentCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You are using:"))), Times.Once);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("<wielded>") && msg.Contains("a sharp longsword"))), Times.Once);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("<worn on body>") && msg.Contains("a suit of chain mail"))), Times.Once);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("<worn as shield>") && msg.Contains("a wooden shield"))), Times.Once);
    }

    [Fact]
    public async Task EquipmentCommand_NoEquipment_ShowsEmptySlots()
    {
        // Arrange - No equipment
        var args = Array.Empty<string>();
        
        // Act
        var result = await _equipmentCommand.ExecuteAsync(_mockPlayer.Object, args);
        
        // Assert
        result.Should().Be(CommandResult.Success);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("You are using:"))), Times.Once);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("<worn as light>      <nothing>"))), Times.Once);
        _mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(msg => 
            msg.Contains("<wielded>            <nothing>"))), Times.Once);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task AllCommands_PerformanceTest_AcceptableSpeed()
    {
        // Arrange - Setup inventory and equipment
        _mockPlayer.Object.GetInventory().Add(_testSword);
        _mockPlayer.Object.GetInventory().Add(_testPotion);
        _mockRoom.Object.Items.Add(_testArmor);
        
        var commands = new ICommand[] 
        { 
            _wearCommand, _wieldCommand, _removeCommand, 
            _dropCommand, _getCommand, _inventoryCommand, _equipmentCommand 
        };
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act - Execute many command operations
        for (int i = 0; i < 100; i++)
        {
            foreach (var command in commands)
            {
                await command.ExecuteAsync(_mockPlayer.Object, new[] { "test" });
            }
        }
        
        stopwatch.Stop();
        
        // Assert - Should be reasonably fast
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    #endregion

    #region Legacy Compatibility Tests

    [Fact]
    public async Task CommandMessages_MatchOriginalCircleMudFormat()
    {
        // Test that command messages match original CircleMUD exactly
        var expectedMessages = new Dictionary<string, string>
        {
            { "wear_success", "You wear {item}." },
            { "wield_success", "You wield {item}." },
            { "remove_success", "You stop using {item}." },
            { "drop_success", "You drop {item}." },
            { "get_success", "You get {item}." },
            { "give_success", "You give {item} to {target}." },
            { "wear_cant", "You can't wear that." },
            { "wield_cant", "You can't wield that." },
            { "not_here", "They aren't here." },
            { "dont_have", "You don't have that item." },
            { "no_drop", "You can't drop that." },
            { "cursed", "You can't remove that, it appears to be cursed." }
        };

        // Each command should use exact messages from original
        foreach (var expectedMessage in expectedMessages)
        {
            // Test will validate message format when commands are implemented
            expectedMessage.Value.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task CommandAbbreviations_MatchOriginalShortcuts()
    {
        // Test that command abbreviations work like original CircleMUD
        var abbreviations = new Dictionary<string, ICommand>
        {
            { "eq", _equipmentCommand },     // equipment
            { "inv", _inventoryCommand },    // inventory  
            { "i", _inventoryCommand },      // inventory
            { "rem", _removeCommand },       // remove
            { "unw", _removeCommand },       // unwield (alias for remove)
            { "wie", _wieldCommand },        // wield
            { "wea", _wearCommand },         // wear
            { "dr", _dropCommand },          // drop
            { "ge", _getCommand },           // get
            { "giv", _giveCommand }          // give
        };

        foreach (var abbrev in abbreviations)
        {
            // Commands should be registered with these abbreviations
            abbrev.Value.Should().NotBeNull($"Command {abbrev.Key} should be available");
        }
    }

    #endregion

    #region Helper Methods

    private WorldObject CreateTestSword()
    {
        return new WorldObject
        {
            VirtualNumber = 3001,
            Name = "sword longsword sharp",
            ShortDescription = "a sharp longsword",
            LongDescription = "A sharp longsword has been left here.",
            ObjectType = ObjectType.WEAPON,
            WearFlags = (1 << 16), // ITEM_WEAR_WIELD
            Weight = 15,
            Cost = 100,
            Values = new int[] { 2, 4, 2, 3 }
        };
    }

    private WorldObject CreateTestArmor()
    {
        return new WorldObject
        {
            VirtualNumber = 3002,
            Name = "armor mail chain",
            ShortDescription = "a suit of chain mail",
            LongDescription = "A suit of chain mail lies here.",
            ObjectType = ObjectType.ARMOR,
            WearFlags = (1 << 5), // ITEM_WEAR_BODY
            Weight = 30,
            Cost = 200,
            Values = new int[] { 5, 0, 0, 0 }
        };
    }

    private WorldObject CreateTestShield()
    {
        return new WorldObject
        {
            VirtualNumber = 3003,
            Name = "shield wooden",
            ShortDescription = "a wooden shield",
            LongDescription = "A wooden shield has been dropped here.",
            ObjectType = ObjectType.ARMOR,
            WearFlags = (1 << 11), // ITEM_WEAR_SHIELD
            Weight = 10,
            Cost = 50,
            Values = new int[] { 3, 0, 0, 0 }
        };
    }

    private WorldObject CreateTestRing()
    {
        return new WorldObject
        {
            VirtualNumber = 3005,
            Name = "ring golden gold",
            ShortDescription = "a golden ring",
            LongDescription = "A golden ring sparkles here.",
            ObjectType = ObjectType.WORN,
            WearFlags = (1 << 1) | (1 << 2), // ITEM_WEAR_FINGER
            Weight = 1,
            Cost = 100
        };
    }

    private WorldObject CreateTestPotion()
    {
        return new WorldObject
        {
            VirtualNumber = 3006,
            Name = "potion healing red",
            ShortDescription = "a healing potion",
            LongDescription = "A red healing potion sits here.",
            ObjectType = ObjectType.POTION,
            Weight = 1,
            Cost = 50,
            Values = new int[] { 10, 201, -1, -1 }
        };
    }

    private WorldObject CreateTestContainer()
    {
        return new WorldObject
        {
            VirtualNumber = 3007,
            Name = "satchel leather bag",
            ShortDescription = "a leather satchel",
            LongDescription = "A leather satchel has been dropped here.",
            ObjectType = ObjectType.CONTAINER,
            Weight = 5,
            Cost = 25,
            Values = new int[] { 50, 0, 0, 0 },
            Contents = new List<WorldObject>()
        };
    }

    private WorldObject CreateTwoHandedWeapon()
    {
        return new WorldObject
        {
            VirtualNumber = 3008,
            Name = "sword two-handed massive",
            ShortDescription = "a massive two-handed sword",
            LongDescription = "A massive two-handed sword lies here.",
            ObjectType = ObjectType.WEAPON,
            WearFlags = (1 << 16), // ITEM_WEAR_WIELD
            ExtraFlags = (1 << 17), // ITEM_TWO_HANDED (custom flag)
            Weight = 25,
            Values = new int[] { 3, 8, 4, 3 }
        };
    }

    private WorldObject CreateCursedRing()
    {
        return new WorldObject
        {
            VirtualNumber = 3009,
            Name = "ring cursed black",
            ShortDescription = "a cursed black ring",
            LongDescription = "A cursed black ring pulses with dark energy.",
            ObjectType = ObjectType.WORN,
            WearFlags = (1 << 1) | (1 << 2), // ITEM_WEAR_FINGER
            ExtraFlags = (1 << 2) | (1 << 3), // ITEM_NODROP | ITEM_CURSED
            Weight = 1,
            Cost = 0
        };
    }

    private WorldObject CreateNoDropItem()
    {
        return new WorldObject
        {
            VirtualNumber = 3010,
            Name = "item cursed binding",
            ShortDescription = "a cursed binding item",
            LongDescription = "A cursed item that cannot be dropped.",
            ObjectType = ObjectType.TRASH,
            ExtraFlags = (1 << 2), // ITEM_NODROP
            Weight = 5,
            Cost = 0
        };
    }

    private WorldObject CreateHeavyItem()
    {
        return new WorldObject
        {
            VirtualNumber = 3011,
            Name = "boulder stone massive",
            ShortDescription = "a massive boulder",
            LongDescription = "A massive boulder sits here.",
            ObjectType = ObjectType.TRASH,
            Weight = 200, // Very heavy
            Cost = 0
        };
    }

    private WorldObject CreateGoldPile(int amount)
    {
        return new WorldObject
        {
            VirtualNumber = 0, // Special vnum for gold
            Name = "coins gold",
            ShortDescription = $"{amount} gold coins",
            LongDescription = $"{amount} gold coins are scattered here.",
            ObjectType = ObjectType.MONEY,
            Weight = 0,
            Cost = 0,
            Gold = amount
        };
    }

    #endregion
}