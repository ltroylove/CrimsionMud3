using System;
using System.IO;
using System.Threading.Tasks;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Service responsible for orchestrating the loading of world data files
/// Coordinates between file discovery, parsing, and database storage
/// </summary>
public class WorldLoader
{
    private readonly IWorldDatabase _worldDatabase;
    private readonly IMobileDatabase _mobileDatabase;
    private readonly IObjectDatabase _objectDatabase;
    private readonly IZoneDatabase _zoneDatabase;
    
    /// <summary>
    /// Initializes a new WorldLoader with the specified databases
    /// </summary>
    /// <param name="worldDatabase">The world database to load room data into</param>
    /// <param name="mobileDatabase">The mobile database to load mobile data into</param>
    /// <param name="objectDatabase">The object database to load object data into</param>
    /// <param name="zoneDatabase">The zone database to load zone data into</param>
    public WorldLoader(IWorldDatabase worldDatabase, IMobileDatabase mobileDatabase, IObjectDatabase objectDatabase, IZoneDatabase zoneDatabase)
    {
        _worldDatabase = worldDatabase ?? throw new ArgumentNullException(nameof(worldDatabase));
        _mobileDatabase = mobileDatabase ?? throw new ArgumentNullException(nameof(mobileDatabase));
        _objectDatabase = objectDatabase ?? throw new ArgumentNullException(nameof(objectDatabase));
        _zoneDatabase = zoneDatabase ?? throw new ArgumentNullException(nameof(zoneDatabase));
    }
    
