using System;

namespace C3Mud.Core.World.Models;

/// <summary>
/// Bitfield flags defining special properties of objects
/// These correspond to the extra flags in CircleMUD/DikuMUD .obj files
/// </summary>
[Flags]
public enum ExtraFlags : long
{
    /// <summary>Object glows</summary>
    GLOW = 1L << 0,          // 1
    
    /// <summary>Object hums</summary>
    HUM = 1L << 1,           // 2
    
    /// <summary>Object cannot be dropped</summary>
    NODROP = 1L << 2,        // 4
    
    /// <summary>Object cannot be removed once worn</summary>
    NOREMOVE = 1L << 3,      // 8
    
    /// <summary>Object cannot be given to others</summary>
    NOGIVE = 1L << 4,        // 16
    
    /// <summary>Invisible object (can't be seen without detect invis)</summary>
    INVISIBLE = 1L << 5,     // 32
    
    /// <summary>Magical object</summary>
    MAGIC = 1L << 6,         // 64
    
    /// <summary>Object cannot be cursed</summary>
    NOBLESS = 1L << 7,       // 128
    
    /// <summary>Anti-good alignment</summary>
    ANTI_GOOD = 1L << 8,     // 256
    
    /// <summary>Anti-evil alignment</summary>
    ANTI_EVIL = 1L << 9,     // 512
    
    /// <summary>Anti-neutral alignment</summary>
    ANTI_NEUTRAL = 1L << 10, // 1024
    
    /// <summary>Anti-magic-user class</summary>
    ANTI_MAGIC_USER = 1L << 11, // 2048
    
    /// <summary>Anti-cleric class</summary>
    ANTI_CLERIC = 1L << 12,  // 4096
    
    /// <summary>Anti-thief class</summary>
    ANTI_THIEF = 1L << 13,   // 8192
    
    /// <summary>Anti-warrior class</summary>
    ANTI_WARRIOR = 1L << 14, // 16384
    
    /// <summary>Cannot be sold to shops</summary>
    NOSELL = 1L << 15,       // 32768
    
    /// <summary>Object is cursed</summary>
    CURSED = 1L << 16,       // 65536
    
    /// <summary>Object is blessed</summary>
    BLESSED = 1L << 17,      // 131072
    
    /// <summary>Object cannot be located by spells</summary>
    NOLOCATE = 1L << 18,     // 262144
    
    /// <summary>Object is unique (only one in game)</summary>
    UNIQUE = 1L << 19,       // 524288
    
    /// <summary>Object decays over time</summary>
    DECAY = 1L << 20,        // 1048576
    
    /// <summary>Object cannot be stolen</summary>
    NOSTEAL = 1L << 21,      // 2097152
    
    /// <summary>Object has special properties</summary>
    SPECIAL = 1L << 22,      // 4194304
    
    /// <summary>Object is donated (from donation room)</summary>
    DONATED = 1L << 23,      // 8388608
    
    /// <summary>Object cannot be disarmed</summary>
    NODISARM = 1L << 24,     // 16777216
    
    /// <summary>Object cannot be enchanted</summary>
    NOENCHANT = 1L << 25,    // 33554432
    
    /// <summary>Object is a quest item</summary>
    QUEST = 1L << 26,        // 67108864
    
    /// <summary>Object is ethereal/ghostly</summary>
    ETHEREAL = 1L << 27,     // 134217728
    
    /// <summary>Object is anti-paladin class</summary>
    ANTI_PALADIN = 1L << 28, // 268435456
    
    /// <summary>Object is anti-ranger class</summary>
    ANTI_RANGER = 1L << 29   // 536870912
}