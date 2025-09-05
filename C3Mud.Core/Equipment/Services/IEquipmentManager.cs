using C3Mud.Core.Equipment.Models;
using C3Mud.Core.Players;
using C3Mud.Core.World.Models;

namespace C3Mud.Core.Equipment.Services;

/// <summary>
/// Interface for equipment management operations
/// Based on original CircleMUD equipment functions
/// </summary>
public interface IEquipmentManager
{
    /// <summary>
    /// Equip an item in the specified slot
    /// </summary>
    /// <param name="item">Item to equip</param>
    /// <param name="slot">Equipment slot to place item in</param>
    /// <returns>Result of the equipment operation</returns>
    EquipmentOperationResult EquipItem(WorldObject item, EquipmentSlot slot);
    
    /// <summary>
    /// Unequip an item from the specified slot
    /// </summary>
    /// <param name="slot">Equipment slot to clear</param>
    /// <returns>Result of the unequip operation</returns>
    EquipmentOperationResult UnequipItem(EquipmentSlot slot);
    
    /// <summary>
    /// Get the carrying capacity based on strength
    /// </summary>
    /// <returns>Maximum weight that can be carried</returns>
    int GetCarryingCapacity();
    
    /// <summary>
    /// Get the current weight of all equipped items
    /// </summary>
    /// <returns>Total weight of equipped items</returns>
    int GetCurrentWeight();
    
    /// <summary>
    /// Check if an item can be equipped in a specific slot
    /// </summary>
    /// <param name="item">Item to check</param>
    /// <param name="slot">Slot to check</param>
    /// <returns>True if item can be equipped in slot</returns>
    bool CanEquipInSlot(WorldObject item, EquipmentSlot slot);
    
    /// <summary>
    /// Get equipment display for the equipment command
    /// </summary>
    /// <returns>Formatted equipment list</returns>
    string GetEquipmentDisplay();
}