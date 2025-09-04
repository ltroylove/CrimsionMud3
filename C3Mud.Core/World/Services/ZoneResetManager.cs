using C3Mud.Core.World.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Manages zone resets including mob/object spawning and door state management
/// </summary>
public class ZoneResetManager : IZoneResetManager
{
    private readonly IWorldDatabase _worldDatabase;
    private readonly IMobileDatabase _mobileDatabase;
    private readonly IObjectDatabase _objectDatabase;
    private readonly ILogger<ZoneResetManager>? _logger;
    
    // Track context for dependent commands (equipment, give, put)
    private Mobile? _lastSpawnedMobile;
    private WorldObject? _lastSpawnedObject;
    
    public ZoneResetManager(
        IWorldDatabase worldDatabase,
        IMobileDatabase mobileDatabase,
        IObjectDatabase objectDatabase,
        ILogger<ZoneResetManager>? logger = null)
    {
        _worldDatabase = worldDatabase;
        _mobileDatabase = mobileDatabase;
        _objectDatabase = objectDatabase;
        _logger = logger;
    }
    
    /// <summary>
    /// Executes a zone reset, processing all reset commands in order
    /// </summary>
    public ZoneResetResult ExecuteReset(Zone zone)
    {
        var result = new ZoneResetResult { Success = true };
        _lastSpawnedMobile = null;
        _lastSpawnedObject = null;
        
        try
        {
            _logger?.LogInformation("Starting reset for zone {ZoneNumber} ({ZoneName})", zone.VirtualNumber, zone.Name);
            
            foreach (var command in zone.ResetCommands)
            {
                var executed = ExecuteResetCommand(command, zone, result);
                if (executed)
                    result.CommandsExecuted++;
                    
                // If any command failed, don't update zone reset time
                if (!result.Success)
                    break;
            }
            
            // Only update zone reset time if all commands succeeded
            if (result.Success)
            {
                zone.LastReset = DateTime.UtcNow;
                zone.Age = 0;
            }
            
            _logger?.LogInformation("Zone {ZoneNumber} reset completed. {CommandsExecuted} commands executed.", 
                zone.VirtualNumber, result.CommandsExecuted);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger?.LogError(ex, "Error resetting zone {ZoneNumber}", zone.VirtualNumber);
        }
        
        return result;
    }
    
    /// <summary>
    /// Executes a single reset command
    /// </summary>
    private bool ExecuteResetCommand(ResetCommand command, Zone zone, ZoneResetResult result)
    {
        try
        {
            return command.CommandType switch
            {
                ResetCommandType.Mobile => ExecuteMobileCommand(command, zone, result),
                ResetCommandType.Object => ExecuteObjectCommand(command, zone, result),
                ResetCommandType.Equip => ExecuteEquipCommand(command, zone, result),
                ResetCommandType.Give => ExecuteGiveCommand(command, zone, result),
                ResetCommandType.Door => ExecuteDoorCommand(command, zone, result),
                ResetCommandType.Put => ExecutePutCommand(command, zone, result),
                ResetCommandType.Remove => ExecuteRemoveCommand(command, zone, result),
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to execute reset command {CommandType} in zone {ZoneNumber}", 
                command.CommandType, zone.VirtualNumber);
            return false;
        }
    }
    
    /// <summary>
    /// Executes mobile spawn command: M arg1 mobile_vnum limit room_vnum
    /// </summary>
    private bool ExecuteMobileCommand(ResetCommand command, Zone zone, ZoneResetResult result)
    {
        var mobileVnum = command.Arg2;
        var maxExisting = command.Arg3;
        var roomVnum = command.Arg4;
        
        // Check if we've reached the spawn limit
        if (command.Arg1 == 0 && _worldDatabase.CountMobilesInZone(zone.VirtualNumber, mobileVnum) >= maxExisting)
        {
            return false; // Don't spawn if at limit
        }
        
        // Get room and mobile prototype
        var room = _worldDatabase.GetRoom(roomVnum);
        if (room == null)
        {
            result.Success = false;
            result.ErrorMessage = $"Room {roomVnum} not found for mobile spawn";
            return false;
        }
        
        var mobilePrototype = _mobileDatabase.GetMobile(mobileVnum);
        if (mobilePrototype == null)
        {
            result.Success = false;
            result.ErrorMessage = $"Mobile prototype {mobileVnum} not found";
            _logger?.LogWarning("Mobile prototype {MobileVnum} not found", mobileVnum);
            return false;
        }
        
        // Spawn the mobile
        _lastSpawnedMobile = _worldDatabase.SpawnMobile(mobilePrototype, room);
        result.ResetLog.Add($"Spawned mobile {mobileVnum} in room {roomVnum}");
        
        return true;
    }
    
