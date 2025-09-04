using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Parsers;

namespace C3Mud.Core.World.Services;

/// <summary>
/// In-memory mobile database providing fast O(1) mobile template lookup and thread-safe operations
/// Uses ConcurrentDictionary for optimal performance under concurrent access
/// </summary>
public class MobileDatabase : IMobileDatabase
{
    private readonly ConcurrentDictionary<int, Mobile> _mobiles;
    private readonly MobileFileParser _parser;
    
    /// <summary>
    /// Initializes a new MobileDatabase with empty mobile template storage
    /// </summary>
    public MobileDatabase()
    {
        _mobiles = new ConcurrentDictionary<int, Mobile>();
        _parser = new MobileFileParser();
    }
    
    /// <summary>
    /// Loads a single mobile template into the database
    /// Thread-safe operation that overwrites existing mobile templates with the same VNum
    /// </summary>
    /// <param name="mobile">The mobile template to load</param>
    public void LoadMobile(Mobile mobile)
    {
        if (mobile == null)
            throw new ArgumentNullException(nameof(mobile));
            
        _mobiles.AddOrUpdate(mobile.VirtualNumber, mobile, (key, oldValue) => mobile);
    }
    
    /// <summary>
    /// Loads multiple mobile templates from a .mob file asynchronously
    /// Parses the entire .mob file and loads all mobile templates into memory
    /// </summary>
    /// <param name="filePath">Path to the .mob file</param>
    /// <returns>Task representing the async operation</returns>
    public async Task LoadMobilesAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Mobile file not found: {filePath}");
        
        var fileContent = await File.ReadAllTextAsync(filePath);
        var mobiles = ParseMobilesFromFile(fileContent);
        
        foreach (var mobile in mobiles)
        {
            LoadMobile(mobile);
        }
    }
    
    /// <summary>
    /// Retrieves a mobile template by virtual number with O(1) performance
    /// </summary>
    /// <param name="vnum">Virtual number of the mobile template</param>
    /// <returns>Mobile template if found, null otherwise</returns>
    public Mobile? GetMobile(int vnum)
    {
        return _mobiles.TryGetValue(vnum, out var mobile) ? mobile : null;
    }
    
    /// <summary>
    /// Creates a new mobile instance from the template for spawning
    /// This is used when creating actual mobile instances in the world
    /// </summary>
    /// <param name="vnum">Virtual number of the mobile template</param>
    /// <returns>New mobile instance based on template, null if template not found</returns>
    public Mobile? CreateMobileInstance(int vnum)
    {
        var template = GetMobile(vnum);
        return template?.CreateInstance();
    }
    
    /// <summary>
    /// Gets all mobile templates currently loaded in the database
    /// </summary>
    /// <returns>Collection of all mobile templates</returns>
    public IEnumerable<Mobile> GetAllMobiles()
    {
        return _mobiles.Values;
    }
    
    /// <summary>
    /// Gets the total count of loaded mobile templates
    /// </summary>
    /// <returns>Number of mobile templates in the database</returns>
    public int GetMobileCount()
    {
        return _mobiles.Count;
    }
    
    /// <summary>
    /// Checks if a mobile template is loaded in the database
    /// </summary>
    /// <param name="vnum">Virtual number to check</param>
    /// <returns>True if mobile template exists, false otherwise</returns>
    public bool IsMobileLoaded(int vnum)
    {
        return _mobiles.ContainsKey(vnum);
    }
    
    /// <summary>
    /// Parses mobile templates from a complete .mob file content
    /// Splits the file into individual mobile sections and parses each one
    /// </summary>
    /// <param name="fileContent">Complete content of the .mob file</param>
    /// <returns>List of parsed mobile templates</returns>
    private List<Mobile> ParseMobilesFromFile(string fileContent)
    {
        var mobiles = new List<Mobile>();
        
        // Split file content by mobile separators (lines starting with #)
        var lines = fileContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var currentMobileData = new List<string>();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Skip empty lines and comment lines
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("$"))
                continue;
            
            // Check if this is the start of a new mobile (line starting with #)
            if (trimmedLine.StartsWith("#") && currentMobileData.Count > 0)
            {
                // Parse the previous mobile data
                try
                {
                    var mobileDataString = string.Join("\n", currentMobileData);
                    var mobile = _parser.ParseMobile(mobileDataString);
                    mobiles.Add(mobile);
                }
                catch (ParseException ex)
                {
                    // Log parsing error but continue with other mobiles
                    Console.WriteLine($"Error parsing mobile: {ex.Message}");
                }
                
                currentMobileData.Clear();
            }
            
            currentMobileData.Add(line);
        }
        
        // Parse the last mobile if any data remains
        if (currentMobileData.Count > 0)
        {
            try
            {
                var mobileDataString = string.Join("\n", currentMobileData);
                var mobile = _parser.ParseMobile(mobileDataString);
                mobiles.Add(mobile);
            }
            catch (ParseException ex)
            {
                Console.WriteLine($"Error parsing final mobile: {ex.Message}");
            }
        }
        
        return mobiles;
    }
}