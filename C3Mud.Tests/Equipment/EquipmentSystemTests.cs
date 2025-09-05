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
/// Core Equipment System Tests - All tests should FAIL initially
/// 
/// Based on original CircleMUD equipment mechanics:
/// - Equipment array: struct obj_data *equipment[MAX_WEAR] (18 slots)
/// - Wear flags validation: can_wear_on_eq()
/// - Class restrictions: invalid_class() checks
/// - Weight calculations: IS_CARRYING_W(ch)
/// - Stat applications: affect_modify() when equipping/removing
/// </summary>
public class EquipmentSystemTests
{
    private readonly Mock<IPlayer> _mockPlayer;
    private readonly Mock<IWorldDatabase> _mockWorldDatabase;
    private readonly Mock<IEquipmentManager> _mockEquipmentManager;
    
    // Test equipment instances based on original CircleMUD data
    private readonly WorldObject _testSword;
    private readonly WorldObject _testArmor;
    private readonly WorldObject _testShield;
    private readonly WorldObject _testHelmet;
    private readonly WorldObject _testRing;

    public EquipmentSystemTests()
    {
        _mockPlayer = new Mock<IPlayer>();
        _mockWorldDatabase = new Mock<IWorldDatabase>();
        _mockEquipmentManager = new Mock<IEquipmentManager>();
        
        // Setup basic player properties
        _mockPlayer.Setup(p => p.Name).Returns("TestWarrior");
        _mockPlayer.Setup(p => p.Level).Returns(10);
        _mockPlayer.Setup(p => p.Strength).Returns(18);
        _mockPlayer.Setup(p => p.Dexterity).Returns(16);
        _mockPlayer.Setup(p => p.Constitution).Returns(17);
        _mockPlayer.SetupProperty(p => p.ArmorClass, 10);
        
        // Create test equipment based on original CircleMUD objects
        _testSword = CreateTestWeapon();
        _testArmor = CreateTestArmor();
        _testShield = CreateTestShield();
        _testHelmet = CreateTestHelmet();
        _testRing = CreateTestRing();
    }

    #region Basic Equipment Operations

