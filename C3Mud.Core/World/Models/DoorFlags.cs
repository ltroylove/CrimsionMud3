using System;

namespace C3Mud.Core.World.Models;

/// <summary>
/// Door flags for exits defining door state
/// Based on original CircleMUD EX_* flags
/// </summary>
[Flags]
public enum DoorFlags : int
{
    /// <summary>No door or open passage</summary>
    None = 0,
    
    /// <summary>Door exists (EX_ISDOOR)</summary>
    ISDOOR = 1 << 0,          // BIT0
    
    /// <summary>Door is closed (EX_CLOSED)</summary>
    CLOSED = 1 << 1,          // BIT1
    
    /// <summary>Door is locked (EX_LOCKED)</summary>
    LOCKED = 1 << 2,          // BIT2
    
    /// <summary>Door cannot be picked (EX_PICKPROOF)</summary>
    PICKPROOF = 1 << 3,       // BIT3
    
    /// <summary>Door automatically closes (EX_AUTOCLOSE)</summary>
    AUTOCLOSE = 1 << 4,       // BIT4
    
    /// <summary>Door automatically locks (EX_AUTOLOCK)</summary>
    AUTOLOCK = 1 << 5,        // BIT5
    
    /// <summary>Hidden door (EX_HIDDEN)</summary>
    HIDDEN = 1 << 6,          // BIT6
    
    /// <summary>One-way passage (EX_ONEWAY)</summary>
    ONEWAY = 1 << 7           // BIT7
}