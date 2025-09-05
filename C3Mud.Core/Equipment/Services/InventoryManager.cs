using C3Mud.Core.Equipment.Models;
using C3Mud.Core.Players;
using C3Mud.Core.World.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace C3Mud.Core.Equipment.Services;

/// <summary>
/// Inventory management service implementation
/// Based on original CircleMUD inventory functions
/// </summary>
public class InventoryManager : IInventoryManager
{
    private readonly IPlayer _player;
    
    /// <summary>
    /// Strength-based weight capacity table from original CircleMUD
    /// </summary>
    private static readonly Dictionary<int, int> StrengthWeightTable = new()
    {
        { 3, 10 }, { 4, 20 }, { 5, 30 }, { 6, 40 }, { 7, 50 },
        { 8, 60 }, { 9, 70 }, { 10, 80 }, { 11, 90 }, { 12, 100 },
        { 13, 110 }, { 14, 120 }, { 15, 130 }, { 16, 130 }, { 17, 150 },
        { 18, 200 }, { 19, 300 }, { 20, 400 }, { 21, 500 }, { 22, 600 },
        { 23, 700 }, { 24, 800 }, { 25, 640 }
    };
    
    /// <summary>
    /// Dexterity-based item capacity table
    /// </summary>
    private static readonly Dictionary<int, int> DexterityItemTable = new()
    {
        { 3, 5 }, { 4, 6 }, { 5, 7 }, { 6, 8 }, { 7, 9 },
        { 8, 10 }, { 9, 11 }, { 10, 12 }, { 11, 13 }, { 12, 14 },
        { 13, 15 }, { 14, 16 }, { 15, 17 }, { 16, 20 }, { 17, 22 },
        { 18, 25 }, { 19, 28 }, { 20, 30 }, { 21, 32 }, { 22, 34 },
        { 23, 36 }, { 24, 38 }, { 25, 35 }
    };

