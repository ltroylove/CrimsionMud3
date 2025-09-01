namespace C3Mud.Core.Players;

/// <summary>
/// Player positions based on original MUD position values
/// Used for command requirements and game mechanics
/// </summary>
public enum PlayerPosition : byte
{
    /// <summary>
    /// Player is dead
    /// </summary>
    Dead = 0,
    
    /// <summary>
    /// Player is mortally wounded
    /// </summary>
    MortallyWounded = 1,
    
    /// <summary>
    /// Player is incapacitated
    /// </summary>
    Incapacitated = 2,
    
    /// <summary>
    /// Player is stunned
    /// </summary>
    Stunned = 3,
    
    /// <summary>
    /// Player is sleeping
    /// </summary>
    Sleeping = 4,
    
    /// <summary>
    /// Player is resting
    /// </summary>
    Resting = 5,
    
    /// <summary>
    /// Player is sitting
    /// </summary>
    Sitting = 6,
    
    /// <summary>
    /// Player is fighting
    /// </summary>
    Fighting = 7,
    
    /// <summary>
    /// Player is standing (normal position)
    /// </summary>
    Standing = 8
}