using System.Collections.Concurrent;
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
    
    /// <summary>
    /// Initializes a new WorldDatabase with empty room storage
    /// </summary>
    public WorldDatabase()
    {
        _rooms = new ConcurrentDictionary<int, Room>();
        _parser = new WorldFileParser();
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
}