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
        
        // Get the wear flag bit for the slot
        int wearBit = GetWearBitForSlot(slot);
        if (wearBit == -1) return false;
        
        // Check if the item has the appropriate wear flag
        return (item.WearFlags & (1 << wearBit)) != 0;
    }
    
    /// <summary>
    /// Get the wear bit position for an equipment slot
    /// Based on original CircleMUD ITEM_WEAR_* flags
    /// </summary>
    /// <param name="slot">Equipment slot</param>
    /// <returns>Bit position for wear flag, or -1 if invalid</returns>
    private static int GetWearBitForSlot(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Light => 0,        // ITEM_WEAR_LIGHT
            EquipmentSlot.FingerRight => 1,  // ITEM_WEAR_FINGER
            EquipmentSlot.FingerLeft => 1,   // ITEM_WEAR_FINGER (same as right)
            EquipmentSlot.Neck1 => 3,        // ITEM_WEAR_NECK
            EquipmentSlot.Neck2 => 3,        // ITEM_WEAR_NECK (same as neck1)
            EquipmentSlot.Body => 5,         // ITEM_WEAR_BODY
            EquipmentSlot.Head => 6,         // ITEM_WEAR_HEAD
            EquipmentSlot.Legs => 7,         // ITEM_WEAR_LEGS
            EquipmentSlot.Feet => 8,         // ITEM_WEAR_FEET
            EquipmentSlot.Hands => 9,        // ITEM_WEAR_HANDS
            EquipmentSlot.Arms => 10,        // ITEM_WEAR_ARMS
            EquipmentSlot.Shield => 11,      // ITEM_WEAR_SHIELD
            EquipmentSlot.About => 12,       // ITEM_WEAR_ABOUT
            EquipmentSlot.Waist => 13,       // ITEM_WEAR_WAIST
            EquipmentSlot.WristRight => 14,  // ITEM_WEAR_WRIST
            EquipmentSlot.WristLeft => 14,   // ITEM_WEAR_WRIST (same as right)
            EquipmentSlot.Wield => 16,       // ITEM_WEAR_WIELD
            EquipmentSlot.Hold => 17,        // ITEM_WEAR_HOLD
            _ => -1
        };
    }
}