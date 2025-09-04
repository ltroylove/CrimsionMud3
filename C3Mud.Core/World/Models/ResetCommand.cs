namespace C3Mud.Core.World.Models;

/// <summary>
/// Represents a zone reset command that specifies how to spawn mobs, objects, and set doors
/// </summary>
public class ResetCommand
{
    /// <summary>
    /// Type of reset command (M=mobile, O=object, E=equip, G=give, D=door, P=put)
    /// </summary>
    public ResetCommandType CommandType { get; set; }
    
    /// <summary>
    /// Command argument 1 - usually 0 for conditional resets, 1 for forced
    /// </summary>
    public int Arg1 { get; set; }
    
    /// <summary>
    /// Command argument 2 - virtual number (mobile/object vnum or room vnum for doors)
    /// </summary>
    public int Arg2 { get; set; }
    
    /// <summary>
    /// Command argument 3 - limit (max existing) or wear position for equipment
    /// </summary>
    public int Arg3 { get; set; }
    
    /// <summary>
    /// Command argument 4 - room vnum for spawns, direction for doors, container for puts
    /// </summary>
    public int Arg4 { get; set; }
    
    /// <summary>
    /// Command argument 5 - door state for door commands
    /// </summary>
    public int Arg5 { get; set; }
    
    /// <summary>
    /// Nesting level for dependent commands (equipment depends on mobile, etc.)
    /// </summary>
    public int NestingLevel { get; set; }
    
    /// <summary>
    /// Comment or description for this command
    /// </summary>
    public string Comment { get; set; } = string.Empty;
}

/// <summary>
/// Types of zone reset commands
/// </summary>
public enum ResetCommandType
{
    /// <summary>
    /// Load mobile (M command)
    /// Format: M arg1 mobile_vnum limit room_vnum
    /// </summary>
    Mobile = 'M',
    
    /// <summary>
    /// Load object in room (O command)  
    /// Format: O arg1 object_vnum limit room_vnum
    /// </summary>
    Object = 'O',
    
    /// <summary>
    /// Equip object on mobile (E command)
    /// Format: E arg1 object_vnum limit wear_position
    /// </summary>
    Equip = 'E',
    
    /// <summary>
    /// Give object to mobile (G command)
    /// Format: G arg1 object_vnum limit unused
    /// </summary>
    Give = 'G',
    
    /// <summary>
    /// Set door state (D command)
    /// Format: D arg1 room_vnum direction state
    /// </summary>
    Door = 'D',
    
    /// <summary>
    /// Put object in container (P command)
    /// Format: P arg1 object_vnum limit container_vnum
    /// </summary>
    Put = 'P',
    
    /// <summary>
    /// Remove object (R command)
    /// Format: R arg1 room_vnum object_vnum unused
    /// </summary>
    Remove = 'R'
}

/// <summary>
/// Door states for door reset commands
/// </summary>
public enum DoorState
{
    /// <summary>
    /// Door is open
    /// </summary>
    Open = 0,
    
    /// <summary>
    /// Door is closed but not locked
    /// </summary>
    Closed = 1,
    
    /// <summary>
    /// Door is closed and locked
    /// </summary>
    Locked = 2
}