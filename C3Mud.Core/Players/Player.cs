using System.Linq;
using C3Mud.Core.Networking;
using C3Mud.Core.Players.Models;
using C3Mud.Core.World.Models;
using C3Mud.Core.Characters;
using C3Mud.Core.Characters.Models;

namespace C3Mud.Core.Players;

/// <summary>
/// Basic player implementation
/// Based on original char_data structure from structs.h
/// </summary>
public class Player : IPlayer, ICharacter
{
    public string Id { get; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public PlayerPosition Position { get; set; } = PlayerPosition.Standing;
    public bool IsConnected => Connection?.IsConnected ?? false;
    public IConnectionDescriptor? Connection { get; set; }
    public int CurrentRoomVnum { get; set; } = 20385; // Default starting room from 15Rooms.wld
    public DateTime LastMovementTime { get; set; } = DateTime.MinValue;
    public bool CanFly { get; private set; } = false;
    public bool HasLight { get; private set; } = false;
    public LegacyPlayerFileData? LegacyPlayerFileData { get; set; }

    // ICharacter implementation
    public int Nr { get; set; } = -1; // Players always have Nr = -1
    public CharacterType Type => CharacterType.Player;
    public ICharacter? Fighting { get; set; }
    public bool IsInCombat => Fighting != null;
    
    // Equipment and inventory storage
    private readonly Dictionary<EquipmentSlot, WorldObject?> _equipment = new();
    private readonly List<WorldObject> _inventory = new();

    public Player(string id)
    {
        Id = id;
        
        // Initialize all equipment slots to null
        foreach (EquipmentSlot slot in Enum.GetValues<EquipmentSlot>())
        {
            _equipment[slot] = null;
        }
    }

    public Player(string id, IConnectionDescriptor connection) : this(id)
    {
        Connection = connection;
    }

    public async Task SendMessageAsync(string message)
    {
        if (Connection != null && IsConnected)
        {
            await Connection.SendDataAsync(message + "\r\n");
        }
    }

    public async Task SendFormattedMessageAsync(string message)
    {
        if (Connection != null && IsConnected)
        {
            // Process color codes through the telnet handler
            var processedMessage = Connection.TelnetHandler.ProcessColorCodes(message, Connection);
            await Connection.SendDataAsync(processedMessage + "\r\n");
        }
    }

    public async Task DisconnectAsync(string reason = "Goodbye!")
    {
        if (Connection != null && IsConnected)
        {
            await SendMessageAsync(reason);
            await Connection.CloseAsync();
        }
    }

    public bool HasItem(int itemVnum)
    {
        // Check equipped items first
        foreach (var equippedItem in _equipment.Values)
        {
            if (equippedItem != null && equippedItem.VirtualNumber == itemVnum)
                return true;
        }
        
        // Check inventory items
        foreach (var item in _inventory)
        {
            if (item.VirtualNumber == itemVnum)
                return true;
        }
        
        return false;
    }

    // Combat-related properties - stub implementations for TDD Red phase
    public int HitPoints { get; set; } = 100;
    public int MaxHitPoints { get; set; } = 100;
    public int Strength => LegacyPlayerFileData?.Abilities.Strength ?? 13; // Get from legacy data or default
    public int Dexterity => LegacyPlayerFileData?.Abilities.Dexterity ?? 13; // Get from legacy data or default
    public int Constitution => LegacyPlayerFileData?.Abilities.Constitution ?? 13; // Get from legacy data or default
    public int ArmorClass => 10; // Basic AC
    public int ExperiencePoints { get; set; } = 0;
    public int Gold { get; set; } = 0;
    public int RecentDeathCount => LegacyPlayerFileData?.Spare1 ?? 0; // Use spare1 field for death tracking

    public WorldObject? GetWieldedWeapon()
    {
        return _equipment.GetValueOrDefault(EquipmentSlot.Wield);
    }

    public WorldObject? GetEquippedItem(EquipmentSlot slot)
    {
        return _equipment.GetValueOrDefault(slot);
    }

    public int GetSkillLevel(string skillName)
    {
        // TODO: Implement skill system
        return 0; // No skills for now
    }

    public List<WorldObject> GetInventory()
    {
        return new List<WorldObject>(_inventory); // Return a copy to prevent external modification
    }

    public CharacterClass GetCharacterClass()
    {
        // Use LegacyPlayerFileData if available, otherwise default to Warrior
        if (LegacyPlayerFileData.HasValue)
        {
            return (CharacterClass)LegacyPlayerFileData.Value.Class;
        }
        return CharacterClass.Warrior;
    }

    public Alignment GetAlignment()
    {
        // Use LegacyPlayerFileData if available, otherwise default to Neutral
        if (LegacyPlayerFileData.HasValue)
        {
            var alignmentValue = LegacyPlayerFileData.Value.Alignment;
            if (alignmentValue <= -350)
                return Alignment.Evil;
            else if (alignmentValue >= 350)
                return Alignment.Good;
            else
                return Alignment.Neutral;
        }
        return Alignment.Neutral;
    }
    
    /// <summary>
    /// Internal method for equipment manager to set equipment
    /// </summary>
    /// <param name="slot">Equipment slot</param>
    /// <param name="item">Item to equip (null to unequip)</param>
    internal void SetEquippedItem(EquipmentSlot slot, WorldObject? item)
    {
        _equipment[slot] = item;
    }

    /// <summary>
    /// Add an item to the player's inventory
    /// </summary>
    /// <param name="item">Item to add</param>
    public void AddToInventory(WorldObject item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
            
        _inventory.Add(item);
    }
    
    /// <summary>
    /// Remove an item from the player's inventory
    /// </summary>
    /// <param name="item">Item to remove</param>
    /// <returns>True if the item was found and removed, false otherwise</returns>
    public bool RemoveFromInventory(WorldObject item)
    {
        if (item == null)
            return false;
            
        return _inventory.Remove(item);
    }
    
    /// <summary>
    /// Remove an item from inventory by virtual number
    /// </summary>
    /// <param name="vnum">Virtual number of item to remove</param>
    /// <returns>The removed item, or null if not found</returns>
    public WorldObject? RemoveFromInventory(int vnum)
    {
        var item = _inventory.FirstOrDefault(i => i.VirtualNumber == vnum);
        if (item != null)
        {
            _inventory.Remove(item);
        }
        return item;
    }
    
    /// <summary>
    /// Find an item in inventory or equipment by virtual number
    /// This matches CircleMUD's get_obj_list_vis behavior for searching player possessions
    /// </summary>
    /// <param name="vnum">Virtual number to search for</param>
    /// <returns>The found item, or null if not found</returns>
    public WorldObject? FindItem(int vnum)
    {
        // First check equipped items
        foreach (var equippedItem in _equipment.Values)
        {
            if (equippedItem != null && equippedItem.VirtualNumber == vnum)
                return equippedItem;
        }
        
        // Then check inventory
        return _inventory.FirstOrDefault(item => item.VirtualNumber == vnum);
    }
    
    /// <summary>
    /// Find an item in inventory or equipment by name keywords
    /// This matches CircleMUD's get_obj_list_vis behavior for name matching
    /// </summary>
    /// <param name="name">Name or keywords to search for</param>
    /// <returns>The found item, or null if not found</returns>
    public WorldObject? FindItem(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
            
        var searchName = name.Trim().ToLower();
        
        // First check equipped items
        foreach (var equippedItem in _equipment.Values)
        {
            if (equippedItem != null && IsNameMatch(searchName, equippedItem.Name))
                return equippedItem;
        }
        
        // Then check inventory
        return _inventory.FirstOrDefault(item => IsNameMatch(searchName, item.Name));
    }
    
    /// <summary>
    /// Matches name keywords similar to CircleMUD's isname() function
    /// </summary>
    /// <param name="searchName">Name being searched for (already lowercased)</param>
    /// <param name="itemName">Item's name string containing keywords</param>
    /// <returns>True if the search name matches any keyword in the item name</returns>
    private bool IsNameMatch(string searchName, string itemName)
    {
        if (string.IsNullOrWhiteSpace(searchName) || string.IsNullOrWhiteSpace(itemName))
            return false;
            
        var keywords = itemName.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var keyword in keywords)
        {
            if (keyword.StartsWith(searchName))
                return true;
        }
        
        return false;
    }

    public override bool Equals(object? obj)
    {
        return obj is Player other && Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return $"Player[{Id}]: {Name} (Level {Level})";
    }
}