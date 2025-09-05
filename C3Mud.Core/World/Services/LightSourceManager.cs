using C3Mud.Core.Players;
using C3Mud.Core.World.Models;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Manages light sources and room lighting
/// Based on original CircleMUD light source management
/// </summary>
public static class LightSourceManager
{
    /// <summary>
    /// Check if an object is an active light source
    /// Based on original CircleMUD light checking logic
    /// </summary>
    /// <param name="item">Item to check</param>
    /// <returns>True if item is an active light source</returns>
    public static bool IsActiveLightSource(WorldObject item)
    {
        if (item == null) return false;
        
        // Check if item is a light or quest light type
        if (item.ObjectType != ObjectType.LIGHT && item.ObjectType != ObjectType.QSTLIGHT)
            return false;
            
        // Check if light is ON (value[2] > 0)
        return item.Values.Length > 2 && item.Values[2] > 0;
    }
    
    /// <summary>
    /// Add light source to room when player equips or enters with light
    /// Based on original CircleMUD world[room].light++ logic
    /// </summary>
    /// <param name="player">Player with the light source</param>
    /// <param name="room">Room to add light to</param>
    /// <param name="worldDatabase">World database to update room</param>
    public static void AddLightSourceToRoom(IPlayer player, Room room, IWorldDatabase worldDatabase)
    {
        if (player == null || room == null) return;
        
        // Check if player has active light source equipped
        var equipment = player.GetEquipment();
        if (equipment.TryGetValue(EquipmentSlot.Light, out var lightItem) && lightItem != null)
        {
            if (IsActiveLightSource(lightItem))
            {
                room.LightSources++;
                // Update the room in the database
                // TODO: Implement room update in WorldDatabase
            }
        }
    }
    
    /// <summary>
    /// Remove light source from room when player unequips or leaves with light
    /// Based on original CircleMUD world[room].light-- logic
    /// </summary>
    /// <param name="player">Player with the light source</param>
    /// <param name="room">Room to remove light from</param>
    /// <param name="worldDatabase">World database to update room</param>
    public static void RemoveLightSourceFromRoom(IPlayer player, Room room, IWorldDatabase worldDatabase)
    {
        if (player == null || room == null) return;
        
        // Check if player has active light source equipped
        var equipment = player.GetEquipment();
        if (equipment.TryGetValue(EquipmentSlot.Light, out var lightItem) && lightItem != null)
        {
            if (IsActiveLightSource(lightItem))
            {
                room.LightSources = Math.Max(0, room.LightSources - 1);
                // Update the room in the database
                // TODO: Implement room update in WorldDatabase
            }
        }
    }
    
    /// <summary>
    /// Handle light source being equipped
    /// Based on original CircleMUD equip light source logic
    /// </summary>
    /// <param name="player">Player equipping the light</param>
    /// <param name="lightItem">Light source item</param>
    /// <param name="worldDatabase">World database</param>
    public static void OnLightSourceEquipped(IPlayer player, WorldObject lightItem, IWorldDatabase worldDatabase)
    {
        if (player == null || lightItem == null || worldDatabase == null) return;
        
        if (IsActiveLightSource(lightItem))
        {
            // Get player's current room
            var room = worldDatabase.GetRoomByVnum(player.CurrentRoomVnum);
            if (room != null)
            {
                room.LightSources++;
                // TODO: Implement room update in WorldDatabase
                // Update HasLight status for player
                UpdatePlayerLightStatus(player);
            }
        }
    }
    
    /// <summary>
    /// Handle light source being unequipped
    /// Based on original CircleMUD unequip light source logic
    /// </summary>
    /// <param name="player">Player unequipping the light</param>
    /// <param name="lightItem">Light source item</param>
    /// <param name="worldDatabase">World database</param>
    public static void OnLightSourceUnequipped(IPlayer player, WorldObject lightItem, IWorldDatabase worldDatabase)
    {
        if (player == null || lightItem == null || worldDatabase == null) return;
        
        if (IsActiveLightSource(lightItem))
        {
            // Get player's current room
            var room = worldDatabase.GetRoomByVnum(player.CurrentRoomVnum);
            if (room != null)
            {
                room.LightSources = Math.Max(0, room.LightSources - 1);
                // TODO: Implement room update in WorldDatabase
                // Update HasLight status for player
                UpdatePlayerLightStatus(player);
            }
        }
    }
    
    /// <summary>
    /// Update player's HasLight status based on equipped light sources
    /// </summary>
    /// <param name="player">Player to update</param>
    private static void UpdatePlayerLightStatus(IPlayer player)
    {
        var equipment = player.GetEquipment();
        var hasLight = false;
        
        if (equipment.TryGetValue(EquipmentSlot.Light, out var lightItem) && lightItem != null)
        {
            hasLight = IsActiveLightSource(lightItem);
        }
        
        // TODO: Set player.HasLight = hasLight when Player class supports it
    }
    
    /// <summary>
    /// Check if a room has sufficient light for visibility
    /// Based on original CircleMUD room visibility logic
    /// </summary>
    /// <param name="room">Room to check</param>
    /// <param name="player">Player checking visibility</param>
    /// <returns>True if room has sufficient light</returns>
    public static bool HasSufficientLight(Room room, IPlayer player)
    {
        if (room == null) return false;
        
        // Check if room has inherent light (outdoor, lit room flag, etc.)
        if (room.LightLevel > 0 || room.LightSources > 0)
            return true;
            
        // Check if player has personal light source
        var equipment = player.GetEquipment();
        if (equipment.TryGetValue(EquipmentSlot.Light, out var lightItem) && lightItem != null)
        {
            return IsActiveLightSource(lightItem);
        }
        
        return false;
    }
}