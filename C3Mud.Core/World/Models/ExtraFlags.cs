using System;

namespace C3Mud.Core.World.Models;

/// <summary>
/// Extra flags for objects defining special properties
/// Based on original CircleMUD ITEM_* flags
/// </summary>
[Flags]
public enum ExtraFlags : long
{
    /// <summary>No special flags</summary>
    None = 0,
    
    /// <summary>Item glows with magical light</summary>
    GLOW = 1L << 0,           // BIT0
    
    /// <summary>Item hums with magical energy</summary>
    HUM = 1L << 1,            // BIT1
    
    /// <summary>Item is cursed and cannot be removed easily</summary>
    CURSED = 1L << 2,         // BIT2
    
    /// <summary>Item cannot be dropped</summary>
    NODROP = 1L << 3,         // BIT3
    
    /// <summary>Item cannot be given to others</summary>
    NOGIVE = 1L << 4,         // BIT4
    
    /// <summary>Item cannot be rented at inns</summary>
    NORENT = 1L << 5,         // BIT5
    
    /// <summary>Item cannot be donated</summary>
    NODONATE = 1L << 6,       // BIT6
    
    /// <summary>Item cannot be sold to shopkeepers</summary>
    NOSELL = 1L << 7,         // BIT7
    
    /// <summary>Item is magical in nature</summary>
    MAGIC = 1L << 8,          // BIT8
    
    /// <summary>Item cannot be located by spells</summary>
    NOLOCATE = 1L << 9,       // BIT9
    
    /// <summary>Item cannot be used by magic users</summary>
    ANTI_MAGIC_USER = 1L << 10,  // BIT10
    
    /// <summary>Item cannot be used by clerics</summary>
    ANTI_CLERIC = 1L << 11,   // BIT11
    
    /// <summary>Item cannot be used by thieves</summary>
    ANTI_THIEF = 1L << 12,    // BIT12
    
    /// <summary>Item cannot be used by warriors</summary>
    ANTI_WARRIOR = 1L << 13,  // BIT13
    
    /// <summary>Item cannot be used by good aligned characters</summary>
    ANTI_GOOD = 1L << 14,     // BIT14
    
    /// <summary>Item cannot be used by evil aligned characters</summary>
    ANTI_EVIL = 1L << 15,     // BIT15
    
    /// <summary>Item cannot be used by neutral aligned characters</summary>
    ANTI_NEUTRAL = 1L << 16,  // BIT16
    
    /// <summary>Item requires two hands to wield</summary>
    TWO_HANDED = 1L << 17,    // BIT17
    
    /// <summary>Item is a light source</summary>
    LIGHT_SOURCE = 1L << 18,  // BIT18
    
    /// <summary>Item is invisible</summary>
    INVISIBLE = 1L << 19,     // BIT19
    
    /// <summary>Item is blessed</summary>
    BLESSED = 1L << 20,       // BIT20
    
    /// <summary>Item cannot be enchanted further</summary>
    NOENCHANT = 1L << 21,     // BIT21
    
    /// <summary>Item is a container that cannot be closed</summary>
    NO_CLOSE = 1L << 22,      // BIT22
    
    /// <summary>Item decays over time</summary>
    DECAY = 1L << 23,         // BIT23
    
    /// <summary>Item requires quest completion to use</summary>
    QUEST_ITEM = 1L << 24     // BIT24
}