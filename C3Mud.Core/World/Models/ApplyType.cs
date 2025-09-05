namespace C3Mud.Core.World.Models;

/// <summary>
/// Types of stat modifications that items can apply to characters
/// Based on original CircleMUD APPLY_* constants
/// </summary>
public enum ApplyType
{
    /// <summary>No effect</summary>
    NONE = 0,
    
    /// <summary>Strength attribute bonus/penalty</summary>
    STR = 1,
    
    /// <summary>Dexterity attribute bonus/penalty</summary>
    DEX = 2,
    
    /// <summary>Intelligence attribute bonus/penalty</summary>
    INT = 3,
    
    /// <summary>Wisdom attribute bonus/penalty</summary>
    WIS = 4,
    
    /// <summary>Constitution attribute bonus/penalty</summary>
    CON = 5,
    
    /// <summary>Charisma attribute bonus/penalty</summary>
    CHA = 6,
    
    /// <summary>Class (reserved, do not use)</summary>
    CLASS = 7,
    
    /// <summary>Level (reserved, do not use)</summary>
    LEVEL = 8,
    
    /// <summary>Age modification</summary>
    AGE = 9,
    
    /// <summary>Character weight modification</summary>
    CHAR_WEIGHT = 10,
    
    /// <summary>Character height modification</summary>
    CHAR_HEIGHT = 11,
    
    /// <summary>Mana points bonus/penalty</summary>
    MANA = 12,
    
    /// <summary>Hit points bonus/penalty</summary>
    HIT = 13,
    
    /// <summary>Movement points bonus/penalty</summary>
    MOVE = 14,
    
    /// <summary>Gold (money) bonus/penalty</summary>
    GOLD = 15,
    
    /// <summary>Experience points bonus/penalty</summary>
    EXP = 16,
    
    /// <summary>Armor class bonus/penalty (lower is better)</summary>
    AC = 17,
    
    /// <summary>Hit roll bonus/penalty (attack accuracy)</summary>
    HITROLL = 18,
    
    /// <summary>Damage roll bonus/penalty (damage output)</summary>
    DAMROLL = 19,
    
    /// <summary>Saving throw vs paralyzation/poison/death magic</summary>
    SAVING_PARA = 20,
    
    /// <summary>Saving throw vs rods/staves/wands</summary>
    SAVING_ROD = 21,
    
    /// <summary>Saving throw vs petrification/polymorph</summary>
    SAVING_PETRI = 22,
    
    /// <summary>Saving throw vs breath weapons</summary>
    SAVING_BREATH = 23,
    
    /// <summary>Saving throw vs spells</summary>
    SAVING_SPELL = 24,
    
    /// <summary>Extra attacks per round (CRITICAL: Only one item with this apply allowed)</summary>
    EXTRA_ATTACKS = 25
}