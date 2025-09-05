using System.Collections.Concurrent;
using System.Linq;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Parsers;

namespace C3Mud.Core.World.Services;

/// <summary>
/// In-memory world database providing fast O(1) room lookup and thread-safe operations
/// Uses ConcurrentDictionary for optimal performance under concurrent access
/// </summary>
public class WorldDatabase : IWorldDatabase
{
    private readonly ConcurrentDictionary<int, Room> _rooms;
    private readonly WorldFileParser _parser;
    private readonly IMobileInstanceManager _mobileInstanceManager;
    private readonly IMobileSpawner _mobileSpawner;
    private readonly IObjectInstanceManager _objectInstanceManager;
    private readonly IObjectSpawner _objectSpawner;
    
    /// <summary>
    /// Initializes a new WorldDatabase with empty room storage
    /// </summary>
    public WorldDatabase(IMobileInstanceManager? mobileInstanceManager = null, IMobileSpawner? mobileSpawner = null, 
                        IObjectInstanceManager? objectInstanceManager = null, IObjectSpawner? objectSpawner = null)
    {
        _rooms = new ConcurrentDictionary<int, Room>();
        _parser = new WorldFileParser();
        _mobileInstanceManager = mobileInstanceManager ?? new MobileInstanceManager();
        _mobileSpawner = mobileSpawner ?? new MobileSpawner();
        _objectInstanceManager = objectInstanceManager ?? new ObjectInstanceManager();
        _objectSpawner = objectSpawner ?? new ObjectSpawner();
    }
    
    /// <summary>
    /// Initializes a new WorldDatabase with empty room storage (legacy constructor for tests)
    /// </summary>
    public WorldDatabase() : this(null, null)
    {
    }
    
    /// <summary>
    /// Loads a single room into the database
    /// Thread-safe operation that overwrites existing rooms with the same VNum
    /// </summary>
    /// <param name="room">The room to load</param>
    public void LoadRoom(Room room)
    {
        if (room == null)
            throw new ArgumentNullException(nameof(room));
            
        _rooms.AddOrUpdate(room.VirtualNumber, room, (key, oldValue) => room);
    }
    
