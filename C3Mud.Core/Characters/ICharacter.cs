using C3Mud.Core.World.Models;
using C3Mud.Core.Characters.Models;
using C3Mud.Core.Players.Models;
using C3Mud.Core.Players;

namespace C3Mud.Core.Characters;

/// <summary>
/// Unified character interface for both players and mobiles (NPCs)
/// Based on original char_data structure from Crimson-2-MUD
/// </summary>
public interface ICharacter
{
    /// <summary>
    /// Character name
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Character number: -1 for players, mob index for NPCs
    /// Based on original nr field in char_data
    /// </summary>
    int Nr { get; set; }
    
    /// <summary>
    /// Type of character (Player, NPC, etc.)
    /// </summary>
    CharacterType Type { get; }
    
    /// <summary>
    /// Current room the character is in
    /// </summary>
    int CurrentRoomVnum { get; set; }
    
    /// <summary>
    /// Character's current position (standing, sitting, fighting, etc.)
    /// </summary>
    PlayerPosition Position { get; set; }
    
    // Combat Stats
    int HitPoints { get; set; }
    int MaxHitPoints { get; set; }
    int Level { get; }
    int ArmorClass { get; }
    int Strength { get; }
    int Dexterity { get; }
    int Constitution { get; }
    
    // Combat State
    ICharacter? Fighting { get; set; }
    bool IsInCombat { get; }
    
    // Original MUD type checks
    bool IsNPC => Type == CharacterType.NPC;
    bool IsPC => Type == CharacterType.Player;
    bool IsMob => IsNPC && Nr > -1;
    
    // Equipment and items
    WorldObject? GetWieldedWeapon();
    WorldObject? GetEquippedItem(EquipmentSlot slot);
    
    // Communication
    Task SendMessageAsync(string message);
    
    // Skills (for bash, kick, etc.)
    int GetSkillLevel(string skillName);
}