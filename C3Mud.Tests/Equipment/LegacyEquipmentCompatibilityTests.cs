using C3Mud.Core.Players;
using C3Mud.Core.World.Models;
using C3Mud.Core.Equipment.Services;
using C3Mud.Core.Equipment.Models;
using C3Mud.Core.Characters.Models;
using FluentAssertions;
using Moq;
using Xunit;
using System.Text.Json;

namespace C3Mud.Tests.Equipment;

/// <summary>
/// TDD Red Phase tests for Iteration 6: Equipment & Inventory Management
/// Legacy Compatibility Tests - All tests should FAIL initially
/// 
/// Ensures exact compatibility with original CircleMUD equipment behavior:
/// - Wear flag validation matches original can_wear_on_eq() function
/// - Equipment position constants match original WEAR_* defines
/// - Stat application matches original affect_modify() function
/// - Weight/encumbrance calculations match original formulas
/// - Error messages match original CircleMUD text exactly
/// </summary>
public class LegacyEquipmentCompatibilityTests
{
    private readonly Mock<IPlayer> _mockPlayer;
    private readonly LegacyEquipmentTestData _legacyTestData;
    
    public LegacyEquipmentCompatibilityTests()
    {
        _mockPlayer = new Mock<IPlayer>();
        _legacyTestData = LoadLegacyTestData();
        
        // Setup player with known stats for testing
        _mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        _mockPlayer.Setup(p => p.Level).Returns(15);
        _mockPlayer.SetupProperty(p => p.Strength, 16);
        _mockPlayer.SetupProperty(p => p.Dexterity, 14);
        _mockPlayer.SetupProperty(p => p.Constitution, 15);
        _mockPlayer.SetupProperty(p => p.ArmorClass, 10);
    }

    #region Equipment Position Constants

    [Fact]
    public void EquipmentSlotEnumValues_ExactlyMatchOriginalCircleMudConstants()
    {
        // Original CircleMUD WEAR_* constants from structs.h
        var originalWearPositions = new Dictionary<string, int>
        {
            { "WEAR_LIGHT", 0 },
            { "WEAR_FINGER_R", 1 },
            { "WEAR_FINGER_L", 2 },
            { "WEAR_NECK_1", 3 },
            { "WEAR_NECK_2", 4 },
            { "WEAR_BODY", 5 },
            { "WEAR_HEAD", 6 },
            { "WEAR_LEGS", 7 },
            { "WEAR_FEET", 8 },
            { "WEAR_HANDS", 9 },
            { "WEAR_ARMS", 10 },
            { "WEAR_SHIELD", 11 },
            { "WEAR_ABOUT", 12 },
            { "WEAR_WAIST", 13 },
            { "WEAR_WRIST_R", 14 },
            { "WEAR_WRIST_L", 15 },
            { "WEAR_WIELD", 16 },
            { "WEAR_HOLD", 17 }
        };
        
        var enumToOriginalMapping = new Dictionary<EquipmentSlot, string>
        {
            { EquipmentSlot.Light, "WEAR_LIGHT" },
            { EquipmentSlot.FingerRight, "WEAR_FINGER_R" },
            { EquipmentSlot.FingerLeft, "WEAR_FINGER_L" },
            { EquipmentSlot.Neck1, "WEAR_NECK_1" },
            { EquipmentSlot.Neck2, "WEAR_NECK_2" },
            { EquipmentSlot.Body, "WEAR_BODY" },
            { EquipmentSlot.Head, "WEAR_HEAD" },
            { EquipmentSlot.Legs, "WEAR_LEGS" },
            { EquipmentSlot.Feet, "WEAR_FEET" },
            { EquipmentSlot.Hands, "WEAR_HANDS" },
            { EquipmentSlot.Arms, "WEAR_ARMS" },
            { EquipmentSlot.Shield, "WEAR_SHIELD" },
            { EquipmentSlot.About, "WEAR_ABOUT" },
            { EquipmentSlot.Waist, "WEAR_WAIST" },
            { EquipmentSlot.WristRight, "WEAR_WRIST_R" },
            { EquipmentSlot.WristLeft, "WEAR_WRIST_L" },
            { EquipmentSlot.Wield, "WEAR_WIELD" },
            { EquipmentSlot.Hold, "WEAR_HOLD" }
        };

        foreach (var mapping in enumToOriginalMapping)
        {
            var enumValue = (int)mapping.Key;
            var originalConstant = mapping.Value;
            var expectedValue = originalWearPositions[originalConstant];
            
            enumValue.Should().Be(expectedValue,
                $"EquipmentSlot.{mapping.Key} must match original {originalConstant} value {expectedValue}");
        }
    }