    /// <summary>
    /// Executes object load command: O arg1 object_vnum limit room_vnum
    /// </summary>
    private bool ExecuteObjectCommand(ResetCommand command, Zone zone, ZoneResetResult result)
    {
        var objectVnum = command.Arg2;
        var maxExisting = command.Arg3;
        var roomVnum = command.Arg4;
        
        // Check if we've reached the spawn limit
        if (command.Arg1 == 0 && _worldDatabase.CountObjectsInZone(zone.VirtualNumber, objectVnum) >= maxExisting)
        {
            return false; // Don't spawn if at limit
        }
        
        // Get room and object prototype
        var room = _worldDatabase.GetRoom(roomVnum);
        if (room == null)
        {
            result.Success = false;
            result.ErrorMessage = $"Room {roomVnum} not found for object spawn";
            return false;
        }
        
        var objectPrototype = _objectDatabase.GetObject(objectVnum);
        if (objectPrototype == null)
        {
            _logger?.LogWarning("Object prototype {ObjectVnum} not found", objectVnum);
            return false;
        }
        
        // Spawn the object
        _lastSpawnedObject = _worldDatabase.SpawnObject(objectPrototype, room);
        result.ResetLog.Add($"Loaded object {objectVnum} in room {roomVnum}");
        
        return true;
    }
    
    /// <summary>
    /// Executes equipment command: E arg1 object_vnum limit wear_position
    /// </summary>
    private bool ExecuteEquipCommand(ResetCommand command, Zone zone, ZoneResetResult result)
    {
        if (_lastSpawnedMobile == null)
        {
            _logger?.LogWarning("Equipment command without preceding mobile spawn");
            return false;
        }
        
        var objectVnum = command.Arg2;
        var maxExisting = command.Arg3;
        var wearPosition = command.Arg4;
        
        // Check limits if needed
        if (command.Arg1 == 0 && _worldDatabase.CountObjectsInZone(zone.VirtualNumber, objectVnum) >= maxExisting)
        {
            return false;
        }
        
        var objectPrototype = _objectDatabase.GetObject(objectVnum);
        if (objectPrototype == null)
        {
            _logger?.LogWarning("Object prototype {ObjectVnum} not found for equipment", objectVnum);
            return false;
        }
        
        _worldDatabase.EquipObjectOnMobile(objectPrototype, _lastSpawnedMobile, wearPosition);
        result.ResetLog.Add($"Equipped object {objectVnum} on mobile {_lastSpawnedMobile.VirtualNumber} at position {wearPosition}");
        
        return true;
    }
    
    /// <summary>
    /// Executes give command: G arg1 object_vnum limit unused
    /// </summary>
    private bool ExecuteGiveCommand(ResetCommand command, Zone zone, ZoneResetResult result)
    {
        if (_lastSpawnedMobile == null)
        {
            _logger?.LogWarning("Give command without preceding mobile spawn");
            return false;
        }
        
        var objectVnum = command.Arg2;
        var maxExisting = command.Arg3;
        
        // Check limits if needed
        if (command.Arg1 == 0 && _worldDatabase.CountObjectsInZone(zone.VirtualNumber, objectVnum) >= maxExisting)
        {
            return false;
        }
        
        var objectPrototype = _objectDatabase.GetObject(objectVnum);
        if (objectPrototype == null)
        {
            _logger?.LogWarning("Object prototype {ObjectVnum} not found for give", objectVnum);
            return false;
        }
        
        _worldDatabase.GiveObjectToMobile(objectPrototype, _lastSpawnedMobile);
        result.ResetLog.Add($"Gave object {objectVnum} to mobile {_lastSpawnedMobile.VirtualNumber}");
        
        return true;
    }
    
