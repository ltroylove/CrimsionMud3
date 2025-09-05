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
    
    /// <summary>
    /// Gets the current weather description for outdoor areas
    /// </summary>
    /// <returns>Weather description string</returns>
    string GetCurrentWeather();
    
    // Zone Reset Methods
    
    /// <summary>
    /// Spawns a mobile in the specified room
    /// </summary>
    /// <param name="mobilePrototype">Mobile template to spawn from</param>
    /// <param name="room">Room to spawn the mobile in</param>
    /// <returns>The spawned mobile instance</returns>
    Mobile SpawnMobile(Mobile mobilePrototype, Room room);
    
    /// <summary>
    /// Spawns an object in the specified room
    /// </summary>
    /// <param name="objectPrototype">Object template to spawn from</param>
    /// <param name="room">Room to spawn the object in</param>
    /// <returns>The spawned object instance</returns>
    WorldObject SpawnObject(WorldObject objectPrototype, Room room);
    
    /// <summary>
    /// Equips an object on a mobile at the specified wear position
    /// </summary>
    /// <param name="objectPrototype">Object template to equip</param>
    /// <param name="mobile">Mobile to equip the object on</param>
    /// <param name="wearPosition">Position to wear the object</param>
    void EquipObjectOnMobile(WorldObject objectPrototype, Mobile mobile, int wearPosition);
    
    /// <summary>
    /// Gives an object to a mobile's inventory
    /// </summary>
    /// <param name="objectPrototype">Object template to give</param>
    /// <param name="mobile">Mobile to give the object to</param>
    void GiveObjectToMobile(WorldObject objectPrototype, Mobile mobile);
    
    /// <summary>
    /// Sets the state of a door in a room
    /// </summary>
    /// <param name="room">Room containing the door</param>
    /// <param name="direction">Direction of the door</param>
    /// <param name="doorState">New state for the door</param>
    void SetDoorState(Room room, Direction direction, DoorState doorState);
    
    /// <summary>
    /// Puts an object inside a container
    /// </summary>
    /// <param name="objectPrototype">Object template to put</param>
    /// <param name="container">Container to put the object in</param>
    void PutObjectInContainer(WorldObject objectPrototype, WorldObject container);
    
    /// <summary>
    /// Removes an object from a room
    /// </summary>
    /// <param name="room">Room to remove object from</param>
    /// <param name="objectVnum">Virtual number of object to remove</param>
    /// <returns>True if object was removed</returns>
    bool RemoveObjectFromRoom(Room room, int objectVnum);
    
    /// <summary>
    /// Counts the number of mobiles of a specific vnum in a zone
    /// </summary>
    /// <param name="zoneNumber">Zone number to count in</param>
    /// <param name="mobileVnum">Mobile vnum to count</param>
    /// <returns>Count of mobiles</returns>
    int CountMobilesInZone(int zoneNumber, int mobileVnum);
    
    /// <summary>
    /// Counts the number of objects of a specific vnum in a zone
    /// </summary>
    /// <param name="zoneNumber">Zone number to count in</param>
    /// <param name="objectVnum">Object vnum to count</param>
    /// <returns>Count of objects</returns>
    int CountObjectsInZone(int zoneNumber, int objectVnum);
    
    /// <summary>
    /// Counts the number of players currently in a zone
    /// </summary>
    /// <param name="zoneNumber">Zone number to count players in</param>
    /// <returns>Count of players in zone</returns>
    int CountPlayersInZone(int zoneNumber);
}