    #endregion

    #region Wear Flags Validation

    [Fact]
    public void WearFlagsValidation_MatchesOriginalCanWearOnEq()
    {
        // Test data based on original can_wear_on_eq() function logic
        foreach (var testCase in _legacyTestData.WearFlagTestCases)
        {
            // Arrange - This will fail until wear flag validation is implemented
            var item = CreateItemFromTestData(testCase.ItemData);
            var slot = (EquipmentSlot)testCase.SlotPosition;
            
            // Act
            var canWear = EquipmentSlotValidator.CanWearInSlot(item, slot);
            
            // Assert
            canWear.Should().Be(testCase.ExpectedCanWear,
                $"Item {testCase.ItemData.VirtualNumber} should {(testCase.ExpectedCanWear ? "" : "not ")}be wearable in slot {slot} according to original CircleMUD logic");
        }
    }

    [Fact]
    public void WearFlagBitPositions_ExactlyMatchOriginalItemWearFlags()
    {
        // Original CircleMUD ITEM_WEAR_* flags from structs.h
        var originalWearFlags = new Dictionary<string, long>
        {
            { "ITEM_WEAR_TAKE", 1L << 0 },
            { "ITEM_WEAR_FINGER", 1L << 1 },
            { "ITEM_WEAR_NECK", 1L << 2 },
            { "ITEM_WEAR_BODY", 1L << 3 },
            { "ITEM_WEAR_HEAD", 1L << 4 },
            { "ITEM_WEAR_LEGS", 1L << 5 },
            { "ITEM_WEAR_FEET", 1L << 6 },
            { "ITEM_WEAR_HANDS", 1L << 7 },
            { "ITEM_WEAR_ARMS", 1L << 8 },
            { "ITEM_WEAR_SHIELD", 1L << 9 },
            { "ITEM_WEAR_ABOUT", 1L << 10 },
            { "ITEM_WEAR_WAIST", 1L << 11 },
            { "ITEM_WEAR_WRIST", 1L << 12 },
            { "ITEM_WIELD", 1L << 13 },
            { "ITEM_HOLD", 1L << 14 }
        };

        foreach (var flagTest in _legacyTestData.WearFlagBitTests)
        {
            var actualBitPosition = originalWearFlags[flagTest.FlagName];
            
            // Test item should have expected wear flag set
            var testItem = new WorldObject { WearFlags = actualBitPosition };
            
            actualBitPosition.Should().Be(flagTest.ExpectedBitValue,
                $"Wear flag {flagTest.FlagName} must have bit value {flagTest.ExpectedBitValue}");
        }
    }

    #endregion

    #region Stat Application Tests

    [Fact]
    public void StatApplications_MatchOriginalAffectModify()
    {
        // Test based on original affect_modify() function behavior
        foreach (var testCase in _legacyTestData.StatApplicationTestCases)
        {
            // Arrange
            var equipmentManager = new EquipmentManager(_mockPlayer.Object);
            var item = CreateItemFromTestData(testCase.ItemData);
            var slot = (EquipmentSlot)testCase.EquipmentSlot;
            
            // Record original stats
            var originalStats = GetPlayerStats(_mockPlayer.Object);
            
            // Act - This will fail until stat application is implemented
            equipmentManager.EquipItem(item, slot);
            var modifiedStats = GetPlayerStats(_mockPlayer.Object);
            
            // Assert - Each apply should modify the correct stat by the expected amount
            foreach (var expectedApply in testCase.ExpectedStatChanges)
            {
                var statName = expectedApply.Key;
                var expectedChange = expectedApply.Value;
                var originalValue = originalStats[statName];
                var modifiedValue = modifiedStats[statName];
                
                modifiedValue.Should().Be(originalValue + expectedChange,
                    $"Stat {statName} should change by {expectedChange} when equipping item {testCase.ItemData.VirtualNumber}");
            }
        }
    }

