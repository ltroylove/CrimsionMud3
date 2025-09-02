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
    
    /// <summary>
    /// Initializes a new WorldLoader with the specified databases
    /// </summary>
    /// <param name="worldDatabase">The world database to load room data into</param>
    /// <param name="mobileDatabase">The mobile database to load mobile data into</param>
    /// <param name="objectDatabase">The object database to load object data into</param>
    public WorldLoader(IWorldDatabase worldDatabase, IMobileDatabase mobileDatabase, IObjectDatabase objectDatabase)
    {
        _worldDatabase = worldDatabase ?? throw new ArgumentNullException(nameof(worldDatabase));
        _mobileDatabase = mobileDatabase ?? throw new ArgumentNullException(nameof(mobileDatabase));
        _objectDatabase = objectDatabase ?? throw new ArgumentNullException(nameof(objectDatabase));
    }
    
    /// <summary>
    /// Loads all world files from a specified directory
    /// Processes all .wld, .mob, and .obj files found in the directory
    /// </summary>
    /// <param name="worldDirectory">Directory containing .wld, .mob, and .obj files</param>
    /// <returns>Task representing the loading operation with loading statistics</returns>
    public async Task<WorldLoadingResult> LoadWorldFromDirectoryAsync(string worldDirectory)
    {
        if (string.IsNullOrWhiteSpace(worldDirectory))
            throw new ArgumentException("World directory cannot be null or empty", nameof(worldDirectory));
            
        if (!Directory.Exists(worldDirectory))
            throw new DirectoryNotFoundException($"World directory not found: {worldDirectory}");
        
        var worldFiles = Directory.GetFiles(worldDirectory, "*.wld");
        var mobileFiles = Directory.GetFiles(worldDirectory, "*.mob");
        var objectFiles = Directory.GetFiles(worldDirectory, "*.obj");
        
        if (worldFiles.Length == 0 && mobileFiles.Length == 0 && objectFiles.Length == 0)
        {
            throw new InvalidOperationException($"No .wld, .mob, or .obj files found in directory: {worldDirectory}");
        }
        
        int initialRoomCount = _worldDatabase.GetRoomCount();
        int initialMobileCount = _mobileDatabase.GetMobileCount();
        int initialObjectCount = _objectDatabase.ObjectCount;
        
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
        
        int finalRoomCount = _worldDatabase.GetRoomCount();
        int finalMobileCount = _mobileDatabase.GetMobileCount();
        int finalObjectCount = _objectDatabase.ObjectCount;
        int loadedRoomCount = finalRoomCount - initialRoomCount;
        int loadedMobileCount = finalMobileCount - initialMobileCount;
        int loadedObjectCount = finalObjectCount - initialObjectCount;
        
        Console.WriteLine($"World loading complete. Loaded {loadedRoomCount} rooms from {worldFiles.Length} files.");
        Console.WriteLine($"Mobile loading complete. Loaded {loadedMobileCount} mobiles from {mobileFiles.Length} files.");
        Console.WriteLine($"Object loading complete. Loaded {loadedObjectCount} objects from {objectFiles.Length} files.");
        Console.WriteLine($"Total rooms in database: {finalRoomCount}");
        Console.WriteLine($"Total mobiles in database: {finalMobileCount}");
        Console.WriteLine($"Total objects in database: {finalObjectCount}");
        
        return new WorldLoadingResult
        {
            LoadedRooms = loadedRoomCount,
            LoadedMobiles = loadedMobileCount,
            LoadedObjects = loadedObjectCount,
            TotalRooms = finalRoomCount,
            TotalMobiles = finalMobileCount,
            TotalObjects = finalObjectCount,
            WorldFilesProcessed = worldFiles.Length,
            MobileFilesProcessed = mobileFiles.Length,
            ObjectFilesProcessed = objectFiles.Length
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
}