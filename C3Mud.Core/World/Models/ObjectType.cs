namespace C3Mud.Core.World.Models;

/// <summary>
/// Defines the types of objects in CircleMUD/DikuMUD
/// These correspond to the object type field in .obj files
/// </summary>
public enum ObjectType
{
    /// <summary>Undefined or invalid object type</summary>
    UNDEFINED = 0,
    
    /// <summary>Light source (torches, lanterns, etc.)</summary>
    LIGHT = 1,
    
    /// <summary>Scrolls with spells that can be read</summary>
    SCROLL = 2,
    
    /// <summary>Magical wands that cast spells</summary>
    WAND = 3,
    
    /// <summary>Magical staffs that cast spells</summary>
    STAFF = 4,
    
    /// <summary>Weapons for combat</summary>
    WEAPON = 5,
    
    /// <summary>Furniture that can be sat on, slept in, etc.</summary>
    FURNITURE = 6,
    
    /// <summary>Miscellaneous items with no special properties</summary>
    TRASH = 7,
    
    /// <summary>Containers that can hold other objects</summary>
    CONTAINER = 8,
    
    /// <summary>Note/book that can be written on and read</summary>
    NOTE = 9,
    
    /// <summary>Liquid containers (drinks, potions)</summary>
    DRINK_CON = 10,
    
    /// <summary>Keys that open locks</summary>
    KEY = 11,
    
    /// <summary>Food that can be eaten</summary>
    FOOD = 12,
    
    /// <summary>Money/coins</summary>
    MONEY = 13,
    
    /// <summary>Pens for writing on notes</summary>
    PEN = 14,
    
    /// <summary>Boats for water travel</summary>
    BOAT = 15,
    
    /// <summary>Fountains that provide water</summary>
    FOUNTAIN = 16,
    
    /// <summary>Armor worn to protect body parts</summary>
    ARMOR = 17,
    
    /// <summary>Potions that can be quaffed</summary>
    POTION = 18,
    
    /// <summary>Worn items (jewelry, clothing)</summary>
    WORN = 19,
    
    /// <summary>Other miscellaneous items</summary>
    OTHER = 20,
    
    /// <summary>Portal objects for transportation</summary>
    PORTAL = 21,
    
    /// <summary>Boards for posting messages</summary>
    BOARD = 22,
    
    /// <summary>Corpses (special container type)</summary>
    CORPSE = 23,
    
    /// <summary>Magical components for spells</summary>
    COMPONENT = 24,
    
    /// <summary>Instruments for making music</summary>
    INSTRUMENT = 25
}