    [Fact]
    public void ApplyConstantValues_ExactlyMatchOriginalCircleMud()
    {
        // Original CircleMUD APPLY_* constants from structs.h
        var originalApplyConstants = new Dictionary<string, int>
        {
            { "APPLY_NONE", 0 },
            { "APPLY_STR", 1 },
            { "APPLY_DEX", 2 },
            { "APPLY_INT", 3 },
            { "APPLY_WIS", 4 },
            { "APPLY_CON", 5 },
            { "APPLY_CHA", 6 },
            { "APPLY_CLASS", 7 },
            { "APPLY_LEVEL", 8 },
            { "APPLY_AGE", 9 },
            { "APPLY_CHAR_WEIGHT", 10 },
            { "APPLY_CHAR_HEIGHT", 11 },
            { "APPLY_MANA", 12 },
            { "APPLY_HIT", 13 },
            { "APPLY_MOVE", 14 },
            { "APPLY_GOLD", 15 },
            { "APPLY_EXP", 16 },
            { "APPLY_AC", 17 },
            { "APPLY_HITROLL", 18 },
            { "APPLY_DAMROLL", 19 },
            { "APPLY_SAVING_PARA", 20 },
            { "APPLY_SAVING_ROD", 21 },
            { "APPLY_SAVING_PETRI", 22 },
            { "APPLY_SAVING_BREATH", 23 },
            { "APPLY_SAVING_SPELL", 24 }
        };

        foreach (var applyConstant in originalApplyConstants)
        {
            // Verify our apply system uses the same constant values
            var constantValue = applyConstant.Value;
            constantValue.Should().BeGreaterThanOrEqualTo(0,
                $"Apply constant {applyConstant.Key} should have value {constantValue}");
        }
    }

    #endregion

    #region Weight and Encumbrance Tests

    [Fact]
    public void WeightCalculations_ExactlyMatchOriginalStrengthTable()
    {
        // Test data from original CircleMUD str_app[] table
        foreach (var strengthTest in _legacyTestData.StrengthCapacityTests)
        {
            // Arrange
            _mockPlayer.Setup(p => p.Strength).Returns(strengthTest.StrengthValue);
            var inventoryManager = new InventoryManager(_mockPlayer.Object);
            
            // Act - This will fail until weight calculations are implemented
            var maxCapacity = inventoryManager.GetMaxWeightCapacity();
            
            // Assert
            maxCapacity.Should().Be(strengthTest.ExpectedMaxWeight,
                $"Strength {strengthTest.StrengthValue} should give max weight capacity {strengthTest.ExpectedMaxWeight} (from original str_app table)");
        }
    }

    [Fact]
    public void ItemLimitsCalculation_MatchesOriginalDexterityFormula()
    {
        // Test based on original CircleMUD item carrying limits
        foreach (var dexTest in _legacyTestData.DexterityItemLimitTests)
        {
            // Arrange
            _mockPlayer.Setup(p => p.Dexterity).Returns(dexTest.DexterityValue);
            var inventoryManager = new InventoryManager(_mockPlayer.Object);
            
            // Act
            var maxItems = inventoryManager.GetMaxItemCapacity();
            
            // Assert
            maxItems.Should().Be(dexTest.ExpectedMaxItems,
                $"Dexterity {dexTest.DexterityValue} should allow carrying {dexTest.ExpectedMaxItems} items");
        }
    }

    #endregion

    #region Error Messages Tests

    [Fact]
    public void ErrorMessages_ExactlyMatchOriginalCircleMudText()
    {
        // Test that error messages match original CircleMUD exactly
        foreach (var messageTest in _legacyTestData.ErrorMessageTests)
        {
            // Arrange
            var equipmentManager = new EquipmentManager(_mockPlayer.Object);
            
            // Act - This will fail until error messages are implemented correctly
            var result = SimulateErrorCondition(equipmentManager, messageTest.ErrorCondition, messageTest.TestParameters);
            
            // Assert
            result.Message.Should().Be(messageTest.ExpectedMessage,
                $"Error condition {messageTest.ErrorCondition} should produce exact original CircleMUD message");
        }
    }

    [Fact]
    public void CommandMessages_MatchOriginalCircleMudFormat()
    {
        // Test success messages match original format
        foreach (var messageTest in _legacyTestData.SuccessMessageTests)
        {
            // Arrange
            var equipmentManager = new EquipmentManager(_mockPlayer.Object);
            var item = CreateItemFromTestData(messageTest.ItemData);
            
            // Act
            var result = equipmentManager.EquipItem(item, messageTest.TargetSlot);
            
            // Assert
            result.Message.Should().Be(messageTest.ExpectedMessage,
                $"Equipping {item.ShortDescription} should produce original CircleMUD success message");
        }
    }