    /// <summary>
    /// Loads all world files from a specified directory
    /// Processes all .wld, .mob, .obj, and .zon files found in the directory
    /// </summary>
    /// <param name="worldDirectory">Directory containing .wld, .mob, .obj, and .zon files</param>
    /// <returns>Task representing the loading operation with loading statistics</returns>
    public async Task<WorldLoadingResult> LoadWorldFromDirectoryAsync(string worldDirectory)
    {
        if (string.IsNullOrWhiteSpace(worldDirectory))
            throw new ArgumentException("World directory cannot be null or empty", nameof(worldDirectory));
            
        if (!Directory.Exists(worldDirectory))
            throw new DirectoryNotFoundException($"World directory not found: {worldDirectory}");
        
        var worldFiles = Directory.GetFiles(Path.Combine(worldDirectory, "Areas"), "*.wld");
        var mobileFiles = Directory.GetFiles(Path.Combine(worldDirectory, "Mobiles"), "*.mob");
        var objectFiles = Directory.GetFiles(Path.Combine(worldDirectory, "Objects"), "*.obj");
        var zoneFiles = Directory.GetFiles(Path.Combine(worldDirectory, "Zones"), "*.zon");
        
        if (worldFiles.Length == 0 && mobileFiles.Length == 0 && objectFiles.Length == 0 && zoneFiles.Length == 0)
        {
            throw new InvalidOperationException($"No .wld, .mob, .obj, or .zon files found in directory: {worldDirectory}");
        }
        
        int initialRoomCount = _worldDatabase.GetRoomCount();
        int initialMobileCount = _mobileDatabase.GetMobileCount();
        int initialObjectCount = _objectDatabase.ObjectCount;
        int initialZoneCount = _zoneDatabase.GetZoneCount();
        
        // Load world files (.wld)
        foreach (var worldFile in worldFiles)
        {
            try
            {
                await _worldDatabase.LoadRoomsAsync(worldFile);
                Console.WriteLine($"Loaded world file: {Path.GetFileName(worldFile)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading world file {worldFile}: {ex.Message}");
                // Continue loading other files despite errors
            }
        }
        
        // Load mobile files (.mob)
        foreach (var mobileFile in mobileFiles)
        {
            try
            {
                await _mobileDatabase.LoadMobilesAsync(mobileFile);
                Console.WriteLine($"Loaded mobile file: {Path.GetFileName(mobileFile)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading mobile file {mobileFile}: {ex.Message}");
                // Continue loading other files despite errors
            }
        }
        
        // Load object files (.obj)
        foreach (var objectFile in objectFiles)
        {
            try
            {
                await _objectDatabase.LoadObjectsAsync(objectFile);
                Console.WriteLine($"Loaded object file: {Path.GetFileName(objectFile)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading object file {objectFile}: {ex.Message}");
                // Continue loading other files despite errors
            }
        }
        
        // Load zone files (.zon)
        foreach (var zoneFile in zoneFiles)
        {
            try
            {
                await _zoneDatabase.LoadZonesAsync(zoneFile);
                Console.WriteLine($"Loaded zone file: {Path.GetFileName(zoneFile)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading zone file {zoneFile}: {ex.Message}");
                // Continue loading other files despite errors
            }
        }
        
        int finalRoomCount = _worldDatabase.GetRoomCount();
        int finalMobileCount = _mobileDatabase.GetMobileCount();
        int finalObjectCount = _objectDatabase.ObjectCount;
        int finalZoneCount = _zoneDatabase.GetZoneCount();
        int loadedRoomCount = finalRoomCount - initialRoomCount;
        int loadedMobileCount = finalMobileCount - initialMobileCount;
        int loadedObjectCount = finalObjectCount - initialObjectCount;
        int loadedZoneCount = finalZoneCount - initialZoneCount;
        
        Console.WriteLine($"World loading complete. Loaded {loadedRoomCount} rooms from {worldFiles.Length} files.");
        Console.WriteLine($"Mobile loading complete. Loaded {loadedMobileCount} mobiles from {mobileFiles.Length} files.");
        Console.WriteLine($"Object loading complete. Loaded {loadedObjectCount} objects from {objectFiles.Length} files.");
        Console.WriteLine($"Zone loading complete. Loaded {loadedZoneCount} zones from {zoneFiles.Length} files.");
        Console.WriteLine($"Total rooms in database: {finalRoomCount}");
        Console.WriteLine($"Total mobiles in database: {finalMobileCount}");
        Console.WriteLine($"Total objects in database: {finalObjectCount}");
        Console.WriteLine($"Total zones in database: {finalZoneCount}");
        
        return new WorldLoadingResult
        {
            LoadedRooms = loadedRoomCount,
            LoadedMobiles = loadedMobileCount,
            LoadedObjects = loadedObjectCount,
            LoadedZones = loadedZoneCount,
            TotalRooms = finalRoomCount,
            TotalMobiles = finalMobileCount,
            TotalObjects = finalObjectCount,
            TotalZones = finalZoneCount,
            WorldFilesProcessed = worldFiles.Length,
            MobileFilesProcessed = mobileFiles.Length,
            ObjectFilesProcessed = objectFiles.Length,
            ZoneFilesProcessed = zoneFiles.Length
        };
    }
    
    /// <summary>
    /// Loads a single world file
    /// </summary>
    /// <param name="worldFilePath">Path to the .wld file to load</param>
    /// <returns>Task representing the loading operation</returns>
    public async Task LoadWorldFileAsync(string worldFilePath)
    {
        if (string.IsNullOrWhiteSpace(worldFilePath))
            throw new ArgumentException("World file path cannot be null or empty", nameof(worldFilePath));
        
        await _worldDatabase.LoadRoomsAsync(worldFilePath);
    }
    
    /// <summary>
    /// Loads a single mobile file
    /// </summary>
    /// <param name="mobileFilePath">Path to the .mob file to load</param>
    /// <returns>Task representing the loading operation</returns>
    public async Task LoadMobileFileAsync(string mobileFilePath)
    {
        if (string.IsNullOrWhiteSpace(mobileFilePath))
            throw new ArgumentException("Mobile file path cannot be null or empty", nameof(mobileFilePath));
        
        await _mobileDatabase.LoadMobilesAsync(mobileFilePath);
    }
    
