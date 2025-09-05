namespace C3Mud.Core.World.Models;

/// <summary>
/// Equipment slots where items can be worn or wielded
/// Based on original CircleMUD equipment positions
/// </summary>
public enum EquipmentSlot
{
    /// <summary>
    /// Light source (torch, lantern)
    /// </summary>
    Light = 0,

    /// <summary>
    /// Right finger ring
    /// </summary>
    FingerRight = 1,

    /// <summary>
    /// Left finger ring
    /// </summary>
    FingerLeft = 2,

    /// <summary>
    /// Neck jewelry (necklace, amulet)
    /// </summary>
    Neck1 = 3,

    /// <summary>
    /// Second neck slot
    /// </summary>
    Neck2 = 4,

    /// <summary>
    /// Body armor (shirt, robe, breastplate)
    /// </summary>
    Body = 5,

    /// <summary>
    /// Head armor (helmet, hat)
    /// </summary>
    Head = 6,

    /// <summary>
    /// Leg armor (pants, leggings)
    /// </summary>
    Legs = 7,

    /// <summary>
    /// Foot armor (boots, shoes)
    /// </summary>
    Feet = 8,

    /// <summary>
    /// Hand armor (gloves, gauntlets)
    /// </summary>
    Hands = 9,

    /// <summary>
    /// Arm armor (sleeves, bracers)
    /// </summary>
    Arms = 10,

    /// <summary>
    /// Shield
    /// </summary>
    Shield = 11,

    /// <summary>
    /// Cloak or cape about body
    /// </summary>
    About = 12,

    /// <summary>
    /// Waist armor (belt, sash)
    /// </summary>
    Waist = 13,

    /// <summary>
    /// Right wrist jewelry (bracelet)
    /// </summary>
    WristRight = 14,

    /// <summary>
    /// Left wrist jewelry (bracelet)
    /// </summary>
    WristLeft = 15,

    /// <summary>
    /// Primary weapon (right hand)
    /// </summary>
    Wield = 16,

    /// <summary>
    /// Held item (left hand when not using shield)
    /// </summary>
    Hold = 17,
    
    /// <summary>
    /// Tail slot (for races with tails)
    /// </summary>
    Tail = 18,
    
    /// <summary>
    /// First four-legs slot (for quadruped races)
    /// </summary>
    FourLegs1 = 19,
    
    /// <summary>
    /// Second four-legs slot (for quadruped races)
    /// </summary>
    FourLegs2 = 20
}