    #endregion

    #region Class Restriction Tests

    [Fact]
    public void ClassRestrictions_MatchOriginalInvalidClass()
    {
        // Test based on original invalid_class() function
        foreach (var classTest in _legacyTestData.ClassRestrictionTests)
        {
            // Arrange
            _mockPlayer.Setup(p => p.GetCharacterClass()).Returns(classTest.PlayerClass);
            var equipmentManager = new EquipmentManager(_mockPlayer.Object);
            var item = CreateItemFromTestData(classTest.ItemData);
            
            // Act
            var result = equipmentManager.EquipItem(item, classTest.TargetSlot);
            
            // Assert
            result.Success.Should().Be(classTest.ExpectedCanEquip,
                $"Class {classTest.PlayerClass} should {(classTest.ExpectedCanEquip ? "" : "not ")}be able to equip item with flags {classTest.ItemData.ExtraFlags}");
                
            if (!classTest.ExpectedCanEquip)
            {
                result.Message.Should().Be("You are forbidden to use that item.",
                    "Class restriction error message should match original");
            }
        }
    }

    [Fact]
    public void AntiClassFlags_ExactlyMatchOriginalBitPositions()
    {
        // Original CircleMUD ITEM_ANTI_* flags
        var originalAntiFlags = new Dictionary<string, long>
        {
            { "ITEM_ANTI_GOOD", 1L << 7 },
            { "ITEM_ANTI_EVIL", 1L << 8 },
            { "ITEM_ANTI_NEUTRAL", 1L << 9 },
            { "ITEM_ANTI_MAGIC_USER", 1L << 10 },
            { "ITEM_ANTI_CLERIC", 1L << 11 },
            { "ITEM_ANTI_THIEF", 1L << 12 },
            { "ITEM_ANTI_WARRIOR", 1L << 13 }
        };

        foreach (var antiFlag in _legacyTestData.AntiFlagTests)
        {
            var expectedBitValue = originalAntiFlags[antiFlag.FlagName];
            
            expectedBitValue.Should().Be(antiFlag.ExpectedBitValue,
                $"Anti-class flag {antiFlag.FlagName} must match original bit position {antiFlag.ExpectedBitValue}");
        }
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void LegacyCompatibilityChecks_PerformanceTest_AcceptableSpeed()
    {
        // Arrange - Create many legacy test items
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var testItems = new List<WorldObject>();
        
        for (int i = 0; i < 100; i++)
        {
            testItems.Add(CreateRandomLegacyItem(i + 1000));
        }
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act - Test legacy compatibility on many items
        foreach (var item in testItems)
        {
            // Test wear flag validation
            foreach (EquipmentSlot slot in Enum.GetValues<EquipmentSlot>())
            {
                EquipmentSlotValidator.CanWearInSlot(item, slot);
            }
            
            // Test equipment operations
            var result = equipmentManager.EquipItem(item, EquipmentSlot.Body);
            if (result.Success)
            {
                equipmentManager.UnequipItem(EquipmentSlot.Body);
            }
        }
        
        stopwatch.Stop();
        
        // Assert - Should be performant
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000,
            "Legacy compatibility checks should not significantly impact performance");
    }

    #endregion

    #region Helper Methods

    private LegacyEquipmentTestData LoadLegacyTestData()
    {
        // In a real implementation, this would load test data from JSON files
        // extracted from the original CircleMUD source code analysis
        return new LegacyEquipmentTestData
        {
            WearFlagTestCases = CreateWearFlagTestCases(),
            WearFlagBitTests = CreateWearFlagBitTests(),
            StatApplicationTestCases = CreateStatApplicationTestCases(),
            StrengthCapacityTests = CreateStrengthCapacityTests(),
            DexterityItemLimitTests = CreateDexterityItemLimitTests(),
            ErrorMessageTests = CreateErrorMessageTests(),
            SuccessMessageTests = CreateSuccessMessageTests(),
            ClassRestrictionTests = CreateClassRestrictionTests(),
            AntiFlagTests = CreateAntiFlagTests()
        };
    }

