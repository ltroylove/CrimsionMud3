namespace C3Mud.Core.Characters.Models;

/// <summary>
/// Character classes based on original CircleMUD class system
/// Values must match original CLASS_* constants for compatibility
/// </summary>
public enum CharacterClass
{
    /// <summary>
    /// Magic User (Mage) - Primary spellcaster
    /// </summary>
    Mage = 0,
    
    /// <summary>
    /// Cleric - Healing and divine magic
    /// </summary>
    Cleric = 1,
    
    /// <summary>
    /// Thief - Stealth and skills
    /// </summary>
    Thief = 2,
    
    /// <summary>
    /// Warrior - Combat specialist
    /// </summary>
    Warrior = 3,
    
    /// <summary>
    /// Ranger - Nature warrior hybrid
    /// </summary>
    Ranger = 4,
    
    /// <summary>
    /// Paladin - Holy warrior
    /// </summary>
    Paladin = 5,
    
    /// <summary>
    /// Bard - Jack of all trades
    /// </summary>
    Bard = 6,
    
    /// <summary>
    /// Druid - Nature magic
    /// </summary>
    Druid = 7,
    
    /// <summary>
    /// Sorcerer - Inherent magic
    /// </summary>
    Sorcerer = 8,
    
    /// <summary>
    /// Monk - Martial arts specialist
    /// </summary>
    Monk = 9
}