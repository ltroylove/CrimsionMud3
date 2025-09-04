using System;

namespace C3Mud.Core.World.Models;

/// <summary>
/// Room flags bitfield values from CircleMUD/DikuMUD format
/// These flags define special properties of a room
/// </summary>
[Flags]
public enum RoomFlags
{
    /// <summary>
    /// No special flags
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Room is dark (players need light source)
    /// </summary>
    Dark = 1,
    
    /// <summary>
    /// Death trap - players die when entering
    /// </summary>
    Death = 2,
    
    /// <summary>
    /// No mobiles allowed in this room
    /// </summary>
    NoMob = 4,
    
    /// <summary>
    /// Room is indoors (affects weather, light, etc.)
    /// </summary>
    Indoors = 8,
    
    /// <summary>
    /// Peaceful room - no fighting allowed
    /// </summary>
    Peaceful = 16,
    
    /// <summary>
    /// No summon allowed to/from this room
    /// </summary>
    NoSummon = 32,
    
    /// <summary>
    /// No teleport/portal allowed to this room
    /// </summary>
    NoTeleport = 64,
    
    /// <summary>
    /// Private room - limited number of players
    /// </summary>
    Private = 128,
    
    /// <summary>
    /// God room - only immortals can enter
    /// </summary>
    GodRoom = 256,
    
    /// <summary>
    /// House room - player housing
    /// </summary>
    House = 512,
    
    /// <summary>
    /// House crash - house save on crash
    /// </summary>
    HouseCrash = 1024,
    
    /// <summary>
    /// Atmosphere room - has special atmosphere
    /// </summary>
    Atrium = 2048,
    
    /// <summary>
    /// Clan room - restricted to clan members
    /// </summary>
    Clan = 4096
}