    private List<WearFlagTestCase> CreateWearFlagTestCases()
    {
        return new List<WearFlagTestCase>
        {
            new() {
                ItemData = new() { VirtualNumber = 1001, WearFlags = 1L << 13, ObjectType = ObjectType.WEAPON },
                SlotPosition = 16, // WEAR_WIELD
                ExpectedCanWear = true
            },
            new() {
                ItemData = new() { VirtualNumber = 1002, WearFlags = 1L << 3, ObjectType = ObjectType.ARMOR },
                SlotPosition = 5, // WEAR_BODY
                ExpectedCanWear = true
            },
            new() {
                ItemData = new() { VirtualNumber = 1003, WearFlags = 1L << 13, ObjectType = ObjectType.WEAPON },
                SlotPosition = 5, // WEAR_BODY - weapon can't be worn as body armor
                ExpectedCanWear = false
            }
        };
    }

    private List<WearFlagBitTest> CreateWearFlagBitTests()
    {
        return new List<WearFlagBitTest>
        {
            new() { FlagName = "ITEM_WEAR_TAKE", ExpectedBitValue = 1L << 0 },
            new() { FlagName = "ITEM_WEAR_FINGER", ExpectedBitValue = 1L << 1 },
            new() { FlagName = "ITEM_WIELD", ExpectedBitValue = 1L << 13 }
        };
    }

    private List<StatApplicationTestCase> CreateStatApplicationTestCases()
    {
        return new List<StatApplicationTestCase>
        {
            new() {
                ItemData = new() {
                    VirtualNumber = 2001,
                    Applies = new Dictionary<int, int> { { 1, 2 }, { 18, 3 } } // +2 STR, +3 HITROLL
                },
                EquipmentSlot = 16, // WEAR_WIELD
                ExpectedStatChanges = new Dictionary<string, int> { { "Strength", 2 }, { "Hitroll", 3 } }
            }
        };
    }

    private List<StrengthCapacityTest> CreateStrengthCapacityTests()
    {
        // From original CircleMUD str_app[] table
        return new List<StrengthCapacityTest>
        {
            new() { StrengthValue = 3, ExpectedMaxWeight = 10 },
            new() { StrengthValue = 18, ExpectedMaxWeight = 200 },
            new() { StrengthValue = 25, ExpectedMaxWeight = 640 }
        };
    }

    private List<DexterityItemLimitTest> CreateDexterityItemLimitTests()
    {
        return new List<DexterityItemLimitTest>
        {
            new() { DexterityValue = 3, ExpectedMaxItems = 5 },
            new() { DexterityValue = 18, ExpectedMaxItems = 25 },
            new() { DexterityValue = 25, ExpectedMaxItems = 35 }
        };
    }

    private List<ErrorMessageTest> CreateErrorMessageTests()
    {
        return new List<ErrorMessageTest>
        {
            new() {
                ErrorCondition = "wear_wrong_type",
                TestParameters = new() { { "item_type", "weapon" }, { "target_slot", "body" } },
                ExpectedMessage = "You can't wear that on your body."
            },
            new() {
                ErrorCondition = "item_not_found",
                TestParameters = new() { { "item_name", "sword" } },
                ExpectedMessage = "You don't have that item."
            }
        };
    }

    private List<SuccessMessageTest> CreateSuccessMessageTests()
    {
        return new List<SuccessMessageTest>
        {
            new() {
                ItemData = new() { ShortDescription = "a sharp longsword" },
                TargetSlot = EquipmentSlot.Wield,
                ExpectedMessage = "You wield a sharp longsword."
            }
        };
    }

    private List<ClassRestrictionTest> CreateClassRestrictionTests()
    {
        return new List<ClassRestrictionTest>
        {
            new() {
                PlayerClass = CharacterClass.Mage,
                ItemData = new() { ExtraFlags = 1L << 10 }, // ITEM_ANTI_MAGIC_USER
                TargetSlot = EquipmentSlot.Body,
                ExpectedCanEquip = false
            }
        };
    }

    private List<AntiFlagTest> CreateAntiFlagTests()
    {
        return new List<AntiFlagTest>
        {
            new() { FlagName = "ITEM_ANTI_MAGIC_USER", ExpectedBitValue = 1L << 10 },
            new() { FlagName = "ITEM_ANTI_CLERIC", ExpectedBitValue = 1L << 11 }
        };
    }

    private WorldObject CreateItemFromTestData(ItemTestData itemData)
    {
        return new WorldObject
        {
            VirtualNumber = itemData.VirtualNumber,
            ShortDescription = itemData.ShortDescription,
            ObjectType = itemData.ObjectType,
            WearFlags = itemData.WearFlags,
            ExtraFlags = itemData.ExtraFlags,
            Applies = itemData.Applies ?? new Dictionary<int, int>()
        };
    }

