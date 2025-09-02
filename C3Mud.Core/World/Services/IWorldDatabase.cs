using C3Mud.Core.World.Models;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Interface for the in-memory world database that provides fast room lookup and storage
/// Supports O(1) room retrieval by virtual number for optimal performance
/// </summary>
public interface IWorldDatabase
{
    /// <summary>
    /// Loads a single room into the database
    /// If a room with the same VNum already exists, it will be overwritten
    /// </summary>
    /// <param name="room">The room to load into the database</param>
    void LoadRoom(Room room);
    
    /// <summary>
    /// Loads multiple rooms from a world file into the database
    /// </summary>
    /// <param name="filePath">Path to the .wld file to load</param>
    /// <returns>Task representing the async loading operation</returns>
    Task LoadRoomsAsync(string filePath);
    
    /// <summary>
    /// Retrieves a room by its virtual number
    /// Provides O(1) lookup performance using Dictionary/ConcurrentDictionary
    /// </summary>
    /// <param name="vnum">Virtual number of the room to retrieve</param>
    /// <returns>Room object if found, null otherwise</returns>
    Room? GetRoom(int vnum);
    
    /// <summary>
    /// Gets all loaded rooms in the database
    /// </summary>
    /// <returns>Enumerable collection of all rooms</returns>
    IEnumerable<Room> GetAllRooms();
    
    /// <summary>
    /// Gets the total number of rooms currently loaded in the database
    /// </summary>
    /// <returns>Count of loaded rooms</returns>
    int GetRoomCount();
    
    /// <summary>
    /// Checks if a room with the specified virtual number is loaded
    /// </summary>
    /// <param name="vnum">Virtual number to check</param>
    /// <returns>True if room is loaded, false otherwise</returns>
    bool IsRoomLoaded(int vnum);
}