    [Fact]
    public void EquipItem_ValidWeapon_SuccessfullyEquipped()
    {
        // Arrange - This will fail until equipment system is implemented
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        
        // Act
        var result = equipmentManager.EquipItem(_testSword, EquipmentSlot.Wield);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("You wield a sharp longsword.");
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Wield).Should().Be(_testSword);
    }

    [Fact]
    public void EquipItem_AlreadyEquippedSlot_ReplacesExistingItem()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var oldWeapon = CreateTestDagger();
        
        // First equip old weapon
        equipmentManager.EquipItem(oldWeapon, EquipmentSlot.Wield);
        
        // Act - Try to equip new weapon in same slot
        var result = equipmentManager.EquipItem(_testSword, EquipmentSlot.Wield);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("You stop using a rusty dagger.\r\nYou wield a sharp longsword.");
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Wield).Should().Be(_testSword);
        _mockPlayer.Object.GetInventory().Should().Contain(oldWeapon);
    }

    [Fact]
    public void UnequipItem_EquippedWeapon_SuccessfullyRemoved()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        equipmentManager.EquipItem(_testSword, EquipmentSlot.Wield);
        
        // Act
        var result = equipmentManager.UnequipItem(EquipmentSlot.Wield);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("You stop using a sharp longsword.");
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Wield).Should().BeNull();
        _mockPlayer.Object.GetInventory().Should().Contain(_testSword);
    }

    [Fact]
    public void UnequipItem_EmptySlot_ReturnsFailure()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        
        // Act
        var result = equipmentManager.UnequipItem(EquipmentSlot.Wield);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("You aren't wielding anything.");
    }

    #endregion

    #region Equipment Slot Validation

    [Fact]
    public void EquipItem_WeaponInArmorSlot_ReturnsFailure()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        
        // Act - Try to wear weapon as body armor
        var result = equipmentManager.EquipItem(_testSword, EquipmentSlot.Body);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("You can't wear that on your body.");
    }

    [Fact]
    public void EquipItem_ArmorInWeaponSlot_ReturnsFailure()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        
        // Act - Try to wield armor as weapon
        var result = equipmentManager.EquipItem(_testArmor, EquipmentSlot.Wield);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("You can't wield that.");
    }

    [Theory]
    [InlineData(EquipmentSlot.FingerRight)]
    [InlineData(EquipmentSlot.FingerLeft)]
    public void EquipItem_RingInFingerSlot_SuccessfullyEquipped(EquipmentSlot slot)
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        
        // Act
        var result = equipmentManager.EquipItem(_testRing, slot);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be($"You slide a golden ring on your {GetFingerName(slot)} finger.");
        _mockPlayer.Object.GetEquippedItem(slot).Should().Be(_testRing);
    }

    [Fact]
    public void EquipItem_RingInNonFingerSlot_ReturnsFailure()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        
        // Act
        var result = equipmentManager.EquipItem(_testRing, EquipmentSlot.Head);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("You can't wear that on your head.");
    }

    #endregion

    #region Weight and Carrying Capacity

    [Fact]
    public void EquipItem_ExceedsCarryingCapacity_ReturnsFailure()
    {
        // Arrange - Player with very low strength
        _mockPlayer.Setup(p => p.Strength).Returns(3); // Very weak
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var heavyItem = CreateHeavyItem(); // 1000 lbs
        
        // Act
        var result = equipmentManager.EquipItem(heavyItem, EquipmentSlot.Body);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("That item is too heavy for you to wear.");
    }

    [Fact]
    public void GetCarryingCapacity_VariousStrengthLevels_MatchesOriginalFormula()
    {
        // Test data based on original CircleMUD str_app[] table
        var testCases = new[]
        {
            new { Strength = 3, ExpectedCapacity = 10 },
            new { Strength = 18, ExpectedCapacity = 200 },
            new { Strength = 25, ExpectedCapacity = 640 } // Exceptional strength
        };

        foreach (var testCase in testCases)
        {
            // Arrange
            _mockPlayer.Setup(p => p.Strength).Returns(testCase.Strength);
            var equipmentManager = new EquipmentManager(_mockPlayer.Object);
            
            // Act
            var capacity = equipmentManager.GetCarryingCapacity();
            
            // Assert
            capacity.Should().Be(testCase.ExpectedCapacity,
                $"Strength {testCase.Strength} should give capacity {testCase.ExpectedCapacity}");
        }
    }

    [Fact]
    public void GetCurrentWeight_WithEquippedItems_CalculatesCorrectly()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        equipmentManager.EquipItem(_testSword, EquipmentSlot.Wield);   // 15 lbs
        equipmentManager.EquipItem(_testArmor, EquipmentSlot.Body);    // 30 lbs
        equipmentManager.EquipItem(_testShield, EquipmentSlot.Shield); // 10 lbs
        
        // Act
        var totalWeight = equipmentManager.GetCurrentWeight();
        
        // Assert
        totalWeight.Should().Be(55); // 15 + 30 + 10
    }

    #endregion

    #region Equipment Statistics Integration

    [Fact]
    public void EquipArmor_IncreasesArmorClass_AppliesCorrectly()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var baseAC = _mockPlayer.Object.ArmorClass; // Should be 10
        
        // Act
        equipmentManager.EquipItem(_testArmor, EquipmentSlot.Body); // AC +5
        
        // Assert
        var newAC = _mockPlayer.Object.ArmorClass;
        newAC.Should().Be(baseAC - 5); // AC improves by 5 (lower is better)
    }

    [Fact]
    public void EquipWeapon_AffectsCombatStats_AppliesCorrectly()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var baseHitroll = GetPlayerHitroll(_mockPlayer.Object);
        var baseDamroll = GetPlayerDamroll(_mockPlayer.Object);
        
        // Act
        equipmentManager.EquipItem(_testSword, EquipmentSlot.Wield); // +2 hit, +3 dam
        
        // Assert
        var newHitroll = GetPlayerHitroll(_mockPlayer.Object);
        var newDamroll = GetPlayerDamroll(_mockPlayer.Object);
        
        newHitroll.Should().Be(baseHitroll + 2);
        newDamroll.Should().Be(baseDamroll + 3);
    }

    [Fact]
    public void UnequipItem_RemovesStatBonuses_CorrectlyReverted()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var originalAC = _mockPlayer.Object.ArmorClass;
        
        equipmentManager.EquipItem(_testArmor, EquipmentSlot.Body); // AC +5
        var equippedAC = _mockPlayer.Object.ArmorClass;
        
        // Act
        equipmentManager.UnequipItem(EquipmentSlot.Body);
        
        // Assert
        var finalAC = _mockPlayer.Object.ArmorClass;
        finalAC.Should().Be(originalAC, "AC should return to original value after unequipping");
    }

    [Fact]
    public void EquipMultipleItems_CumulativeStatBonuses_ApplyCorrectly()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var baseAC = _mockPlayer.Object.ArmorClass;
        
        // Act - Equip multiple AC-boosting items
        equipmentManager.EquipItem(_testArmor, EquipmentSlot.Body);    // AC +5
        equipmentManager.EquipItem(_testHelmet, EquipmentSlot.Head);   // AC +2
        equipmentManager.EquipItem(_testShield, EquipmentSlot.Shield); // AC +3
        
        // Assert
        var finalAC = _mockPlayer.Object.ArmorClass;
        finalAC.Should().Be(baseAC - 10); // Total AC improvement of 10
    }

    #endregion

    #region Equipment Restrictions and Class Checks

    [Fact]
    public void EquipItem_ClassRestricted_ReturnsFailure()
    {
        // Arrange - Mage trying to wear plate mail
        _mockPlayer.Setup(p => p.GetCharacterClass()).Returns(CharacterClass.Mage);
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var plateMail = CreateClassRestrictedArmor(); // Anti-mage
        
        // Act
        var result = equipmentManager.EquipItem(plateMail, EquipmentSlot.Body);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("You are forbidden to use that item.");
    }

    [Fact]
    public void EquipItem_LevelTooLow_ReturnsFailure()
    {
        // Arrange - Low level player trying to use high level item
        _mockPlayer.Setup(p => p.Level).Returns(5);
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var highLevelSword = CreateHighLevelWeapon(); // Min level 20
        
        // Act
        var result = equipmentManager.EquipItem(highLevelSword, EquipmentSlot.Wield);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("You are not experienced enough to use that item.");
    }

    [Fact]
    public void EquipItem_AlignmentRestricted_ReturnsFailure()
    {
        // Arrange - Good player trying to use evil item
        _mockPlayer.Setup(p => p.GetAlignment()).Returns(Alignment.Good);
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var evilWeapon = CreateAlignmentRestrictedWeapon(); // Anti-good
        
        // Act
        var result = equipmentManager.EquipItem(evilWeapon, EquipmentSlot.Wield);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("You are forbidden to use that item.");
    }

    #endregion

    #region Shield and Two-Handed Weapon Conflicts

    [Fact]
    public void EquipShield_WithTwoHandedWeapon_ReturnsFailure()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var twoHandedSword = CreateTwoHandedWeapon();
        equipmentManager.EquipItem(twoHandedSword, EquipmentSlot.Wield);
        
        // Act
        var result = equipmentManager.EquipItem(_testShield, EquipmentSlot.Shield);
        
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("You can't use a shield while wielding a two-handed weapon.");
    }

    [Fact]
    public void EquipTwoHandedWeapon_WithShield_RemovesShield()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        equipmentManager.EquipItem(_testShield, EquipmentSlot.Shield);
        
        // Act
        var twoHandedSword = CreateTwoHandedWeapon();
        var result = equipmentManager.EquipItem(twoHandedSword, EquipmentSlot.Wield);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("You stop using a wooden shield.");
        result.Message.Should().Contain("You wield a massive two-handed sword.");
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Shield).Should().BeNull();
        _mockPlayer.Object.GetEquippedItem(EquipmentSlot.Wield).Should().Be(twoHandedSword);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void EquipItem_PerformanceTest_CompletesWithinTimeLimit()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act - Perform 1000 equip operations
        for (int i = 0; i < 1000; i++)
        {
            equipmentManager.EquipItem(_testSword, EquipmentSlot.Wield);
            equipmentManager.UnequipItem(EquipmentSlot.Wield);
        }
        
        stopwatch.Stop();
        
        // Assert - Should complete well under 1 second
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    #endregion

    #region Legacy Compatibility Tests

    [Fact]
    public void AllEquipmentSlots_MatchOriginalCircleMudPositions()
    {
        // Original CircleMUD positions (from structs.h)
        var originalPositions = new Dictionary<EquipmentSlot, int>
        {
            { EquipmentSlot.Light, 0 },
            { EquipmentSlot.FingerRight, 1 },
            { EquipmentSlot.FingerLeft, 2 },
            { EquipmentSlot.Neck1, 3 },
            { EquipmentSlot.Neck2, 4 },
            { EquipmentSlot.Body, 5 },
            { EquipmentSlot.Head, 6 },
            { EquipmentSlot.Legs, 7 },
            { EquipmentSlot.Feet, 8 },
            { EquipmentSlot.Hands, 9 },
            { EquipmentSlot.Arms, 10 },
            { EquipmentSlot.Shield, 11 },
            { EquipmentSlot.About, 12 },
            { EquipmentSlot.Waist, 13 },
            { EquipmentSlot.WristRight, 14 },
            { EquipmentSlot.WristLeft, 15 },
            { EquipmentSlot.Wield, 16 },
            { EquipmentSlot.Hold, 17 }
        };

        foreach (var kvp in originalPositions)
        {
            // Assert - Enum values must match original positions exactly
            ((int)kvp.Key).Should().Be(kvp.Value, 
                $"Equipment slot {kvp.Key} must match original CircleMUD position {kvp.Value}");
        }
    }

    [Fact]
    public void WearFlagsValidation_MatchesOriginalCircleMudLogic()
    {
        // Test cases based on original can_wear_on_eq() function
        var testCases = new[]
        {
            new { Item = _testSword, Slot = EquipmentSlot.Wield, CanWear = true },
            new { Item = _testSword, Slot = EquipmentSlot.Body, CanWear = false },
            new { Item = _testArmor, Slot = EquipmentSlot.Body, CanWear = true },
            new { Item = _testArmor, Slot = EquipmentSlot.Wield, CanWear = false },
            new { Item = _testShield, Slot = EquipmentSlot.Shield, CanWear = true },
            new { Item = _testShield, Slot = EquipmentSlot.Head, CanWear = false }
        };

        foreach (var testCase in testCases)
        {
            // Act
            var canWear = EquipmentSlotValidator.CanWearInSlot(testCase.Item, testCase.Slot);
            
            // Assert
            canWear.Should().Be(testCase.CanWear,
                $"Item {testCase.Item.ShortDescription} should {(testCase.CanWear ? "" : "not ")}be wearable in slot {testCase.Slot}");
        }
    }

    #endregion

    #region Helper Methods

    private WorldObject CreateTestWeapon()
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
            Values = new int[] { 2, 4, 2, 3 }, // 2d4+2 slashing weapon
            Applies = new Dictionary<int, int>
            {
                { 18, 2 }, // APPLY_HITROLL +2
                { 19, 3 }  // APPLY_DAMROLL +3
            }
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
            Values = new int[] { 5, 0, 0, 0 }, // AC +5
            Applies = new Dictionary<int, int>()
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
            Values = new int[] { 3, 0, 0, 0 }, // AC +3
            Applies = new Dictionary<int, int>()
        };
    }

    private WorldObject CreateTestHelmet()
    {
        return new WorldObject
        {
            VirtualNumber = 3004,
            Name = "helmet iron",
            ShortDescription = "an iron helmet",
            LongDescription = "An iron helmet has been left here.",
            ObjectType = ObjectType.ARMOR,
            WearFlags = (1 << 6), // ITEM_WEAR_HEAD
            Weight = 8,
            Values = new int[] { 2, 0, 0, 0 }, // AC +2
            Applies = new Dictionary<int, int>()
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
            Values = new int[] { 0, 0, 0, 0 },
            Applies = new Dictionary<int, int>
            {
                { 13, 1 } // APPLY_STR +1
            }
        };
    }

    private WorldObject CreateTestDagger()
    {
        return new WorldObject
        {
            VirtualNumber = 3006,
            Name = "dagger rusty",
            ShortDescription = "a rusty dagger",
            LongDescription = "A rusty dagger lies here.",
            ObjectType = ObjectType.WEAPON,
            WearFlags = (1 << 16), // ITEM_WEAR_WIELD
            Weight = 5,
            Values = new int[] { 1, 4, 0, 11 }, // 1d4+0 piercing weapon
            Applies = new Dictionary<int, int>()
        };
    }

    private WorldObject CreateHeavyItem()
    {
        return new WorldObject
        {
            VirtualNumber = 9999,
            Name = "boulder massive",
            ShortDescription = "a massive boulder",
            LongDescription = "A massive boulder sits here.",
            ObjectType = ObjectType.ARMOR,
            WearFlags = (1 << 5), // ITEM_WEAR_BODY
            Weight = 1000, // Extremely heavy
            Values = new int[] { 10, 0, 0, 0 },
            Applies = new Dictionary<int, int>()
        };
    }

    private WorldObject CreateClassRestrictedArmor()
    {
        return new WorldObject
        {
            VirtualNumber = 4001,
            Name = "platemail plate mail",
            ShortDescription = "a suit of plate mail",
            LongDescription = "A gleaming suit of plate mail lies here.",
            ObjectType = ObjectType.ARMOR,
            WearFlags = (1 << 5), // ITEM_WEAR_BODY
            ExtraFlags = (1 << 7), // ITEM_ANTI_MAGIC_USER
            Weight = 50,
            Values = new int[] { 8, 0, 0, 0 },
            Applies = new Dictionary<int, int>()
        };
    }

    private WorldObject CreateHighLevelWeapon()
    {
        return new WorldObject
        {
            VirtualNumber = 5001,
            Name = "excalibur legendary sword",
            ShortDescription = "the legendary Excalibur",
            LongDescription = "The legendary sword Excalibur gleams with power.",
            ObjectType = ObjectType.WEAPON,
            WearFlags = (1 << 16), // ITEM_WEAR_WIELD
            ExtraFlags = (1 << 4), // ITEM_MAGIC
            Weight = 20,
            Values = new int[] { 3, 6, 5, 3 }, // 3d6+5 slashing weapon
            Cost = 50000, // High cost indicates high level requirement
            Applies = new Dictionary<int, int>
            {
                { 18, 5 }, // APPLY_HITROLL +5
                { 19, 8 }  // APPLY_DAMROLL +8
            }
        };
    }

    private WorldObject CreateAlignmentRestrictedWeapon()
    {
        return new WorldObject
        {
            VirtualNumber = 6001,
            Name = "sword evil dark",
            ShortDescription = "a dark evil sword",
            LongDescription = "A dark sword radiates evil power.",
            ObjectType = ObjectType.WEAPON,
            WearFlags = (1 << 16), // ITEM_WEAR_WIELD
            ExtraFlags = (1 << 8), // ITEM_ANTI_GOOD
            Weight = 12,
            Values = new int[] { 2, 6, 3, 3 }, // 2d6+3 slashing weapon
            Applies = new Dictionary<int, int>
            {
                { 18, 3 }, // APPLY_HITROLL +3
                { 19, 4 }  // APPLY_DAMROLL +4
            }
        };
    }

    private WorldObject CreateTwoHandedWeapon()
    {
        return new WorldObject
        {
            VirtualNumber = 7001,
            Name = "sword two-handed massive",
            ShortDescription = "a massive two-handed sword",
            LongDescription = "A massive two-handed sword lies here.",
            ObjectType = ObjectType.WEAPON,
            WearFlags = (1 << 16), // ITEM_WEAR_WIELD
            ExtraFlags = (1 << 17), // ITEM_TWO_HANDED (custom flag)
            Weight = 25,
            Values = new int[] { 3, 8, 4, 3 }, // 3d8+4 slashing weapon
            Applies = new Dictionary<int, int>
            {
                { 18, 4 }, // APPLY_HITROLL +4
                { 19, 6 }  // APPLY_DAMROLL +6
            }
        };
    }

    private string GetFingerName(EquipmentSlot slot)
    {
        return slot == EquipmentSlot.FingerRight ? "right" : "left";
    }

    private int GetPlayerHitroll(IPlayer player)
    {
        // TODO: This will be implemented with proper stat system
        return 0;
    }

    private int GetPlayerDamroll(IPlayer player)
    {
        // TODO: This will be implemented with proper stat system
        return 0;
    }

    #endregion
}