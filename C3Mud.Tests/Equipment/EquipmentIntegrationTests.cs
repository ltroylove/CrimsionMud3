using C3Mud.Core.Players;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;
using C3Mud.Core.Equipment.Models;
using C3Mud.Core.Equipment.Services;
using C3Mud.Core.Combat;
using C3Mud.Core.Combat.Models;
using C3Mud.Core.Characters;
using C3Mud.Core.Spells;
using FluentAssertions;
using Moq;
using Xunit;

namespace C3Mud.Tests.Equipment;

/// <summary>
/// TDD Red Phase tests for Iteration 6: Equipment & Inventory Management
/// Equipment Integration Tests - All tests should FAIL initially
/// 
/// Tests integration between equipment system and other game systems:
/// - Combat system integration (weapon damage, armor AC, hit/dam bonuses)
/// - Spell system integration (spell bonuses, mana bonuses)
/// - Character stats integration (strength, dex, con, int, wis, cha bonuses)
/// - Movement system integration (encumbrance effects)
/// 
/// Based on original CircleMUD affect_modify() and related functions
/// </summary>
public class EquipmentIntegrationTests
{
    private readonly Mock<IPlayer> _mockPlayer;
    private readonly Mock<IEquipmentManager> _mockEquipmentManager;
    private readonly Mock<ICombatEngine> _mockCombatEngine;
    private readonly Mock<ISpellEngine> _mockSpellEngine;
    private readonly Mock<IWorldDatabase> _mockWorldDatabase;
    
    // Test equipment with various stat bonuses
    private readonly WorldObject _strengthBoostingWeapon;
    private readonly WorldObject _armorClassBoostingArmor;
    private readonly WorldObject _hitDamBoostingRing;
    private readonly WorldObject _manaBoostingHelm;
    private readonly WorldObject _multiStatRing;

    public EquipmentIntegrationTests()
    {
        _mockPlayer = new Mock<IPlayer>();
        _mockEquipmentManager = new Mock<IEquipmentManager>();
        _mockCombatEngine = new Mock<ICombatEngine>();
        _mockSpellEngine = new Mock<ISpellEngine>();
        _mockWorldDatabase = new Mock<IWorldDatabase>();
        
        // Setup basic player properties
        _mockPlayer.Setup(p => p.Name).Returns("TestWarrior");
        _mockPlayer.Setup(p => p.Level).Returns(15);
        
        // Base stats without equipment
        _mockPlayer.SetupProperty(p => p.Strength, 16);
        _mockPlayer.SetupProperty(p => p.Dexterity, 14);
        _mockPlayer.SetupProperty(p => p.Constitution, 15);
        _mockPlayer.SetupProperty(p => p.ArmorClass, 10);
        _mockPlayer.SetupProperty(p => p.HitPoints, 150);
        _mockPlayer.SetupProperty(p => p.MaxHitPoints, 150);
        
        // Create test equipment
        _strengthBoostingWeapon = CreateStrengthBoostingWeapon();
        _armorClassBoostingArmor = CreateArmorClassBoostingArmor();
        _hitDamBoostingRing = CreateHitDamBoostingRing();
        _manaBoostingHelm = CreateManaBoostingHelm();
        _multiStatRing = CreateMultiStatRing();
    }

    #region Combat System Integration

    [Fact]
    public void EquipWeapon_CombatCalculations_UseCorrectWeaponStats()
    {
        // Arrange - This will fail until equipment integration is implemented
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var combatEngine = new CombatEngine(_mockWorldDatabase.Object);
        
        equipmentManager.EquipItem(_strengthBoostingWeapon, EquipmentSlot.Wield);
        
        // Act - Calculate damage with equipped weapon
        var damage = combatEngine.CalculateWeaponDamage(_mockPlayer.Object);
        
        // Assert - Should use weapon's damage dice (3d6+4)
        damage.Should().BeInRange(7, 22); // 3d6+4 range
        
        // Verify weapon is being used in calculations
        var weaponUsed = combatEngine.GetPlayerWeapon(_mockPlayer.Object);
        weaponUsed.Should().Be(_strengthBoostingWeapon);
    }

