using C3Mud.Core.Equipment.Models;
using C3Mud.Core.Players;
using C3Mud.Core.World.Models;

namespace C3Mud.Core.Equipment.Services;

/// <summary>
/// Interface for inventory management operations
/// Based on original CircleMUD inventory functions
/// </summary>
public interface IInventoryManager
{
    /// <summary>
    /// Add an item to inventory
    /// </summary>
    /// <param name="item">Item to add</param>
    /// <returns>Result of the add operation</returns>
    EquipmentOperationResult AddItem(WorldObject item);
    
    /// <summary>
    /// Remove an item from inventory
    /// </summary>
    /// <param name="item">Item to remove</param>
    /// <returns>Result of the remove operation</returns>
    EquipmentOperationResult RemoveItem(WorldObject item);
    
    /// <summary>
    /// Check if player has an item by virtual number
    /// </summary>
    /// <param name="vnum">Virtual number of the item</param>
    /// <returns>True if player has the item</returns>
    bool HasItem(int vnum);
    
    /// <summary>
    /// Get current inventory weight
    /// </summary>
    /// <returns>Total weight of inventory items</returns>
    int GetCurrentWeight();
    
    /// <summary>
    /// Get maximum weight capacity based on strength
    /// </summary>
    /// <returns>Maximum weight that can be carried</returns>
    int GetMaxWeightCapacity();
    
    /// <summary>
    /// Get maximum item capacity based on dexterity
    /// </summary>
    /// <returns>Maximum number of items that can be carried</returns>
    int GetMaxItemCapacity();
    
    /// <summary>
    /// Put an item into a container
    /// </summary>
    /// <param name="item">Item to put in container</param>
    /// <param name="container">Container to put item in</param>
    /// <returns>Result of the put operation</returns>
    EquipmentOperationResult PutItemInContainer(WorldObject item, WorldObject container);
    
    /// <summary>
    /// Get an item from a container
    /// </summary>
    /// <param name="item">Item to get from container</param>
    /// <param name="container">Container to get item from</param>
    /// <returns>Result of the get operation</returns>
    EquipmentOperationResult GetItemFromContainer(WorldObject item, WorldObject container);
    
    /// <summary>
    /// List the contents of a container
    /// </summary>
    /// <param name="container">Container to list contents of</param>
    /// <returns>List of items in the container</returns>
    List<WorldObject> ListContainerContents(WorldObject container);
    
    /// <summary>
    /// Drop an item in the current room
    /// </summary>
    /// <param name="item">Item to drop</param>
    /// <param name="room">Room to drop item in</param>
    /// <returns>Result of the drop operation</returns>
    EquipmentOperationResult DropItemInRoom(WorldObject item, Room room);
    
    /// <summary>
    /// Get an item from the current room
    /// </summary>
    /// <param name="item">Item to get</param>
    /// <param name="room">Room to get item from</param>
    /// <returns>Result of the get operation</returns>
    EquipmentOperationResult GetItemFromRoom(WorldObject item, Room room);
    
    /// <summary>
    /// Add gold to player's inventory
    /// </summary>
    /// <param name="amount">Amount of gold to add</param>
    /// <returns>Result of the operation</returns>
    EquipmentOperationResult AddGold(int amount);
    
    /// <summary>
    /// Spend gold from player's inventory
    /// </summary>
    /// <param name="amount">Amount of gold to spend</param>
    /// <returns>Result of the operation</returns>
    EquipmentOperationResult SpendGold(int amount);
    
    /// <summary>
    /// Drop gold in a room
    /// </summary>
    /// <param name="amount">Amount of gold to drop</param>
    /// <param name="room">Room to drop gold in</param>
    /// <returns>Result of the operation</returns>
    EquipmentOperationResult DropGold(int amount, Room room);
    
    /// <summary>
    /// Get gold from a room
    /// </summary>
    /// <param name="goldPile">Gold object to pick up</param>
    /// <param name="room">Room to get gold from</param>
    /// <returns>Result of the operation</returns>
    EquipmentOperationResult GetGoldFromRoom(WorldObject goldPile, Room room);
    
    /// <summary>
    /// Find an item in inventory by keyword
    /// </summary>
    /// <param name="keyword">Keyword to search for</param>
    /// <returns>First matching item or null</returns>
    WorldObject? FindItem(string keyword);
    
    /// <summary>
    /// Get formatted inventory display
    /// </summary>
    /// <returns>Formatted inventory list</returns>
    string GetInventoryDisplay();
}