    public InventoryManager(IPlayer player)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
    }

    public EquipmentOperationResult AddItem(WorldObject item)
    {
        if (item == null)
            return EquipmentOperationResult.CreateFailure("Invalid item.");
        
        var inventory = _player.GetInventory();
        
        // Check weight capacity
        var currentWeight = GetCurrentWeight();
        var maxWeight = GetMaxWeightCapacity();
        if (currentWeight + item.Weight > maxWeight)
        {
            return EquipmentOperationResult.CreateFailure("That item is too heavy for you to carry.");
        }
        
        // Check item count capacity
        var maxItems = GetMaxItemCapacity();
        if (inventory.Count >= maxItems)
        {
            return EquipmentOperationResult.CreateFailure("Your hands are full, you can't carry any more items.");
        }
        
        // Add item to inventory
        inventory.Add(item);
        
        return EquipmentOperationResult.CreateSuccess($"You get {item.ShortDescription}.");
    }

    public EquipmentOperationResult RemoveItem(WorldObject item)
    {
        if (item == null)
            return EquipmentOperationResult.CreateFailure("Invalid item.");
        
        var inventory = _player.GetInventory();
        
        if (!inventory.Contains(item))
        {
            return EquipmentOperationResult.CreateFailure("You don't have that item.");
        }
        
        inventory.Remove(item);
        
        return EquipmentOperationResult.CreateSuccess($"You drop {item.ShortDescription}.");
    }

    public bool HasItem(int vnum)
    {
        return _player.GetInventory().Any(item => item.VirtualNumber == vnum);
    }

    public int GetCurrentWeight()
    {
        return _player.GetInventory().Sum(item => item.Weight);
    }

    public int GetMaxWeightCapacity()
    {
        var strength = _player.Strength;
        
        // Handle out-of-range values
        if (strength > 25) strength = 25;
        if (strength < 3) strength = 3;
        
        return StrengthWeightTable.GetValueOrDefault(strength, 100);
    }

    public int GetMaxItemCapacity()
    {
        var dexterity = _player.Dexterity;
        
        // Handle out-of-range values
        if (dexterity > 25) dexterity = 25;
        if (dexterity < 3) dexterity = 3;
        
        return DexterityItemTable.GetValueOrDefault(dexterity, 15);
    }

    public EquipmentOperationResult PutItemInContainer(WorldObject item, WorldObject container)
    {
        if (item == null || container == null)
            return EquipmentOperationResult.CreateFailure("Invalid item or container.");
        
        var inventory = _player.GetInventory();
        
        // Check if player has both items
        if (!inventory.Contains(item))
        {
            return EquipmentOperationResult.CreateFailure("You don't have that item.");
        }
        
        if (!inventory.Contains(container))
        {
            return EquipmentOperationResult.CreateFailure("You don't have that container.");
        }
        
        // Check if target is actually a container
        if (container.ObjectType != ObjectType.CONTAINER)
        {
            return EquipmentOperationResult.CreateFailure("That's not a container.");
        }
        
        // Check container capacity
        var containerCapacity = container.Values[0]; // Container capacity in first value
        var currentContainerWeight = container.Contents.Sum(obj => obj.Weight);
        
        if (currentContainerWeight + item.Weight > containerCapacity)
        {
            return EquipmentOperationResult.CreateFailure("That item won't fit in the container.");
        }
        
        // Move item from inventory to container
        inventory.Remove(item);
        container.Contents.Add(item);
        
        return EquipmentOperationResult.CreateSuccess($"You put {item.ShortDescription} in {container.ShortDescription}.");
    }

    public EquipmentOperationResult GetItemFromContainer(WorldObject item, WorldObject container)
    {
        if (item == null || container == null)
            return EquipmentOperationResult.CreateFailure("Invalid item or container.");
        
        var inventory = _player.GetInventory();
        
        // Check if player has the container
        if (!inventory.Contains(container))
        {
            return EquipmentOperationResult.CreateFailure("You don't have that container.");
        }
        
        // Check if container has the item
        if (!container.Contents.Contains(item))
        {
            return EquipmentOperationResult.CreateFailure($"There is no {item.ShortDescription} in {container.ShortDescription}.");
        }
        
        // Check weight capacity
        if (GetCurrentWeight() + item.Weight > GetMaxWeightCapacity())
        {
            return EquipmentOperationResult.CreateFailure("That item is too heavy for you to carry.");
        }
        
        // Check item count capacity
        if (inventory.Count >= GetMaxItemCapacity())
        {
            return EquipmentOperationResult.CreateFailure("Your hands are full, you can't carry any more items.");
        }
        
        // Move item from container to inventory
        container.Contents.Remove(item);
        inventory.Add(item);
        
        return EquipmentOperationResult.CreateSuccess($"You get {item.ShortDescription} from {container.ShortDescription}.");
    }

    public List<WorldObject> ListContainerContents(WorldObject container)
    {
        if (container == null || container.ObjectType != ObjectType.CONTAINER)
            return new List<WorldObject>();
        
        return new List<WorldObject>(container.Contents);
    }

    public EquipmentOperationResult DropItemInRoom(WorldObject item, Room room)
    {
        if (item == null || room == null)
            return EquipmentOperationResult.CreateFailure("Invalid item or room.");
        
        var inventory = _player.GetInventory();
        
        if (!inventory.Contains(item))
        {
            return EquipmentOperationResult.CreateFailure("You don't have that item.");
        }
        
        // Check for NODROP flag using modern C# enum patterns
        var extraFlags = (ExtraFlags)item.ExtraFlags;
        if (extraFlags.HasFlag(ExtraFlags.NODROP))
        {
            return EquipmentOperationResult.CreateFailure("You can't drop that item.");
        }
        
        // Remove from inventory and add to room
        inventory.Remove(item);
        room.Items.Add(item);
        
        return EquipmentOperationResult.CreateSuccess($"You drop {item.ShortDescription}.");
    }

    public EquipmentOperationResult GetItemFromRoom(WorldObject item, Room room)
    {
        if (item == null || room == null)
            return EquipmentOperationResult.CreateFailure("Invalid item or room.");
        
        if (!room.Items.Contains(item))
        {
            return EquipmentOperationResult.CreateFailure($"There is no {item.ShortDescription} here.");
        }
        
        // Check weight and item capacity before picking up
        if (GetCurrentWeight() + item.Weight > GetMaxWeightCapacity())
        {
            return EquipmentOperationResult.CreateFailure("That item is too heavy for you to carry.");
        }
        
        var inventory = _player.GetInventory();
        if (inventory.Count >= GetMaxItemCapacity())
        {
            return EquipmentOperationResult.CreateFailure("Your hands are full, you can't carry any more items.");
        }
        
        // Move item from room to inventory
        room.Items.Remove(item);
        inventory.Add(item);
        
        return EquipmentOperationResult.CreateSuccess($"You get {item.ShortDescription}.");
    }

    public EquipmentOperationResult AddGold(int amount)
    {
        if (amount <= 0)
            return EquipmentOperationResult.CreateFailure("Invalid amount.");
        
        _player.Gold += amount;
        
        return EquipmentOperationResult.CreateSuccess($"You receive {amount} gold coins.");
    }

    public EquipmentOperationResult SpendGold(int amount)
    {
        if (amount <= 0)
            return EquipmentOperationResult.CreateFailure("Invalid amount.");
        
        if (_player.Gold < amount)
        {
            return EquipmentOperationResult.CreateFailure("You don't have enough gold.");
        }
        
        _player.Gold -= amount;
        
        return EquipmentOperationResult.CreateSuccess($"You spend {amount} gold coins.");
    }

    public EquipmentOperationResult DropGold(int amount, Room room)
    {
        if (amount <= 0 || room == null)
            return EquipmentOperationResult.CreateFailure("Invalid amount or room.");
        
        if (_player.Gold < amount)
        {
            return EquipmentOperationResult.CreateFailure("You don't have enough gold.");
        }
        
        // Deduct gold from player
        _player.Gold -= amount;
        
        // Create a gold pile object
        var goldPile = CreateGoldPile(amount);
        room.Items.Add(goldPile);
        
        return EquipmentOperationResult.CreateSuccess($"You drop {amount} gold coins.");
    }

    public EquipmentOperationResult GetGoldFromRoom(WorldObject goldPile, Room room)
    {
        if (goldPile == null || room == null)
            return EquipmentOperationResult.CreateFailure("Invalid gold pile or room.");
        
        if (!room.Items.Contains(goldPile))
        {
            return EquipmentOperationResult.CreateFailure("There are no gold coins here.");
        }
        
        if (goldPile.ObjectType != ObjectType.MONEY)
        {
            return EquipmentOperationResult.CreateFailure("That's not gold.");
        }
        
        var amount = goldPile.Gold;
        
        // Remove gold pile from room and add gold to player
        room.Items.Remove(goldPile);
        _player.Gold += amount;
        
        return EquipmentOperationResult.CreateSuccess($"You get {amount} gold coins.");
    }

    public WorldObject? FindItem(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return null;
        
        var inventory = _player.GetInventory();
        
        // Handle dot notation (e.g., "2.sword" for second sword)
        var dotMatch = Regex.Match(keyword, @"^(\d+)\.(.+)$");
        if (dotMatch.Success)
        {
            var number = int.Parse(dotMatch.Groups[1].Value);
            var searchKeyword = dotMatch.Groups[2].Value;
            
            var matches = inventory.Where(item => 
                item.Name.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase) ||
                item.ShortDescription.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (number <= matches.Count && number > 0)
            {
                return matches[number - 1]; // Convert to 0-based index
            }
            
            return null;
        }
        
        // Simple keyword search (case-insensitive)
        return inventory.FirstOrDefault(item => 
            item.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            item.ShortDescription.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    public string GetInventoryDisplay()
    {
        var inventory = _player.GetInventory();
        
        if (!inventory.Any())
        {
            return "You are not carrying anything.";
        }
        
        var sb = new StringBuilder();
        sb.AppendLine("You are carrying:");
        
        foreach (var item in inventory)
        {
            sb.AppendLine(item.ShortDescription);
        }
        
        return sb.ToString();
    }
    
    #region Helper Methods
    
    private WorldObject CreateGoldPile(int amount)
    {
        return new WorldObject
        {
            VirtualNumber = 0, // Special vnum for gold
            Name = "coins gold",
            ShortDescription = $"{amount} gold coins",
            LongDescription = $"{amount} gold coins are scattered here.",
            ObjectType = ObjectType.MONEY,
            Weight = 0, // Gold has no weight
            Cost = 0,
            Gold = amount
        };
    }
    
    #endregion
}