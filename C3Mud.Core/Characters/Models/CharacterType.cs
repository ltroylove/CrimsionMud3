namespace C3Mud.Core.Characters.Models;

/// <summary>
/// Character types based on original MUD flags
/// Replaces IS_NPC, IS_PC checks from original code
/// </summary>
public enum CharacterType
{
    /// <summary>
    /// Player character (human controlled)
    /// </summary>
    Player = 0,
    
    /// <summary>
    /// Non-player character (NPC/Mobile)
    /// </summary>
    NPC = 1,
    
    /// <summary>
    /// Object/Item character (rarely used)
    /// </summary>
    Object = 2
}