    /// <summary>
    /// Loads a single object file
    /// </summary>
    /// <param name="objectFilePath">Path to the .obj file to load</param>
    /// <returns>Task representing the loading operation</returns>
    public async Task LoadObjectFileAsync(string objectFilePath)
    {
        if (string.IsNullOrWhiteSpace(objectFilePath))
            throw new ArgumentException("Object file path cannot be null or empty", nameof(objectFilePath));
        
        await _objectDatabase.LoadObjectsAsync(objectFilePath);
    }
    
    /// <summary>
    /// Loads a single zone file
    /// </summary>
    /// <param name="zoneFilePath">Path to the .zon file to load</param>
    /// <returns>Task representing the loading operation</returns>
    public async Task LoadZoneFileAsync(string zoneFilePath)
    {
        if (string.IsNullOrWhiteSpace(zoneFilePath))
            throw new ArgumentException("Zone file path cannot be null or empty", nameof(zoneFilePath));
        
        await _zoneDatabase.LoadZonesAsync(zoneFilePath);
    }
    
    /// <summary>
    /// Gets statistics about the currently loaded world
    /// </summary>
    /// <returns>World loading statistics</returns>
    public WorldLoadingStats GetLoadingStats()
    {
        return new WorldLoadingStats
        {
            TotalRooms = _worldDatabase.GetRoomCount(),
            TotalMobiles = _mobileDatabase.GetMobileCount(),
            TotalObjects = _objectDatabase.ObjectCount,
            TotalZones = _zoneDatabase.GetZoneCount(),
            LoadedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Statistics about world loading operations
/// </summary>
public class WorldLoadingStats
{
    /// <summary>
    /// Total number of rooms loaded
    /// </summary>
    public int TotalRooms { get; set; }
    
    /// <summary>
    /// Total number of mobile templates loaded
    /// </summary>
    public int TotalMobiles { get; set; }
    
    /// <summary>
    /// Total number of object templates loaded
    /// </summary>
    public int TotalObjects { get; set; }
    
    /// <summary>
    /// Total number of zones loaded
    /// </summary>
    public int TotalZones { get; set; }
    
    /// <summary>
    /// When the world data was loaded
    /// </summary>
    public DateTime LoadedAt { get; set; }
}

/// <summary>
/// Result of loading world data from a directory
/// </summary>
public class WorldLoadingResult
{
    /// <summary>
    /// Number of rooms loaded in this operation
    /// </summary>
    public int LoadedRooms { get; set; }
    
    /// <summary>
    /// Number of mobile templates loaded in this operation
    /// </summary>
    public int LoadedMobiles { get; set; }
    
    /// <summary>
    /// Number of object templates loaded in this operation
    /// </summary>
    public int LoadedObjects { get; set; }
    
    /// <summary>
    /// Number of zones loaded in this operation
    /// </summary>
    public int LoadedZones { get; set; }
    
    /// <summary>
    /// Total number of rooms after loading
    /// </summary>
    public int TotalRooms { get; set; }
    
    /// <summary>
    /// Total number of mobile templates after loading
    /// </summary>
    public int TotalMobiles { get; set; }
    
    /// <summary>
    /// Total number of object templates after loading
    /// </summary>
    public int TotalObjects { get; set; }
    
    /// <summary>
    /// Total number of zones after loading
    /// </summary>
    public int TotalZones { get; set; }
    
    /// <summary>
    /// Number of .wld files processed
    /// </summary>
    public int WorldFilesProcessed { get; set; }
    
    /// <summary>
    /// Number of .mob files processed
    /// </summary>
    public int MobileFilesProcessed { get; set; }
    
    /// <summary>
    /// Number of .obj files processed
    /// </summary>
    public int ObjectFilesProcessed { get; set; }
    
    /// <summary>
    /// Number of .zon files processed
    /// </summary>
    public int ZoneFilesProcessed { get; set; }
}