using System;

namespace C3Mud.Core.World.Models;

/// <summary>
/// Bitfield flags defining where an object can be worn or how it can be used
/// These correspond to the wear flags in CircleMUD/DikuMUD .obj files
/// </summary>
[Flags]
public enum WearFlags : long
{
    /// <summary>Object can be taken/picked up</summary>
    TAKE = 1L << 0,          // 1
    
    /// <summary>Can be worn on finger</summary>
    FINGER = 1L << 1,        // 2
    
    /// <summary>Can be worn around neck</summary>
    NECK = 1L << 2,          // 4
    
    /// <summary>Can be worn on body/torso</summary>
    BODY = 1L << 3,          // 8
    
    /// <summary>Can be worn on head</summary>
    HEAD = 1L << 4,          // 16
    
    /// <summary>Can be worn on legs</summary>
    LEGS = 1L << 5,          // 32
    
    /// <summary>Can be worn on feet</summary>
    FEET = 1L << 6,          // 64
    
    /// <summary>Can be worn on hands</summary>
    HANDS = 1L << 7,         // 128
    
    /// <summary>Can be worn on arms</summary>
    ARMS = 1L << 8,          // 256
    
    /// <summary>Can be used as a shield</summary>
    SHIELD = 1L << 9,        // 512
    
    /// <summary>Can be worn about body (cloak, robe)</summary>
    ABOUT = 1L << 10,        // 1024
    
    /// <summary>Can be worn around waist</summary>
    WAIST = 1L << 11,        // 2048
    
    /// <summary>Can be worn around wrist</summary>
    WRIST = 1L << 12,        // 4096
    
    /// <summary>Can be wielded as weapon</summary>
    WIELD = 1L << 13,        // 8192
    
    /// <summary>Can be held in hand</summary>
    HOLD = 1L << 14,         // 16384
    
    /// <summary>Can be thrown</summary>
    THROW = 1L << 15,        // 32768
    
    /// <summary>Can be worn on shoulder</summary>
    SHOULDER = 1L << 16,     // 65536
    
    /// <summary>Can be worn around ankle</summary>
    ANKLE = 1L << 17,        // 131072
    
    /// <summary>Can be worn as earring</summary>
    EAR = 1L << 18,          // 262144
    
    /// <summary>Can be worn over eyes</summary>
    EYES = 1L << 19,         // 524288
    
    /// <summary>Can be worn on face (mask)</summary>
    FACE = 1L << 20,         // 1048576
    
    /// <summary>Can be worn in hair</summary>
    HAIR = 1L << 21,         // 2097152
    
    /// <summary>Can be worn as badge</summary>
    BADGE = 1L << 22,        // 4194304
    
    /// <summary>Can be worn on back</summary>
    BACK = 1L << 23,         // 8388608
    
    /// <summary>Can be wielded as two-handed weapon</summary>
    TWOHANDS = 1L << 24      // 16777216
}