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
    public LegacyPlayerFileData LegacyPlayerFileData { get; set; }

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
        // TODO: Implement full inventory checking once inventory system is implemented
        // For Iteration 4 testing, we'll use a simple check
        // In a real implementation, this would check the player's inventory
        
        // For now, always return false unless it's a test key
        // This will be replaced with proper inventory system later
        return false;
    }

    // Combat-related properties - stub implementations for TDD Red phase
    public int HitPoints { get; set; } = 100;
    public int MaxHitPoints { get; set; } = 100;
    public int Strength => 13; // Default attribute values
    public int Dexterity => 13;
    public int Constitution => 13;
    public int ArmorClass => 10; // Basic AC
    public int ExperiencePoints { get; set; } = 0;
    public int Gold { get; set; } = 0;
    public int RecentDeathCount => 0; // TODO: Implement death tracking

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
        return _inventory;
    }

    public CharacterClass GetCharacterClass()
    {
        // TODO: Implement proper character class system
        // For now, return warrior as default
        return CharacterClass.Warrior;
    }

    public Alignment GetAlignment()
    {
        // TODO: Implement proper alignment system
        // For now, return neutral as default
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