using C3Mud.Core.Players;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;
using C3Mud.Core.Equipment.Models;
using C3Mud.Core.Equipment.Services;
using C3Mud.Core.Characters;
using FluentAssertions;
using Moq;
using Xunit;

namespace C3Mud.Tests.Equipment;

/// <summary>
/// TDD Red Phase tests for Iteration 6: Equipment & Inventory Management
/// Inventory Management Tests - All tests should FAIL initially
/// 
/// Based on original CircleMUD inventory mechanics:
/// - Inventory linked list: struct obj_data *carrying
/// - Weight calculations: IS_CARRYING_W(ch) <= CAN_CARRY_W(ch)
/// - Item count limits: IS_CARRYING_N(ch) <= CAN_CARRY_N(ch)
/// - Container handling: get/put operations with containers
/// - Drop/get room interactions
/// </summary>
public class InventoryManagementTests
{
    private readonly Mock<IPlayer> _mockPlayer;
    private readonly Mock<IWorldDatabase> _mockWorldDatabase;
    private readonly Mock<IInventoryManager> _mockInventoryManager;
    private readonly Mock<Room> _mockRoom;
    
    // Test items for inventory operations
    private readonly WorldObject _testSword;
    private readonly WorldObject _testPotion;
    private readonly WorldObject _testContainer;
    private readonly WorldObject _testGold;
    private readonly WorldObject _testFood;

    public InventoryManagementTests()
    {
        _mockPlayer = new Mock<IPlayer>();
        _mockWorldDatabase = new Mock<IWorldDatabase>();
        _mockInventoryManager = new Mock<IInventoryManager>();
        _mockRoom = new Mock<Room>();
        
        // Setup basic player properties
        _mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        _mockPlayer.Setup(p => p.Level).Returns(10);
        _mockPlayer.Setup(p => p.Strength).Returns(16);
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(3001);
        
        // Setup room
        _mockRoom.Setup(r => r.VirtualNumber).Returns(3001);
        _mockWorldDatabase.Setup(w => w.GetRoom(3001)).Returns(_mockRoom.Object);
        
        // Create test items
        _testSword = CreateTestSword();
        _testPotion = CreateTestPotion();
        _testContainer = CreateTestContainer();
        _testGold = CreateTestGold();
        _testFood = CreateTestFood();
    }

    #region Basic Inventory Operations

    [Fact]
    public void AddItem_ValidItem_SuccessfullyAdded()
    {
        // Arrange - This will fail until inventory system is implemented
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        
        // Act
        var result = inventoryManager.AddItem(_testSword);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("You get a sharp longsword.");
        _mockPlayer.Object.GetInventory().Should().Contain(_testSword);
    }

    [Fact]
    public void RemoveItem_ExistingItem_SuccessfullyRemoved()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        inventoryManager.AddItem(_testSword);
        