    /// <summary>
    /// Loads multiple rooms from a world file asynchronously
    /// Parses the entire .wld file and loads all rooms into memory
    /// </summary>
    /// <param name="filePath">Path to the .wld file</param>
    /// <returns>Task representing the async operation</returns>
    public async Task LoadRoomsAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"World file not found: {filePath}");
        
        var fileContent = await File.ReadAllTextAsync(filePath);
        var rooms = ParseRoomsFromFile(fileContent);
        
        foreach (var room in rooms)
        {
            LoadRoom(room);
        }
    }
    
    /// <summary>
    /// Retrieves a room by virtual number with O(1) performance
    /// </summary>
    /// <param name="vnum">Virtual number of the room</param>
    /// <returns>Room if found, null otherwise</returns>
    public Room? GetRoom(int vnum)
    {
        return _rooms.TryGetValue(vnum, out var room) ? room : null;
    }
    
    /// <summary>
    /// Gets all rooms currently loaded in the database
    /// </summary>
    /// <returns>Collection of all rooms</returns>
    public IEnumerable<Room> GetAllRooms()
    {
        return _rooms.Values;
    }
    
    /// <summary>
    /// Gets the total count of loaded rooms
    /// </summary>
    /// <returns>Number of rooms in the database</returns>
    public int GetRoomCount()
    {
        return _rooms.Count;
    }
    
    /// <summary>
    /// Checks if a room is loaded in the database
    /// </summary>
    /// <param name="vnum">Virtual number to check</param>
    /// <returns>True if room exists, false otherwise</returns>
    public bool IsRoomLoaded(int vnum)
    {
        return _rooms.ContainsKey(vnum);
    }
    
    /// <summary>
    /// Parses rooms from a complete .wld file content
    /// Splits the file into individual room sections and parses each one
    /// </summary>
    /// <param name="fileContent">Complete content of the .wld file</param>
    /// <returns>List of parsed rooms</returns>
    private List<Room> ParseRoomsFromFile(string fileContent)
    {
        var rooms = new List<Room>();
        
        // Split file content by room separators (lines starting with #)
        var lines = fileContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var currentRoomData = new List<string>();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Check if this is the start of a new room (line starting with #)
            if (trimmedLine.StartsWith("#") && currentRoomData.Count > 0)
            {
                // Parse the previous room data
                try
                {
                    var roomDataString = string.Join("\n", currentRoomData);
                    var room = _parser.ParseRoom(roomDataString);
                    rooms.Add(room);
                }
                catch (ParseException ex)
                {
                    // Log parsing error but continue with other rooms
                    Console.WriteLine($"Error parsing room: {ex.Message}");
                }
                
                currentRoomData.Clear();
            }
            
            currentRoomData.Add(line);
        }
        
        // Parse the last room if any data remains
        if (currentRoomData.Count > 0)
        {
            try
            {
                var roomDataString = string.Join("\n", currentRoomData);
                var room = _parser.ParseRoom(roomDataString);
                rooms.Add(room);
            }
            catch (ParseException ex)
            {
                Console.WriteLine($"Error parsing final room: {ex.Message}");
            }
        }
        
        return rooms;
    }
    
    // Zone Reset Methods Implementation
    
    /// <summary>
    /// Spawns a mobile in the specified room using the mobile instance system
    /// </summary>
    public Mobile SpawnMobile(Mobile mobilePrototype, Room room)
    {
        // Create a new mobile instance using the spawner
        var mobileInstance = _mobileSpawner.SpawnInRoom(mobilePrototype, room.VirtualNumber);
        
        // Track the mobile instance in the world
        _mobileInstanceManager.TrackMobile(mobileInstance);
        
        // Return the mobile template for backward compatibility with existing code
        // In future iterations, this should return the MobileInstance instead
        return mobilePrototype;
    }
    
    /// <summary>
    /// Spawns an object in the specified room using the object instance system
    /// </summary>
    public WorldObject SpawnObject(WorldObject objectPrototype, Room room)
    {
        // Create a new object instance using the spawner
        var objectInstance = _objectSpawner.SpawnInRoom(objectPrototype, room.VirtualNumber);
        
        // Track the object instance in the world
        _objectInstanceManager.TrackObject(objectInstance);
        
        // Return the object template for backward compatibility with existing code
        // In future iterations, this should return the ObjectInstance instead
        return objectPrototype;
    }
    
    /// <summary>
    /// Equips an object on a mobile using the object instance system
    /// </summary>
    public void EquipObjectOnMobile(WorldObject objectPrototype, Mobile mobile, int wearPosition)
    {
        // Find the mobile instance that corresponds to this mobile
        // For now, we'll find the most recently spawned mobile of this type
        var mobileInstance = _mobileInstanceManager.GetAllActiveMobiles()
            .Where(m => m.Template.VirtualNumber == mobile.VirtualNumber)
            .OrderByDescending(m => m.SpawnTime)
            .FirstOrDefault();
            
        if (mobileInstance != null)
        {
            // Create object instance and equip it
            var objectInstance = _objectSpawner.CreateInstance(objectPrototype);
            _objectSpawner.EquipOnMobile(objectInstance, mobileInstance, (WearPosition)wearPosition);
            
            // Track the object instance
            _objectInstanceManager.TrackObject(objectInstance);
        }
        else
        {
            // Fallback to old behavior for backward compatibility
            Console.WriteLine($"Equipped {objectPrototype.ShortDescription} on {mobile.ShortDescription} at position {wearPosition}");
        }
    }
    
    /// <summary>
    /// Gives an object to a mobile's inventory using the object instance system
    /// </summary>
    public void GiveObjectToMobile(WorldObject objectPrototype, Mobile mobile)
    {
        // Find the mobile instance that corresponds to this mobile
        // For now, we'll find the most recently spawned mobile of this type
        var mobileInstance = _mobileInstanceManager.GetAllActiveMobiles()
            .Where(m => m.Template.VirtualNumber == mobile.VirtualNumber)
            .OrderByDescending(m => m.SpawnTime)
            .FirstOrDefault();
            
        if (mobileInstance != null)
        {
            // Create object instance and give it to the mobile
            var objectInstance = _objectSpawner.CreateInstance(objectPrototype);
            _objectSpawner.GiveToMobile(objectInstance, mobileInstance);
            
            // Track the object instance
            _objectInstanceManager.TrackObject(objectInstance);
        }
        else
        {
            // Fallback to old behavior for backward compatibility
            Console.WriteLine($"Gave {objectPrototype.ShortDescription} to {mobile.ShortDescription}");
        }
    }
    
    /// <summary>
    /// Sets the state of a door in a room (placeholder implementation)
    /// </summary>
    public void SetDoorState(Room room, Direction direction, DoorState doorState)
    {
        // TODO: Implement door state management
        // This would typically modify the exit's door flags
        if (room.Exits.TryGetValue(direction, out var exit))
        {
            // Set door state based on DoorState enum
            switch (doorState)
            {
                case DoorState.Open:
                    exit.DoorFlags = 0; // Clear door flags
                    break;
                case DoorState.Closed:
                    exit.DoorFlags = 1; // Set closed flag
                    break;
                case DoorState.Locked:
                    exit.DoorFlags = 3; // Set closed and locked flags
                    break;
            }
        }
    }
    
    /// <summary>
    /// Puts an object inside a container using the object instance system
    /// </summary>
    public void PutObjectInContainer(WorldObject objectPrototype, WorldObject container)
    {
        // Find the container instance - look for the most recently spawned container
        var containerInstance = _objectInstanceManager.GetAllActiveObjects()
            .Where(o => o.Template.VirtualNumber == container.VirtualNumber)
            .OrderByDescending(o => o.SpawnTime)
            .FirstOrDefault();
            
        if (containerInstance != null)
        {
            // Create object instance and put it in the container
            var objectInstance = _objectSpawner.CreateInstance(objectPrototype);
            _objectSpawner.PutInContainer(objectInstance, containerInstance);
            
            // Track the object instance
            _objectInstanceManager.TrackObject(objectInstance);
        }
        else
        {
            // Fallback to old behavior for backward compatibility
            Console.WriteLine($"Put {objectPrototype.ShortDescription} in {container.ShortDescription}");
        }
    }
    
    /// <summary>
    /// Removes an object from a room using the object instance system
    /// </summary>
    public bool RemoveObjectFromRoom(Room room, int objectVnum)
    {
        // Find object instances in the room
        var objectsInRoom = _objectInstanceManager.GetObjectsInRoom(room.VirtualNumber)
            .Where(o => o.Template.VirtualNumber == objectVnum)
            .ToList();
            
        if (objectsInRoom.Any())
        {
            // Remove the first matching object
            var objectToRemove = objectsInRoom.First();
            _objectInstanceManager.RemoveObject(objectToRemove.InstanceId);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Counts mobiles of a specific vnum in a zone using the mobile instance system
    /// </summary>
    public int CountMobilesInZone(int zoneNumber, int mobileVnum)
    {
        return _mobileInstanceManager.GetMobilesInZone(zoneNumber)
            .Count(m => m.Template.VirtualNumber == mobileVnum);
    }
    
    /// <summary>
    /// Counts objects of a specific vnum in a zone using the object instance system
    /// </summary>
    public int CountObjectsInZone(int zoneNumber, int objectVnum)
    {
        // Count objects in all rooms of the zone
        // Zone rooms are typically vnum / 100 == zoneNumber (standard MUD convention)
        return _objectInstanceManager.GetAllActiveObjects()
            .Where(o => o.Template.VirtualNumber == objectVnum)
            .Count(o => o.Location == ObjectLocation.InRoom && 
                       ((int)o.LocationId) / 100 == zoneNumber);
    }
    
    /// <summary>
    /// Counts players currently in a zone (placeholder implementation)
    /// </summary>
    public int CountPlayersInZone(int zoneNumber)
    {
        // TODO: Implement player counting
        // This would count active players in the zone
        return 0; // Placeholder - always return 0 for now
    }
    
    /// <summary>
    /// Gets the current weather description for outdoor areas
    /// </summary>
    public string GetCurrentWeather()
    {
        // TODO: Implement proper weather system
        // For now, return a placeholder weather description
        return "The sky is clear.";
    }
}