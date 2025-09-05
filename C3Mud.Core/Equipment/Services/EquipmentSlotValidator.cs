using C3Mud.Core.World.Models;

namespace C3Mud.Core.Equipment.Services;

/// <summary>
/// Validator for checking if items can be worn in specific equipment slots
/// Based on original CircleMUD can_wear_on_eq() function
/// </summary>
public static class EquipmentSlotValidator
{
    /// <summary>
    /// Check if an item can be worn in a specific equipment slot
    /// Based on original CircleMUD wear flags validation
    /// </summary>
    /// <param name="item">Item to check</param>
    /// <param name="slot">Equipment slot to check</param>
    /// <returns>True if item can be worn in the slot</returns>
    public static bool CanWearInSlot(WorldObject item, EquipmentSlot slot)
    {
        if (item == null) return false;
        
        // Get the wear flag for the slot using modern C# enum approach
        var requiredFlag = GetWearFlagForSlot(slot);
        if (!requiredFlag.HasValue) return false;
        
        // Check if the item has the appropriate wear flag using HasFlag
        var itemWearFlags = (WearFlags)item.WearFlags;
        return itemWearFlags.HasFlag(requiredFlag.Value);
    }
    
    /// <summary>
    /// Check if an item can be worn in a specific equipment slot by a specific race
    /// Includes both wear flag validation and race restrictions
    /// Based on original CircleMUD race-based equipment restrictions
    /// </summary>
    /// <param name="item">Item to check</param>
    /// <param name="slot">Equipment slot to check</param>
    /// <param name="race">Player's race</param>
    /// <returns>True if item can be worn in the slot by the race</returns>
    public static bool CanWearInSlot(WorldObject item, EquipmentSlot slot, Race race)
    {
        if (item == null) return false;
        
        // First check basic wear flag compatibility
        if (!CanWearInSlot(item, slot)) return false;
        
        // Then check race restrictions
        return CanRaceWearInSlot(race, slot);
    }
    
    /// <summary>
    /// Check if a race can wear items in a specific equipment slot
    /// Based on original CircleMUD race restrictions
    /// </summary>
    /// <param name="race">Player's race</param>
    /// <param name="slot">Equipment slot to check</param>
    /// <returns>True if race can wear items in the slot</returns>
    public static bool CanRaceWearInSlot(Race race, EquipmentSlot slot)
    {
        return slot switch
        {
            // Legs: Cannot be worn by quadrupeds or snake-like races
            EquipmentSlot.Legs => race != Race.ThriKreen && race != Race.Yuanti && 
                                 race != Race.Centaur && race != Race.Saurian,
            
            // Feet: Cannot be worn by quadrupeds or snake-like races  
            EquipmentSlot.Feet => race != Race.ThriKreen && race != Race.Yuanti && 
                                 race != Race.Centaur && race != Race.Saurian,
            
            // Tail: Only for races with tails
            EquipmentSlot.Tail => race == Race.Yuanti || race == Race.Saurian,
            
            // Four legs: Only for quadrupeds
            EquipmentSlot.FourLegs1 => race == Race.ThriKreen || race == Race.Centaur,
            EquipmentSlot.FourLegs2 => race == Race.ThriKreen || race == Race.Centaur,
            
            // All other slots have no race restrictions
            _ => true
        };
    }
    
    /// <summary>
    /// Get the wear flag enum value for an equipment slot
    /// Based on original CircleMUD ITEM_WEAR_* flags using modern C# enum patterns
    /// </summary>
    /// <param name="slot">Equipment slot</param>
    /// <returns>WearFlags enum value, or null if not a wear flag slot</returns>
    private static WearFlags? GetWearFlagForSlot(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Light => WearFlags.TAKE,           // Light sources are takeable items
            EquipmentSlot.FingerRight => WearFlags.FINGER,   // ITEM_WEAR_FINGER
            EquipmentSlot.FingerLeft => WearFlags.FINGER,    // ITEM_WEAR_FINGER (same as right)
            EquipmentSlot.Neck1 => WearFlags.NECK,           // ITEM_WEAR_NECK
            EquipmentSlot.Neck2 => WearFlags.NECK,           // ITEM_WEAR_NECK (same as neck1)
            EquipmentSlot.Body => WearFlags.BODY,            // ITEM_WEAR_BODY
            EquipmentSlot.Head => WearFlags.HEAD,            // ITEM_WEAR_HEAD
            EquipmentSlot.Legs => WearFlags.LEGS,            // ITEM_WEAR_LEGS
            EquipmentSlot.Feet => WearFlags.FEET,            // ITEM_WEAR_FEET
            EquipmentSlot.Hands => WearFlags.HANDS,          // ITEM_WEAR_HANDS
            EquipmentSlot.Arms => WearFlags.ARMS,            // ITEM_WEAR_ARMS
            EquipmentSlot.Shield => WearFlags.SHIELD,        // ITEM_WEAR_SHIELD
            EquipmentSlot.About => WearFlags.ABOUT,          // ITEM_WEAR_ABOUT
            EquipmentSlot.Waist => WearFlags.WAIST,          // ITEM_WEAR_WAISTE
            EquipmentSlot.WristRight => WearFlags.WRIST,     // ITEM_WEAR_WRIST
            EquipmentSlot.WristLeft => WearFlags.WRIST,      // ITEM_WEAR_WRIST (same as right)
            EquipmentSlot.Wield => WearFlags.WIELD,          // ITEM_WIELD
            EquipmentSlot.Hold => WearFlags.HOLD,            // ITEM_HOLD
            EquipmentSlot.Tail => WearFlags.TAIL,            // ITEM_WEAR_TAIL
            EquipmentSlot.FourLegs1 => WearFlags.FOURLEGS,   // ITEM_WEAR_4LEGS
            EquipmentSlot.FourLegs2 => WearFlags.FOURLEGS,   // ITEM_WEAR_4LEGS (same flag for both slots)
            _ => null
        };
    }
}