    /// <summary>
    /// Executes door command: D arg1 room_vnum direction state
    /// </summary>
    private bool ExecuteDoorCommand(ResetCommand command, Zone zone, ZoneResetResult result)
    {
        var roomVnum = command.Arg2;
        var direction = (Direction)command.Arg3;
        var doorState = (DoorState)command.Arg4;
        
        var room = _worldDatabase.GetRoom(roomVnum);
        if (room == null)
        {
            _logger?.LogWarning("Room {RoomVnum} not found for door command", roomVnum);
            return false;
        }
        
        _worldDatabase.SetDoorState(room, direction, doorState);
        result.ResetLog.Add($"Set door in room {roomVnum} direction {direction} to state {doorState}");
        
        return true;
    }
    
    /// <summary>
    /// Executes put command: P arg1 object_vnum limit container_vnum
    /// </summary>
    private bool ExecutePutCommand(ResetCommand command, Zone zone, ZoneResetResult result)
    {
        if (_lastSpawnedObject == null)
        {
            _logger?.LogWarning("Put command without preceding object spawn");
            return false;
        }
        
        var objectVnum = command.Arg2;
        var maxExisting = command.Arg3;
        var containerVnum = command.Arg4;
        
        // Check limits if needed
        if (command.Arg1 == 0 && _worldDatabase.CountObjectsInZone(zone.VirtualNumber, objectVnum) >= maxExisting)
        {
            return false;
        }
        
        var objectPrototype = _objectDatabase.GetObject(objectVnum);
        if (objectPrototype == null)
        {
            _logger?.LogWarning("Object prototype {ObjectVnum} not found for put", objectVnum);
            return false;
        }
        
        // For put commands, we use the last spawned object as the container if it matches the vnum
        if (_lastSpawnedObject.VirtualNumber == containerVnum)
        {
            _worldDatabase.PutObjectInContainer(objectPrototype, _lastSpawnedObject);
            result.ResetLog.Add($"Put object {objectVnum} in container {containerVnum}");
            return true;
        }
        
        _logger?.LogWarning("Container {ContainerVnum} not found or not last spawned object for put command", containerVnum);
        return false;
    }
    
    /// <summary>
    /// Executes remove command: R arg1 room_vnum object_vnum unused
    /// </summary>
    private bool ExecuteRemoveCommand(ResetCommand command, Zone zone, ZoneResetResult result)
    {
        var roomVnum = command.Arg2;
        var objectVnum = command.Arg3;
        
        var room = _worldDatabase.GetRoom(roomVnum);
        if (room == null)
        {
            _logger?.LogWarning("Room {RoomVnum} not found for remove command", roomVnum);
            return false;
        }
        
        var removed = _worldDatabase.RemoveObjectFromRoom(room, objectVnum);
        if (removed)
        {
            result.ResetLog.Add($"Removed object {objectVnum} from room {roomVnum}");
        }
        
        return removed;
    }
    
    /// <summary>
    /// Determines if a zone should be reset based on its configuration and state
    /// </summary>
    public bool ShouldReset(Zone zone)
    {
        // Never reset if mode is Never
        if (zone.ResetMode == ResetMode.Never)
            return false;
            
        // Don't reset if not old enough
        if (zone.Age < zone.ResetTime)
            return false;
            
        // Always reset if mode is Always
        if (zone.ResetMode == ResetMode.Always)
            return true;
            
        // Reset if zone is empty and mode is WhenEmpty
        if (zone.ResetMode == ResetMode.WhenEmpty)
        {
            return _worldDatabase.CountPlayersInZone(zone.VirtualNumber) == 0;
        }
        
        return false;
    }
    
    /// <summary>
    /// Ages a zone by the specified number of minutes
    /// </summary>
    public void AgeZone(Zone zone, int minutesElapsed)
    {
        zone.Age += minutesElapsed;
    }
}