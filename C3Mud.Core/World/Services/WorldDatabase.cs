using System.Collections.Concurrent;
using System.Linq;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Parsers;
using C3Mud.Core.Services;

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
    private readonly IRoomPlayerManager _roomPlayerManager;
    private readonly Random _weatherRandom;
    
    /// <summary>
    /// Initializes a new WorldDatabase with empty room storage
    /// </summary>
    public WorldDatabase(IMobileInstanceManager? mobileInstanceManager = null, IMobileSpawner? mobileSpawner = null, 
                        IObjectInstanceManager? objectInstanceManager = null, IObjectSpawner? objectSpawner = null,
                        IRoomPlayerManager? roomPlayerManager = null)
    {
        _rooms = new ConcurrentDictionary<int, Room>();
        _parser = new WorldFileParser();
        _mobileInstanceManager = mobileInstanceManager ?? new MobileInstanceManager();
        _mobileSpawner = mobileSpawner ?? new MobileSpawner();
        _objectInstanceManager = objectInstanceManager ?? new ObjectInstanceManager();
        _objectSpawner = objectSpawner ?? new ObjectSpawner();
        _roomPlayerManager = roomPlayerManager ?? new BasicRoomPlayerManager();
        _weatherRandom = new Random();
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
    /// Sets the state of a door in a room
    /// Properly modifies the exit's door flags based on the CircleMUD door system
    /// Door flags: 0=no door, 1=door exists, 2=door pickproof, bit combinations for states
    /// </summary>
    public void SetDoorState(Room room, Direction direction, DoorState doorState)
    {
        if (room.Exits.TryGetValue(direction, out var exit))
        {
            // Set door state based on DoorState enum using modern C# flag patterns
            // Based on original CircleMUD EX_ISDOOR, EX_CLOSED, EX_LOCKED flags
            var currentFlags = (DoorFlags)exit.DoorFlags;
            
            switch (doorState)
            {
                case DoorState.Open:
                    // Door exists but is open - clear closed and locked flags but keep door flag
                    currentFlags = currentFlags & ~DoorFlags.CLOSED & ~DoorFlags.LOCKED;
                    currentFlags |= DoorFlags.ISDOOR; // Ensure door flag exists
                    break;
                    
                case DoorState.Closed:
                    // Door exists and is closed but not locked
                    currentFlags = (currentFlags | DoorFlags.ISDOOR | DoorFlags.CLOSED) & ~DoorFlags.LOCKED;
                    break;
                    
                case DoorState.Locked:
                    // Door exists, is closed, and is locked
                    currentFlags = currentFlags | DoorFlags.ISDOOR | DoorFlags.CLOSED | DoorFlags.LOCKED;
                    break;
            }
            
            exit.DoorFlags = (int)currentFlags;
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
    /// Counts players currently in a zone
    /// Uses the room player manager to count players in all rooms within the specified zone
    /// </summary>
    public int CountPlayersInZone(int zoneNumber)
    {
        // Get all rooms in the zone (standard MUD convention: vnum / 100 == zone number)
        var roomsInZone = _rooms.Values
            .Where(room => room.VirtualNumber / 100 == zoneNumber)
            .ToList();
        
        // Count players in each room of the zone
        var totalPlayers = 0;
        foreach (var room in roomsInZone)
        {
            var playersInRoom = _roomPlayerManager.GetPlayersInRoom(room.VirtualNumber);
            totalPlayers += playersInRoom.Count();
        }
        
        return totalPlayers;
    }
    
    /// <summary>
    /// Gets the current weather description for outdoor areas
    /// Uses time-based and random variation to provide different weather states
    /// Based on CircleMUD weather system with SKY_CLOUDLESS, SKY_CLOUDY, SKY_RAINING, SKY_LIGHTNING
    /// </summary>
    public string GetCurrentWeather()
    {
        // Create a pseudo-weather system based on time and randomness
        var currentHour = DateTime.Now.Hour;
        var randomFactor = _weatherRandom.Next(0, 100);
        
        // Weather patterns based on time of day and randomness
        // Morning (6-12): Generally clear to cloudy
        // Afternoon (12-18): More variable weather
        // Evening (18-24): Often stormy
        // Night (0-6): Usually clear or light weather
        
        if (currentHour >= 6 && currentHour < 12) // Morning
        {
            if (randomFactor < 60) return "The sky is clear.";
            if (randomFactor < 85) return "It is cloudy.";
            return "It is raining.";
        }
        else if (currentHour >= 12 && currentHour < 18) // Afternoon
        {
            if (randomFactor < 40) return "The sky is clear.";
            if (randomFactor < 60) return "It is cloudy.";
            if (randomFactor < 85) return "It is raining.";
            return "Lightning flashes across the sky.";
        }
        else if (currentHour >= 18 && currentHour < 24) // Evening
        {
            if (randomFactor < 30) return "The sky is clear.";
            if (randomFactor < 50) return "It is cloudy.";
            if (randomFactor < 75) return "It is raining.";
            return "Lightning flashes across the sky.";
        }
        else // Night (0-6)
        {
            if (randomFactor < 70) return "The sky is clear.";
            if (randomFactor < 90) return "It is cloudy.";
            return "It is raining.";
        }
    }
}