    [Fact]
    public void EquipArmor_ArmorClassCalculation_AppliesCorrectlyToCombat()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var combatEngine = new CombatEngine(_mockWorldDatabase.Object);
        
        var baseAC = _mockPlayer.Object.ArmorClass; // Should be 10
        equipmentManager.EquipItem(_armorClassBoostingArmor, EquipmentSlot.Body); // AC +6
        
        // Act - Check AC in combat calculations
        var effectiveAC = combatEngine.CalculateEffectiveArmorClass(_mockPlayer.Object);
        
        // Assert - AC should improve (lower number is better)
        effectiveAC.Should().Be(baseAC - 6); // 10 - 6 = 4
    }

    [Fact]
    public void EquipHitDamBonusItem_CombatRolls_AppliesBonusesCorrectly()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var combatEngine = new CombatEngine(_mockWorldDatabase.Object);
        
        equipmentManager.EquipItem(_hitDamBoostingRing, EquipmentSlot.FingerRight); // +3 hit, +5 dam
        
        // Act
        var hitBonus = combatEngine.GetPlayerHitBonus(_mockPlayer.Object);
        var damageBonus = combatEngine.GetPlayerDamageBonus(_mockPlayer.Object);
        
        // Assert
        hitBonus.Should().Be(3, "Hit bonus from ring should be +3");
        damageBonus.Should().Be(5, "Damage bonus from ring should be +5");
    }

    [Fact]
    public void UnequipWeapon_CombatCalculations_RevertToUnarmedCombat()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var combatEngine = new CombatEngine(_mockWorldDatabase.Object);
        
        equipmentManager.EquipItem(_strengthBoostingWeapon, EquipmentSlot.Wield);
        
        // Act
        equipmentManager.UnequipItem(EquipmentSlot.Wield);
        var damage = combatEngine.CalculateWeaponDamage(_mockPlayer.Object);
        
        // Assert - Should revert to bare-hand damage (1d2 in original CircleMUD)
        damage.Should().BeInRange(1, 2);
        
        var weaponUsed = combatEngine.GetPlayerWeapon(_mockPlayer.Object);
        weaponUsed.Should().BeNull();
    }

    [Fact]
    public void MultipleArmorPieces_ArmorClassCalculation_CumulativeEffect()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var combatEngine = new CombatEngine(_mockWorldDatabase.Object);
        
        var baseAC = _mockPlayer.Object.ArmorClass;
        var helmet = CreateArmorWithAC(2);
        var shield = CreateArmorWithAC(3);
        var boots = CreateArmorWithAC(1);
        
        // Act - Equip multiple armor pieces
        equipmentManager.EquipItem(_armorClassBoostingArmor, EquipmentSlot.Body); // AC +6
        equipmentManager.EquipItem(helmet, EquipmentSlot.Head); // AC +2
        equipmentManager.EquipItem(shield, EquipmentSlot.Shield); // AC +3
        equipmentManager.EquipItem(boots, EquipmentSlot.Feet); // AC +1
        
        var finalAC = combatEngine.CalculateEffectiveArmorClass(_mockPlayer.Object);
        
        // Assert - Total AC improvement should be 12 (6+2+3+1)
        finalAC.Should().Be(baseAC - 12);
    }

    #endregion

    #region Character Stats Integration

    [Fact]
    public void EquipStatBoostingItem_PlayerStats_ModifiedCorrectly()
    {
        // Arrange - This will fail until stat integration is implemented
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var statsManager = new CharacterStatsManager(_mockPlayer.Object);
        
        var baseStr = _mockPlayer.Object.Strength;
        
        // Act
        equipmentManager.EquipItem(_strengthBoostingWeapon, EquipmentSlot.Wield); // +2 STR
        
        // Assert
        var modifiedStr = statsManager.GetEffectiveStrength();
        modifiedStr.Should().Be(baseStr + 2);
    }

    [Fact]
    public void EquipMultipleStatItems_PlayerStats_AllBonusesApply()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var statsManager = new CharacterStatsManager(_mockPlayer.Object);
        
        var baseStr = _mockPlayer.Object.Strength;
        var baseDex = _mockPlayer.Object.Dexterity;
        var baseCon = _mockPlayer.Object.Constitution;
        
        // Act
        equipmentManager.EquipItem(_strengthBoostingWeapon, EquipmentSlot.Wield); // +2 STR
        equipmentManager.EquipItem(_multiStatRing, EquipmentSlot.FingerLeft); // +1 all stats
        
        // Assert
        statsManager.GetEffectiveStrength().Should().Be(baseStr + 2 + 1);
        statsManager.GetEffectiveDexterity().Should().Be(baseDex + 1);
        statsManager.GetEffectiveConstitution().Should().Be(baseCon + 1);
    }

    [Fact]
    public void UnequipStatBoostingItem_PlayerStats_RevertedCorrectly()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var statsManager = new CharacterStatsManager(_mockPlayer.Object);
        
        var baseStr = _mockPlayer.Object.Strength;
        equipmentManager.EquipItem(_strengthBoostingWeapon, EquipmentSlot.Wield);
        
        // Act
        equipmentManager.UnequipItem(EquipmentSlot.Wield);
        
        // Assert
        var finalStr = statsManager.GetEffectiveStrength();
        finalStr.Should().Be(baseStr);
    }

    [Fact]
    public void StatBonuses_AffectCarryingCapacity_RecalculatedCorrectly()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var inventoryManager = new InventoryManager(_mockPlayer.Object);
        
        var baseCapacity = inventoryManager.GetMaxWeightCapacity();
        
        // Act
        equipmentManager.EquipItem(_strengthBoostingWeapon, EquipmentSlot.Wield); // +2 STR
        var modifiedCapacity = inventoryManager.GetMaxWeightCapacity();
        
        // Assert - Higher strength should increase carrying capacity
        modifiedCapacity.Should().BeGreaterThan(baseCapacity);
    }

    #endregion

    #region Health and Mana Integration

    [Fact]
    public void EquipHealthBoostingItem_PlayerHitPoints_IncreasedCorrectly()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var healthManager = new PlayerHealthManager(_mockPlayer.Object);
        
        var baseMaxHP = _mockPlayer.Object.MaxHitPoints;
        var healthBoostingArmor = CreateHealthBoostingArmor(); // +20 HP
        
        // Act
        equipmentManager.EquipItem(healthBoostingArmor, EquipmentSlot.Body);
        
        // Assert
        var modifiedMaxHP = healthManager.GetEffectiveMaxHitPoints();
        modifiedMaxHP.Should().Be(baseMaxHP + 20);
    }

    [Fact]
    public void EquipManaBoostingItem_PlayerMana_IncreasedCorrectly()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var manaManager = new PlayerManaManager(_mockPlayer.Object);
        
        var baseMaxMana = manaManager.GetBaseMaxMana();
        
        // Act
        equipmentManager.EquipItem(_manaBoostingHelm, EquipmentSlot.Head); // +30 MANA
        
        // Assert
        var modifiedMaxMana = manaManager.GetEffectiveMaxMana();
        modifiedMaxMana.Should().Be(baseMaxMana + 30);
    }

    [Fact]
    public void UnequipHealthItem_PlayerHP_AdjustedSafely()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var healthManager = new PlayerHealthManager(_mockPlayer.Object);
        
        var healthBoostingArmor = CreateHealthBoostingArmor(); // +20 HP
        equipmentManager.EquipItem(healthBoostingArmor, EquipmentSlot.Body);
        
        // Set current HP to a value that would exceed base max
        _mockPlayer.SetupProperty(p => p.HitPoints, _mockPlayer.Object.MaxHitPoints + 10);
        
        // Act
        equipmentManager.UnequipItem(EquipmentSlot.Body);
        
        // Assert - Current HP should be adjusted down but not kill player
        var finalHP = _mockPlayer.Object.HitPoints;
        finalHP.Should().BeLessOrEqualTo(_mockPlayer.Object.MaxHitPoints);
        finalHP.Should().BeGreaterThan(0); // Should not kill player
    }

    #endregion

    #region Spell System Integration

    [Fact]
    public void EquipSpellBoostingItem_SpellEffectiveness_Enhanced()
    {
        // Arrange - This will fail until spell integration is implemented
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var spellEngine = new SpellEngine(_mockWorldDatabase.Object);
        
        var spellBoostingStaff = CreateSpellBoostingStaff(); // +5 spell levels
        equipmentManager.EquipItem(spellBoostingStaff, EquipmentSlot.Hold);
        
        // Act - Cast a healing spell
        var healAmount = spellEngine.CalculateHealingAmount(_mockPlayer.Object, SpellType.Heal);
        
        // Assert - Should be enhanced by equipment bonus
        healAmount.Should().BeGreaterThan(0);
        // Actual calculation depends on spell system implementation
    }

    [Fact]
    public void EquipManaRegenerationItem_ManaRecovery_Enhanced()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var manaManager = new PlayerManaManager(_mockPlayer.Object);
        
        var manaRegenerationRing = CreateManaRegenerationRing(); // +2 mana regen/tick
        
        // Act
        equipmentManager.EquipItem(manaRegenerationRing, EquipmentSlot.FingerRight);
        var regenRate = manaManager.GetManaRegenerationRate();
        
        // Assert
        regenRate.Should().BeGreaterThan(0);
        // Exact rate depends on base regen + equipment bonus
    }

    #endregion

    #region Movement and Encumbrance Integration

    [Fact]
    public void HeavyEquipment_MovementSpeed_ReducedCorrectly()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var movementManager = new PlayerMovementManager(_mockPlayer.Object);
        
        var baseMovementDelay = movementManager.GetMovementDelay();
        var heavyArmor = CreateHeavyArmor(); // Very heavy
        
        // Act
        equipmentManager.EquipItem(heavyArmor, EquipmentSlot.Body);
        var encumberedMovementDelay = movementManager.GetMovementDelay();
        
        // Assert - Movement should be slower when encumbered
        encumberedMovementDelay.Should().BeGreaterThan(baseMovementDelay);
    }

    [Fact]
    public void OverEncumbered_Movement_Blocked()
    {
        // Arrange - Player at maximum carrying capacity
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var movementManager = new PlayerMovementManager(_mockPlayer.Object);
        
        // Fill up to max capacity
        var veryHeavyArmor = CreateExtremelyHeavyArmor();
        equipmentManager.EquipItem(veryHeavyArmor, EquipmentSlot.Body);
        
        // Act - Try to move
        var canMove = movementManager.CanMove();
        
        // Assert
        canMove.Should().BeFalse();
    }

    [Fact]
    public void FlightItem_Movement_EnablesFlight()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var movementManager = new PlayerMovementManager(_mockPlayer.Object);
        
        var wingBoots = CreateFlightItem(); // Enables flight
        
        // Act
        equipmentManager.EquipItem(wingBoots, EquipmentSlot.Feet);
        var canFly = movementManager.CanFly();
        
        // Assert
        canFly.Should().BeTrue();
    }

    #endregion

    #region Performance and Edge Cases

    [Fact]
    public void EquipmentStatCalculations_PerformanceTest_AcceptableSpeed()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var statsManager = new CharacterStatsManager(_mockPlayer.Object);
        
        // Equip many stat-modifying items
        equipmentManager.EquipItem(_strengthBoostingWeapon, EquipmentSlot.Wield);
        equipmentManager.EquipItem(_hitDamBoostingRing, EquipmentSlot.FingerRight);
        equipmentManager.EquipItem(_multiStatRing, EquipmentSlot.FingerLeft);
        equipmentManager.EquipItem(_armorClassBoostingArmor, EquipmentSlot.Body);
        equipmentManager.EquipItem(_manaBoostingHelm, EquipmentSlot.Head);
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act - Recalculate stats many times
        for (int i = 0; i < 1000; i++)
        {
            statsManager.RecalculateAllStats();
        }
        
        stopwatch.Stop();
        
        // Assert - Should be fast
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public void EquipmentSwapping_StatConsistency_MaintainedCorrectly()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var statsManager = new CharacterStatsManager(_mockPlayer.Object);
        
        var originalStr = _mockPlayer.Object.Strength;
        
        // Act - Swap equipment multiple times
        for (int i = 0; i < 10; i++)
        {
            equipmentManager.EquipItem(_strengthBoostingWeapon, EquipmentSlot.Wield);
            equipmentManager.UnequipItem(EquipmentSlot.Wield);
        }
        
        // Assert - Stats should return to original values
        var finalStr = statsManager.GetEffectiveStrength();
        finalStr.Should().Be(originalStr);
    }

    [Fact]
    public void EquipmentWithNegativeStats_StatCalculation_HandledCorrectly()
    {
        // Arrange
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var statsManager = new CharacterStatsManager(_mockPlayer.Object);
        
        var cursedItem = CreateCursedItem(); // -3 STR, -2 DEX
        var baseStr = _mockPlayer.Object.Strength;
        var baseDex = _mockPlayer.Object.Dexterity;
        
        // Act
        equipmentManager.EquipItem(cursedItem, EquipmentSlot.FingerRight);
        
        // Assert
        statsManager.GetEffectiveStrength().Should().Be(baseStr - 3);
        statsManager.GetEffectiveDexterity().Should().Be(baseDex - 2);
        
        // Stats should not go below 1
        if (baseStr <= 3)
        {
            statsManager.GetEffectiveStrength().Should().Be(1);
        }
    }

    #endregion

    #region Legacy Compatibility Tests

    [Fact]
    public void EquipmentStatBonuses_MatchOriginalCircleMudApplies()
    {
        // Test based on original APPLY_* constants from structs.h
        var applyTypes = new Dictionary<int, string>
        {
            { 1, "APPLY_STR" },
            { 2, "APPLY_DEX" },  
            { 3, "APPLY_INT" },
            { 4, "APPLY_WIS" },
            { 5, "APPLY_CON" },
            { 6, "APPLY_CHA" },
            { 13, "APPLY_AC" },
            { 18, "APPLY_HITROLL" },
            { 19, "APPLY_DAMROLL" },
            { 20, "APPLY_HIT" },
            { 21, "APPLY_MANA" }
        };

        foreach (var applyType in applyTypes)
        {
            // Arrange
            var testItem = CreateItemWithApply(applyType.Key, 5);
            var equipmentManager = new EquipmentManager(_mockPlayer.Object);
            
            // Act
            var result = equipmentManager.EquipItem(testItem, EquipmentSlot.Body);
            
            // Assert
            result.Success.Should().BeTrue($"Should be able to equip item with {applyType.Value}");
            // Stat system should recognize and apply the bonus
        }
    }

    [Fact]
    public void EquipmentAffectModify_MatchesOriginalBehavior()
    {
        // Based on original affect_modify() function behavior
        var equipmentManager = new EquipmentManager(_mockPlayer.Object);
        var statsManager = new CharacterStatsManager(_mockPlayer.Object);
        
        // Test item with multiple applies (like original multi-stat items)
        var powerfulRing = new WorldObject
        {
            VirtualNumber = 8001,
            Name = "ring powerful magical",
            ShortDescription = "a powerful magical ring",
            ObjectType = ObjectType.WORN,
            WearFlags = (1 << 1) | (1 << 2), // ITEM_WEAR_FINGER
            Applies = new Dictionary<int, int>
            {
                { 1, 3 },  // APPLY_STR +3
                { 2, 2 },  // APPLY_DEX +2  
                { 18, 4 }, // APPLY_HITROLL +4
                { 19, 6 }, // APPLY_DAMROLL +6
                { 20, 25 } // APPLY_HIT +25
            }
        };
        
        var baseStr = _mockPlayer.Object.Strength;
        var baseDex = _mockPlayer.Object.Dexterity;
        
        // Act
        equipmentManager.EquipItem(powerfulRing, EquipmentSlot.FingerLeft);
        
        // Assert - All applies should take effect
        statsManager.GetEffectiveStrength().Should().Be(baseStr + 3);
        statsManager.GetEffectiveDexterity().Should().Be(baseDex + 2);
        // Hit, hitroll, damroll bonuses should also apply
    }

    #endregion

    #region Helper Methods

    private WorldObject CreateStrengthBoostingWeapon()
    {
        return new WorldObject
        {
            VirtualNumber = 4001,
            Name = "hammer war mighty",
            ShortDescription = "a mighty war hammer",
            LongDescription = "A mighty war hammer radiates strength.",
            ObjectType = ObjectType.WEAPON,
            WearFlags = (1 << 16), // ITEM_WEAR_WIELD
            Weight = 20,
            Values = new int[] { 3, 6, 4, 7 }, // 3d6+4 bludgeoning weapon
            Applies = new Dictionary<int, int>
            {
                { 1, 2 },  // APPLY_STR +2
                { 18, 3 }, // APPLY_HITROLL +3
                { 19, 4 }  // APPLY_DAMROLL +4
            }
        };
    }

    private WorldObject CreateArmorClassBoostingArmor()
    {
        return new WorldObject
        {
            VirtualNumber = 4002,
            Name = "armor plate steel",
            ShortDescription = "steel plate armor",
            LongDescription = "Gleaming steel plate armor provides excellent protection.",
            ObjectType = ObjectType.ARMOR,
            WearFlags = (1 << 5), // ITEM_WEAR_BODY
            Weight = 45,
            Values = new int[] { 6, 0, 0, 0 }, // AC +6
            Applies = new Dictionary<int, int>()
        };
    }

    private WorldObject CreateHitDamBoostingRing()
    {
        return new WorldObject
        {
            VirtualNumber = 4003,
            Name = "ring combat warrior",
            ShortDescription = "a warrior's combat ring",
            LongDescription = "A ring favored by seasoned warriors.",
            ObjectType = ObjectType.WORN,
            WearFlags = (1 << 1) | (1 << 2), // ITEM_WEAR_FINGER
            Weight = 1,
            Applies = new Dictionary<int, int>
            {
                { 18, 3 }, // APPLY_HITROLL +3
                { 19, 5 }  // APPLY_DAMROLL +5
            }
        };
    }

    private WorldObject CreateManaBoostingHelm()
    {
        return new WorldObject
        {
            VirtualNumber = 4004,
            Name = "helm mystic blue",
            ShortDescription = "a mystic blue helm",
            LongDescription = "A helm glowing with mystical blue energy.",
            ObjectType = ObjectType.ARMOR,
            WearFlags = (1 << 6), // ITEM_WEAR_HEAD
            Weight = 8,
            Values = new int[] { 2, 0, 0, 0 }, // AC +2
            Applies = new Dictionary<int, int>
            {
                { 21, 30 }, // APPLY_MANA +30
                { 3, 1 }    // APPLY_INT +1
            }
        };
    }

    private WorldObject CreateMultiStatRing()
    {
        return new WorldObject
        {
            VirtualNumber = 4005,
            Name = "ring balance harmony",
            ShortDescription = "a ring of balance",
            LongDescription = "A perfectly balanced ring enhances all abilities.",
            ObjectType = ObjectType.WORN,
            WearFlags = (1 << 1) | (1 << 2), // ITEM_WEAR_FINGER
            Weight = 1,
            Applies = new Dictionary<int, int>
            {
                { 1, 1 }, // APPLY_STR +1
                { 2, 1 }, // APPLY_DEX +1
                { 3, 1 }, // APPLY_INT +1
                { 4, 1 }, // APPLY_WIS +1
                { 5, 1 }, // APPLY_CON +1
                { 6, 1 }  // APPLY_CHA +1
            }
        };
    }

    private WorldObject CreateArmorWithAC(int acBonus)
    {
        return new WorldObject
        {
            VirtualNumber = 5000 + acBonus,
            Name = $"armor ac{acBonus}",
            ShortDescription = $"armor with +{acBonus} AC",
            LongDescription = "Test armor piece.",
            ObjectType = ObjectType.ARMOR,
            WearFlags = (1 << 6), // ITEM_WEAR_HEAD (default)
            Weight = 5,
            Values = new int[] { acBonus, 0, 0, 0 }
        };
    }

    private WorldObject CreateHealthBoostingArmor()
    {
        return new WorldObject
        {
            VirtualNumber = 4006,
            Name = "armor vitality life",
            ShortDescription = "armor of vitality",
            LongDescription = "Armor that pulses with life energy.",
            ObjectType = ObjectType.ARMOR,
            WearFlags = (1 << 5), // ITEM_WEAR_BODY
            Weight = 25,
            Values = new int[] { 4, 0, 0, 0 }, // AC +4
            Applies = new Dictionary<int, int>
            {
                { 20, 20 }, // APPLY_HIT +20
                { 5, 1 }    // APPLY_CON +1
            }
        };
    }

    private WorldObject CreateSpellBoostingStaff()
    {
        return new WorldObject
        {
            VirtualNumber = 4007,
            Name = "staff power arcane",
            ShortDescription = "a staff of arcane power",
            LongDescription = "A staff crackling with arcane energy.",
            ObjectType = ObjectType.STAFF,
            WearFlags = (1 << 17), // ITEM_WEAR_HOLD
            Weight = 8,
            Values = new int[] { 15, 3, 3, 201 }, // Level 15, 3 charges, 3 max, fireball spell
            Applies = new Dictionary<int, int>
            {
                { 3, 2 },   // APPLY_INT +2
                { 21, 20 }, // APPLY_MANA +20
                { 22, 5 }   // APPLY_SPELL_LEVEL +5 (custom apply)
            }
        };
    }

    private WorldObject CreateManaRegenerationRing()
    {
        return new WorldObject
        {
            VirtualNumber = 4008,
            Name = "ring regeneration mana",
            ShortDescription = "a mana regeneration ring",
            LongDescription = "A ring that pulses with restorative energy.",
            ObjectType = ObjectType.WORN,
            WearFlags = (1 << 1) | (1 << 2), // ITEM_WEAR_FINGER
            Weight = 1,
            Applies = new Dictionary<int, int>
            {
                { 23, 2 } // APPLY_MANA_REGEN +2 (custom apply)
            }
        };
    }

    private WorldObject CreateHeavyArmor()
    {
        return new WorldObject
        {
            VirtualNumber = 4009,
            Name = "armor plate heavy",
            ShortDescription = "heavy plate armor",
            LongDescription = "Extremely heavy plate armor.",
            ObjectType = ObjectType.ARMOR,
            WearFlags = (1 << 5), // ITEM_WEAR_BODY
            Weight = 80, // Very heavy
            Values = new int[] { 8, 0, 0, 0 } // AC +8
        };
    }

    private WorldObject CreateExtremelyHeavyArmor()
    {
        return new WorldObject
        {
            VirtualNumber = 4010,
            Name = "armor boulder",
            ShortDescription = "boulder armor",
            LongDescription = "Armor made from solid boulder.",
            ObjectType = ObjectType.ARMOR,
            WearFlags = (1 << 5), // ITEM_WEAR_BODY
            Weight = 500, // Extremely heavy
            Values = new int[] { 15, 0, 0, 0 } // AC +15
        };
    }

    private WorldObject CreateFlightItem()
    {
        return new WorldObject
        {
            VirtualNumber = 4011,
            Name = "boots winged flying",
            ShortDescription = "winged boots",
            LongDescription = "Boots with small wings enable flight.",
            ObjectType = ObjectType.ARMOR,
            WearFlags = (1 << 8), // ITEM_WEAR_FEET
            Weight = 3,
            Values = new int[] { 1, 0, 0, 0 }, // AC +1
            ExtraFlags = (1 << 20), // ITEM_FLY (custom flag)
            Applies = new Dictionary<int, int>()
        };
    }

    private WorldObject CreateCursedItem()
    {
        return new WorldObject
        {
            VirtualNumber = 4012,
            Name = "ring cursed doom",
            ShortDescription = "a cursed ring of doom",
            LongDescription = "A ring that radiates malevolent energy.",
            ObjectType = ObjectType.WORN,
            WearFlags = (1 << 1) | (1 << 2), // ITEM_WEAR_FINGER
            ExtraFlags = (1 << 2) | (1 << 3), // ITEM_NODROP | ITEM_CURSED
            Weight = 1,
            Applies = new Dictionary<int, int>
            {
                { 1, -3 }, // APPLY_STR -3
                { 2, -2 }  // APPLY_DEX -2
            }
        };
    }

    private WorldObject CreateItemWithApply(int applyType, int modifier)
    {
        return new WorldObject
        {
            VirtualNumber = 6000 + applyType,
            Name = $"item apply{applyType}",
            ShortDescription = $"item with apply {applyType}",
            LongDescription = "Test item with specific apply.",
            ObjectType = ObjectType.WORN,
            WearFlags = (1 << 5), // ITEM_WEAR_BODY
            Weight = 5,
            Applies = new Dictionary<int, int>
            {
                { applyType, modifier }
            }
        };
    }

    #endregion
}