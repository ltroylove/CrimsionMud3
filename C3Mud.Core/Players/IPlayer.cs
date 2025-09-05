using C3Mud.Core.Networking;
using C3Mud.Core.Players.Models;
using C3Mud.Core.World.Models;

namespace C3Mud.Core.Players;

/// <summary>
/// Represents a player in the MUD
/// Based on original char_data structure from structs.h
/// </summary>
public interface IPlayer
{
    /// <summary>
    /// Player's unique ID
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Player's name
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Player's current level
    /// </summary>
    int Level { get; }
    
    /// <summary>
    /// Player's current position
    /// </summary>
    PlayerPosition Position { get; set; }
    
    /// <summary>
    /// Whether player is connected
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Player's connection descriptor
    /// </summary>
    IConnectionDescriptor? Connection { get; }
    
    /// <summary>
    /// Player's current room virtual number
    /// </summary>
    int CurrentRoomVnum { get; set; }
    
    /// <summary>
    /// When the player last moved (for movement delay checking)
    /// </summary>
    DateTime LastMovementTime { get; set; }
    
    /// <summary>
    /// Whether the player can fly
    /// </summary>
    bool CanFly { get; }
    
    /// <summary>
    /// Whether the player has a light source
    /// </summary>
    bool HasLight { get; }
    
    /// <summary>
    /// Check if player has a specific item by vnum
    /// </summary>
    /// <param name="itemVnum">Virtual number of the item to check</param>
    /// <returns>True if player has the item</returns>
    bool HasItem(int itemVnum);
    
    // Combat-related properties
    /// <summary>
    /// Current hit points
    /// </summary>
    int HitPoints { get; set; }
    
    /// <summary>
    /// Maximum hit points
    /// </summary>
    int MaxHitPoints { get; set; }
    
    /// <summary>
    /// Whether player is currently in combat
    /// </summary>
    bool IsInCombat { get; }
    
    /// <summary>
    /// Player's strength attribute
    /// </summary>
    int Strength { get; }
    
    /// <summary>
    /// Player's dexterity attribute
    /// </summary>
    int Dexterity { get; }
    
    /// <summary>
    /// Player's constitution attribute
    /// </summary>
    int Constitution { get; }
    
    /// <summary>
    /// Player's armor class (lower is better)
    /// </summary>
    int ArmorClass { get; }
    
    /// <summary>
    /// Player's experience points
    /// </summary>
    int ExperiencePoints { get; set; }
    
    /// <summary>
    /// Player's gold
    /// </summary>
    int Gold { get; set; }
    
    /// <summary>
    /// Count of recent deaths (for resurrection penalties)
    /// </summary>
    int RecentDeathCount { get; }
    
    /// <summary>
    /// Get wielded weapon
    /// </summary>
    /// <returns>Currently wielded weapon or null</returns>
    WorldObject? GetWieldedWeapon();
    
    /// <summary>
    /// Get equipped item in specific slot
    /// </summary>
    /// <param name="slot">Equipment slot to check</param>
    /// <returns>Equipped item or null</returns>
    WorldObject? GetEquippedItem(EquipmentSlot slot);
    
    /// <summary>
    /// Get skill level for a specific skill
    /// </summary>
    /// <param name="skillName">Name of the skill</param>
    /// <returns>Skill level (0-100)</returns>
    int GetSkillLevel(string skillName);
    
    /// <summary>
    /// Get player's inventory
    /// </summary>
    /// <returns>List of items in inventory</returns>
    List<WorldObject> GetInventory();
    
    /// <summary>
    /// Legacy player file data containing all character statistics
    /// Used for score display and character persistence
    /// </summary>
    LegacyPlayerFileData LegacyPlayerFileData { get; set; }
    
    /// <summary>
    /// Send a message to the player
    /// </summary>
    Task SendMessageAsync(string message);
    
    /// <summary>
    /// Send a formatted message to the player with color codes
    /// </summary>
    Task SendFormattedMessageAsync(string message);
    
    /// <summary>
    /// Disconnect the player
    /// </summary>
    Task DisconnectAsync(string reason = "Goodbye!");
}