        // Act
        var result = inventoryManager.RemoveItem(_testSword);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("You drop a sharp longsword.");
        _mockPlayer.Object.GetInventory().Should().NotContain(_testSword);
    }

    [Fact]
    public void RemoveItem_NonExistentItem_ReturnsFailure()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        
        // Act
        var result = inventoryManager.RemoveItem(_testSword);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("You don't have that item.");
    }

    [Fact]
    public void HasItem_ExistingItem_ReturnsTrue()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        inventoryManager.AddItem(_testSword);
        
        // Act
        var hasItem = inventoryManager.HasItem(_testSword.VirtualNumber);
        
        // Assert
        hasItem.Should().BeTrue();
    }

    [Fact]
    public void HasItem_NonExistentItem_ReturnsFalse()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        
        // Act
        var hasItem = inventoryManager.HasItem(_testSword.VirtualNumber);
        
        // Assert
        hasItem.Should().BeFalse();
    }

    #endregion

    #region Weight and Capacity Management

    [Fact]
    public void AddItem_ExceedsWeightLimit_ReturnsFailure()
    {
        // Arrange - Player with very low strength (capacity of 10 lbs)
        _mockPlayer.Setup(p => p.Strength).Returns(3);
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        var heavyItem = CreateHeavyItem(); // 50 lbs
        
        // Act
        var result = inventoryManager.AddItem(heavyItem);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("That item is too heavy for you to carry.");
    }

    [Fact]
    public void AddItem_ExceedsItemLimit_ReturnsFailure()
    {
        // Arrange - Fill inventory to capacity
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        var maxItems = inventoryManager.GetMaxItemCapacity(); // Should be based on DEX
        
        // Fill inventory to max
        for (int i = 0; i < maxItems; i++)
        {
            var item = CreateLightItem(i + 1000);
            inventoryManager.AddItem(item);
        }
        
        // Act - Try to add one more
        var result = inventoryManager.AddItem(_testSword);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Your hands are full, you can't carry any more items.");
    }

    [Fact]
    public void GetCurrentWeight_MultipleItems_CalculatesCorrectly()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        inventoryManager.AddItem(_testSword);   // 15 lbs
        inventoryManager.AddItem(_testPotion);  // 1 lb
        inventoryManager.AddItem(_testContainer); // 5 lbs (empty)
        
        // Act
        var totalWeight = inventoryManager.GetCurrentWeight();
        
        // Assert
        totalWeight.Should().Be(21); // 15 + 1 + 5
    }

    [Fact]
    public void GetMaxWeightCapacity_VariousStrengthLevels_MatchesOriginalFormula()
    {
        // Test data based on original CircleMUD str_app[] table
        var testCases = new[]
        {
            new { Strength = 3, ExpectedCapacity = 10 },
            new { Strength = 16, ExpectedCapacity = 130 },
            new { Strength = 18, ExpectedCapacity = 200 },
            new { Strength = 25, ExpectedCapacity = 640 }
        };

        foreach (var testCase in testCases)
        {
            // Arrange
            _mockPlayer.Setup(p => p.Strength).Returns(testCase.Strength);
            var inventoryManager = new InventoryManager(_mockPlayer.Object);
            
            // Act
            var capacity = inventoryManager.GetMaxWeightCapacity();
            
            // Assert
            capacity.Should().Be(testCase.ExpectedCapacity,
                $"Strength {testCase.Strength} should give weight capacity {testCase.ExpectedCapacity}");
        }
    }

    [Fact]
    public void GetMaxItemCapacity_VariousDexterityLevels_MatchesOriginalFormula()
    {
        // Test data based on original CircleMUD dex calculations
        var testCases = new[]
        {
            new { Dexterity = 3, ExpectedCapacity = 5 },
            new { Dexterity = 16, ExpectedCapacity = 20 },
            new { Dexterity = 18, ExpectedCapacity = 25 },
            new { Dexterity = 25, ExpectedCapacity = 35 }
        };

        foreach (var testCase in testCases)
        {
            // Arrange
            _mockPlayer.Setup(p => p.Dexterity).Returns(testCase.Dexterity);
            var inventoryManager = new InventoryManager(_mockPlayer.Object);
            
            // Act
            var capacity = inventoryManager.GetMaxItemCapacity();
            
            // Assert
            capacity.Should().Be(testCase.ExpectedCapacity,
                $"Dexterity {testCase.Dexterity} should give item capacity {testCase.ExpectedCapacity}");
        }
    }

    #endregion

    #region Container Operations

    [Fact]
    public void PutItemInContainer_ValidOperation_SuccessfullyStored()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        inventoryManager.AddItem(_testContainer);
        inventoryManager.AddItem(_testPotion);
        
        // Act
        var result = inventoryManager.PutItemInContainer(_testPotion, _testContainer);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("You put a healing potion in a leather satchel.");
        _testContainer.Contents.Should().Contain(_testPotion);
        _mockPlayer.Object.GetInventory().Should().NotContain(_testPotion);
    }

    [Fact]
    public void GetItemFromContainer_ValidOperation_SuccessfullyRetrieved()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        inventoryManager.AddItem(_testContainer);
        _testContainer.Contents.Add(_testPotion);
        
        // Act
        var result = inventoryManager.GetItemFromContainer(_testPotion, _testContainer);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("You get a healing potion from a leather satchel.");
        _testContainer.Contents.Should().NotContain(_testPotion);
        _mockPlayer.Object.GetInventory().Should().Contain(_testPotion);
    }

    [Fact]
    public void PutItemInContainer_ContainerTooSmall_ReturnsFailure()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        var smallContainer = CreateSmallContainer(); // Max 5 lbs capacity
        var largeItem = CreateLargeItem(); // 10 lbs
        
        inventoryManager.AddItem(smallContainer);
        inventoryManager.AddItem(largeItem);
        
        // Act
        var result = inventoryManager.PutItemInContainer(largeItem, smallContainer);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("That item won't fit in the container.");
    }

    [Fact]
    public void PutItemInContainer_NotAContainer_ReturnsFailure()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        inventoryManager.AddItem(_testSword);
        inventoryManager.AddItem(_testPotion);
        
        // Act
        var result = inventoryManager.PutItemInContainer(_testPotion, _testSword);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("That's not a container.");
    }

    [Fact]
    public void ListContainerContents_WithItems_ShowsAllContents()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        _testContainer.Contents.Add(_testPotion);
        _testContainer.Contents.Add(_testFood);
        inventoryManager.AddItem(_testContainer);
        
        // Act
        var contents = inventoryManager.ListContainerContents(_testContainer);
        
        // Assert
        contents.Should().HaveCount(2);
        contents.Should().Contain(_testPotion);
        contents.Should().Contain(_testFood);
    }

    #endregion

    #region Room Interactions

    [Fact]
    public void DropItemInRoom_ValidItem_SuccessfullyDropped()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        inventoryManager.AddItem(_testSword);
        
        // Act
        var result = inventoryManager.DropItemInRoom(_testSword, _mockRoom.Object);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("You drop a sharp longsword.");
        _mockPlayer.Object.GetInventory().Should().NotContain(_testSword);
        _mockRoom.Object.Items.Should().Contain(_testSword);
    }

    [Fact]
    public void GetItemFromRoom_ValidItem_SuccessfullyRetrieved()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        _mockRoom.Object.Items.Add(_testSword);
        
        // Act
        var result = inventoryManager.GetItemFromRoom(_testSword, _mockRoom.Object);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("You get a sharp longsword.");
        _mockRoom.Object.Items.Should().NotContain(_testSword);
        _mockPlayer.Object.GetInventory().Should().Contain(_testSword);
    }

    [Fact]
    public void GetItemFromRoom_ItemNotInRoom_ReturnsFailure()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        
        // Act
        var result = inventoryManager.GetItemFromRoom(_testSword, _mockRoom.Object);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("There is no sharp longsword here.");
    }

    [Fact]
    public void DropItem_NoDropFlag_ReturnsFailure()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        var noDropItem = CreateNoDropItem();
        inventoryManager.AddItem(noDropItem);
        
        // Act
        var result = inventoryManager.DropItemInRoom(noDropItem, _mockRoom.Object);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("You can't drop that item.");
    }

    #endregion

    #region Gold and Money Handling

    [Fact]
    public void AddGold_ValidAmount_SuccessfullyAdded()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        var initialGold = _mockPlayer.Object.Gold;
        
        // Act
        var result = inventoryManager.AddGold(500);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("You receive 500 gold coins.");
        _mockPlayer.Object.Gold.Should().Be(initialGold + 500);
    }

    [Fact]
    public void SpendGold_SufficientFunds_SuccessfullySpent()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        _mockPlayer.SetupProperty(p => p.Gold, 1000);
        
        // Act
        var result = inventoryManager.SpendGold(300);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("You spend 300 gold coins.");
        _mockPlayer.Object.Gold.Should().Be(700);
    }

    [Fact]
    public void SpendGold_InsufficientFunds_ReturnsFailure()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        _mockPlayer.SetupProperty(p => p.Gold, 100);
        
        // Act
        var result = inventoryManager.SpendGold(500);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("You don't have enough gold.");
    }

    [Fact]
    public void DropGold_ValidAmount_CreatesCoinsPile()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        _mockPlayer.SetupProperty(p => p.Gold, 1000);
        
        // Act
        var result = inventoryManager.DropGold(200, _mockRoom.Object);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("You drop 200 gold coins.");
        _mockPlayer.Object.Gold.Should().Be(800);
        // Room should contain a gold object worth 200 coins
        _mockRoom.Object.Items.Should().ContainSingle(item => 
            item.ObjectType == ObjectType.MONEY && item.Gold == 200);
    }

    [Fact]
    public void GetGold_FromRoom_SuccessfullyRetrieved()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        var goldPile = CreateGoldPile(150);
        _mockRoom.Object.Items.Add(goldPile);
        var initialGold = _mockPlayer.Object.Gold;
        
        // Act
        var result = inventoryManager.GetGoldFromRoom(goldPile, _mockRoom.Object);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("You get 150 gold coins.");
        _mockPlayer.Object.Gold.Should().Be(initialGold + 150);
        _mockRoom.Object.Items.Should().NotContain(goldPile);
    }

    #endregion

    #region Item Identification and Search

    [Fact]
    public void FindItem_ByKeyword_ReturnsCorrectItem()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        inventoryManager.AddItem(_testSword);
        inventoryManager.AddItem(_testPotion);
        inventoryManager.AddItem(_testContainer);
        
        // Act
        var foundItem = inventoryManager.FindItem("sword");
        
        // Assert
        foundItem.Should().Be(_testSword);
    }

    [Fact]
    public void FindItem_MultipleMatches_ReturnsFirstMatch()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        var sword1 = CreateTestSword();
        var sword2 = CreateTestSword();
        sword2.VirtualNumber = 3002;
        sword2.ShortDescription = "a rusty sword";
        
        inventoryManager.AddItem(sword1);
        inventoryManager.AddItem(sword2);
        
        // Act
        var foundItem = inventoryManager.FindItem("sword");
        
        // Assert
        foundItem.Should().Be(sword1);
    }

    [Fact]
    public void FindItem_WithDotNotation_ReturnsNthMatch()
    {
        // Arrange - Test "2.sword" notation from original MUD
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        var sword1 = CreateTestSword();
        var sword2 = CreateTestSword();
        sword2.VirtualNumber = 3002;
        sword2.ShortDescription = "a rusty sword";
        
        inventoryManager.AddItem(sword1);
        inventoryManager.AddItem(sword2);
        
        // Act
        var foundItem = inventoryManager.FindItem("2.sword");
        
        // Assert
        foundItem.Should().Be(sword2);
    }

    [Fact]
    public void FindItem_NonExistent_ReturnsNull()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        inventoryManager.AddItem(_testSword);
        
        // Act
        var foundItem = inventoryManager.FindItem("axe");
        
        // Assert
        foundItem.Should().BeNull();
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void InventoryOperations_PerformanceTest_CompletesWithinTimeLimit()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        var items = new List<WorldObject>();
        
        // Create 100 test items
        for (int i = 0; i < 100; i++)
        {
            items.Add(CreateLightItem(i + 1000));
        }
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act - Add all items, then remove them
        foreach (var item in items)
        {
            inventoryManager.AddItem(item);
        }
        
        foreach (var item in items)
        {
            inventoryManager.RemoveItem(item);
        }
        
        stopwatch.Stop();
        
        // Assert - Should complete well under 1 second
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    [Fact]
    public void SearchOperations_LargeInventory_AcceptablePerformance()
    {
        // Arrange
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        
        // Add 50 items to inventory
        for (int i = 0; i < 50; i++)
        {
            inventoryManager.AddItem(CreateLightItem(i + 2000));
        }
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act - Perform 100 search operations
        for (int i = 0; i < 100; i++)
        {
            inventoryManager.FindItem($"item{i % 50}");
        }
        
        stopwatch.Stop();
        
        // Assert - Should complete quickly
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    #endregion

    #region Legacy Compatibility Tests

    [Fact]
    public void InventoryDisplay_MatchesOriginalFormat()
    {
        // Arrange - Based on original CircleMUD list_obj_to_char()
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        inventoryManager.AddItem(_testSword);
        inventoryManager.AddItem(_testPotion);
        inventoryManager.AddItem(_testContainer);
        
        // Act
        var inventoryDisplay = inventoryManager.GetInventoryDisplay();
        
        // Assert - Should match original format exactly
        var expectedLines = new[]
        {
            "a sharp longsword",
            "a healing potion", 
            "a leather satchel"
        };
        
        foreach (var expectedLine in expectedLines)
        {
            inventoryDisplay.Should().Contain(expectedLine);
        }
    }

    [Fact]
    public void WeightCalculations_MatchOriginalFormula()
    {
        // Test based on original IS_CARRYING_W and CAN_CARRY_W macros
        var testCases = new[]
        {
            new { Strength = 3, MaxWeight = 10, TestWeight = 15, CanCarry = false },
            new { Strength = 18, MaxWeight = 200, TestWeight = 150, CanCarry = true },
            new { Strength = 18, MaxWeight = 200, TestWeight = 250, CanCarry = false }
        };

        foreach (var testCase in testCases)
        {
            // Arrange
            _mockPlayer.Setup(p => p.Strength).Returns(testCase.Strength);
            var inventoryManager = new InventoryManager(_mockPlayer.Object);
            
            // Create item with specific weight
            var testItem = CreateItemWithWeight(testCase.TestWeight);
            
            // Act
            var result = inventoryManager.AddItem(testItem);
            
            // Assert
            result.Success.Should().Be(testCase.CanCarry,
                $"Strength {testCase.Strength} should {(testCase.CanCarry ? "" : "not ")}allow carrying {testCase.TestWeight} lbs");
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
            Weight = 15,
            Cost = 100,
            Values = new int[] { 2, 4, 2, 3 }
        };
    }

    private WorldObject CreateTestPotion()
    {
        return new WorldObject
        {
            VirtualNumber = 3002,
            Name = "potion healing red",
            ShortDescription = "a healing potion",
            LongDescription = "A red healing potion sits here.",
            ObjectType = ObjectType.POTION,
            Weight = 1,
            Cost = 50,
            Values = new int[] { 10, 201, -1, -1 } // Level 10, heal spell
        };
    }

    private WorldObject CreateTestContainer()
    {
        return new WorldObject
        {
            VirtualNumber = 3003,
            Name = "satchel leather bag",
            ShortDescription = "a leather satchel",
            LongDescription = "A leather satchel has been dropped here.",
            ObjectType = ObjectType.CONTAINER,
            Weight = 5,
            Cost = 25,
            Values = new int[] { 50, 0, 0, 0 }, // 50 lbs capacity
            Contents = new List<WorldObject>()
        };
    }

    private WorldObject CreateTestGold()
    {
        return new WorldObject
        {
            VirtualNumber = 0, // Special vnum for gold
            Name = "coins gold",
            ShortDescription = "a pile of gold coins",
            LongDescription = "A pile of gold coins sparkles here.",
            ObjectType = ObjectType.MONEY,
            Weight = 0, // Gold has no weight
            Cost = 0,
            Gold = 100
        };
    }

    private WorldObject CreateTestFood()
    {
        return new WorldObject
        {
            VirtualNumber = 3004,
            Name = "bread loaf",
            ShortDescription = "a loaf of bread",
            LongDescription = "A fresh loaf of bread lies here.",
            ObjectType = ObjectType.FOOD,
            Weight = 2,
            Cost = 5,
            Values = new int[] { 24, 0, 0, 0 } // 24 hours of fullness
        };
    }

    private WorldObject CreateHeavyItem()
    {
        return new WorldObject
        {
            VirtualNumber = 9998,
            Name = "anvil iron heavy",
            ShortDescription = "a heavy iron anvil",
            LongDescription = "A massive iron anvil sits here.",
            ObjectType = ObjectType.TRASH,
            Weight = 50,
            Cost = 500
        };
    }

    private WorldObject CreateLightItem(int vnum)
    {
        return new WorldObject
        {
            VirtualNumber = vnum,
            Name = $"item{vnum} light",
            ShortDescription = $"a light item{vnum}",
            LongDescription = $"A light item{vnum} is here.",
            ObjectType = ObjectType.TRASH,
            Weight = 1,
            Cost = 1
        };
    }

    private WorldObject CreateSmallContainer()
    {
        return new WorldObject
        {
            VirtualNumber = 3005,
            Name = "pouch small",
            ShortDescription = "a small pouch",
            LongDescription = "A small pouch has been dropped here.",
            ObjectType = ObjectType.CONTAINER,
            Weight = 1,
            Cost = 10,
            Values = new int[] { 5, 0, 0, 0 }, // 5 lbs capacity
            Contents = new List<WorldObject>()
        };
    }

    private WorldObject CreateLargeItem()
    {
        return new WorldObject
        {
            VirtualNumber = 3006,
            Name = "statue marble",
            ShortDescription = "a marble statue",
            LongDescription = "A beautiful marble statue stands here.",
            ObjectType = ObjectType.TRASH,
            Weight = 10,
            Cost = 1000
        };
    }

    private WorldObject CreateNoDropItem()
    {
        return new WorldObject
        {
            VirtualNumber = 3007,
            Name = "ring cursed",
            ShortDescription = "a cursed ring",
            LongDescription = "A cursed ring glows ominously here.",
            ObjectType = ObjectType.WORN,
            ExtraFlags = (1 << 2), // ITEM_NODROP
            Weight = 1,
            Cost = 0
        };
    }

    private WorldObject CreateGoldPile(int amount)
    {
        return new WorldObject
        {
            VirtualNumber = 0,
            Name = "coins gold",
            ShortDescription = $"{amount} gold coins",
            LongDescription = $"{amount} gold coins are scattered here.",
            ObjectType = ObjectType.MONEY,
            Weight = 0,
            Cost = 0,
            Gold = amount
        };
    }

    private WorldObject CreateItemWithWeight(int weight)
    {
        return new WorldObject
        {
            VirtualNumber = 9999,
            Name = "test weight item",
            ShortDescription = $"a {weight}-pound item",
            LongDescription = $"A {weight}-pound test item is here.",
            ObjectType = ObjectType.TRASH,
            Weight = weight,
            Cost = weight
        };
    }

    #endregion
}