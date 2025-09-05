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
            EquipmentSlot.Light => 0,        // ITEM_WEAR_LIGHT (BIT0)
            EquipmentSlot.FingerRight => 1,  // ITEM_WEAR_FINGER (BIT1)
            EquipmentSlot.FingerLeft => 1,   // ITEM_WEAR_FINGER (BIT1, same as right)
            EquipmentSlot.Neck1 => 2,        // ITEM_WEAR_NECK (BIT2) - FIXED from 3
            EquipmentSlot.Neck2 => 2,        // ITEM_WEAR_NECK (BIT2, same as neck1) - FIXED from 3
            EquipmentSlot.Body => 3,         // ITEM_WEAR_BODY (BIT3) - FIXED from 5
            EquipmentSlot.Head => 4,         // ITEM_WEAR_HEAD (BIT4) - FIXED from 6
            EquipmentSlot.Legs => 5,         // ITEM_WEAR_LEGS (BIT5) - FIXED from 7
            EquipmentSlot.Feet => 6,         // ITEM_WEAR_FEET (BIT6) - FIXED from 8
            EquipmentSlot.Hands => 7,        // ITEM_WEAR_HANDS (BIT7) - FIXED from 9
            EquipmentSlot.Arms => 8,         // ITEM_WEAR_ARMS (BIT8) - FIXED from 10
            EquipmentSlot.Shield => 9,       // ITEM_WEAR_SHIELD (BIT9) - FIXED from 11
            EquipmentSlot.About => 10,       // ITEM_WEAR_ABOUT (BIT10) - FIXED from 12
            EquipmentSlot.Waist => 11,       // ITEM_WEAR_WAISTE (BIT11) - FIXED from 13
            EquipmentSlot.WristRight => 12,  // ITEM_WEAR_WRIST (BIT12) - FIXED from 14
            EquipmentSlot.WristLeft => 12,   // ITEM_WEAR_WRIST (BIT12, same as right) - FIXED from 14
            EquipmentSlot.Wield => -1,       // ITEM_WIELD - Not a wear flag, handled separately
            EquipmentSlot.Hold => -1,        // ITEM_HOLD - Not a wear flag, handled separately
            _ => -1
        };
    }
}