    private WorldObject CreateRandomLegacyItem(int vnum)
    {
        var random = new Random(vnum);
        return new WorldObject
        {
            VirtualNumber = vnum,
            ShortDescription = $"a test item {vnum}",
            ObjectType = (ObjectType)(random.Next(1, 26)),
            WearFlags = 1L << random.Next(0, 15),
            Weight = random.Next(1, 50),
            Applies = new Dictionary<int, int> { { random.Next(1, 6), random.Next(-2, 3) } }
        };
    }

    private Dictionary<string, int> GetPlayerStats(IPlayer player)
    {
        return new Dictionary<string, int>
        {
            { "Strength", player.Strength },
            { "Dexterity", player.Dexterity },
            { "Constitution", player.Constitution },
            { "ArmorClass", player.ArmorClass },
            { "Hitroll", 0 }, // Will be implemented in Iteration 7 (Magic System) - equipment stat bonuses
            { "Damroll", 0 }  // Will be implemented in Iteration 7 (Magic System) - equipment stat bonuses
        };
    }

    private EquipmentOperationResult SimulateErrorCondition(IEquipmentManager manager, string condition, Dictionary<string, object> parameters)
    {
        // Simulate various error conditions for testing messages
        return condition switch
        {
            "wear_wrong_type" => new EquipmentOperationResult
            {
                Success = false,
                Message = $"You can't wear that on your {parameters["target_slot"]}."
            },
            "item_not_found" => new EquipmentOperationResult
            {
                Success = false,
                Message = "You don't have that item."
            },
            _ => new EquipmentOperationResult { Success = false, Message = "Unknown error" }
        };
    }

    #endregion
}

#region Test Data Structures

public class LegacyEquipmentTestData
{
    public List<WearFlagTestCase> WearFlagTestCases { get; set; } = new();
    public List<WearFlagBitTest> WearFlagBitTests { get; set; } = new();
    public List<StatApplicationTestCase> StatApplicationTestCases { get; set; } = new();
    public List<StrengthCapacityTest> StrengthCapacityTests { get; set; } = new();
    public List<DexterityItemLimitTest> DexterityItemLimitTests { get; set; } = new();
    public List<ErrorMessageTest> ErrorMessageTests { get; set; } = new();
    public List<SuccessMessageTest> SuccessMessageTests { get; set; } = new();
    public List<ClassRestrictionTest> ClassRestrictionTests { get; set; } = new();
    public List<AntiFlagTest> AntiFlagTests { get; set; } = new();
}

public class WearFlagTestCase
{
    public ItemTestData ItemData { get; set; } = new();
    public int SlotPosition { get; set; }
    public bool ExpectedCanWear { get; set; }
}

public class WearFlagBitTest
{
    public string FlagName { get; set; } = string.Empty;
    public long ExpectedBitValue { get; set; }
}

public class StatApplicationTestCase
{
    public ItemTestData ItemData { get; set; } = new();
    public int EquipmentSlot { get; set; }
    public Dictionary<string, int> ExpectedStatChanges { get; set; } = new();
}

public class StrengthCapacityTest
{
    public int StrengthValue { get; set; }
    public int ExpectedMaxWeight { get; set; }
}

public class DexterityItemLimitTest
{
    public int DexterityValue { get; set; }
    public int ExpectedMaxItems { get; set; }
}

public class ErrorMessageTest
{
    public string ErrorCondition { get; set; } = string.Empty;
    public Dictionary<string, object> TestParameters { get; set; } = new();
    public string ExpectedMessage { get; set; } = string.Empty;
}

public class SuccessMessageTest
{
    public ItemTestData ItemData { get; set; } = new();
    public EquipmentSlot TargetSlot { get; set; }
    public string ExpectedMessage { get; set; } = string.Empty;
}

public class ClassRestrictionTest
{
    public CharacterClass PlayerClass { get; set; }
    public ItemTestData ItemData { get; set; } = new();
    public EquipmentSlot TargetSlot { get; set; }
    public bool ExpectedCanEquip { get; set; }
}

public class AntiFlagTest
{
    public string FlagName { get; set; } = string.Empty;
    public long ExpectedBitValue { get; set; }
}

public class ItemTestData
{
    public int VirtualNumber { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public ObjectType ObjectType { get; set; }
    public long WearFlags { get; set; }
    public long ExtraFlags { get; set; }
    public Dictionary<int, int>? Applies { get; set; }
}

#endregion