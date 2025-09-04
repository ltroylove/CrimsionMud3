using System.Collections.Concurrent;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Parsers;

namespace C3Mud.Core.World.Services;

/// <summary>
/// In-memory zone database providing fast zone lookup and thread-safe operations
/// </summary>
public class ZoneDatabase : IZoneDatabase
{
    private readonly ConcurrentDictionary<int, Zone> _zones;
    private readonly ZoneFileParser _parser;
    
    /// <summary>
    /// Initializes a new ZoneDatabase
    /// </summary>
    public ZoneDatabase()
    {
        _zones = new ConcurrentDictionary<int, Zone>();
        _parser = new ZoneFileParser();
    }
    
    /// <summary>
    /// Loads a single zone into the database
    /// </summary>
    public void LoadZone(Zone zone)
    {
        if (zone == null)
            throw new ArgumentNullException(nameof(zone));
            
        _zones.AddOrUpdate(zone.VirtualNumber, zone, (key, oldValue) => zone);
    }
    
    /// <summary>
    /// Loads zones from a .zon file
    /// </summary>
    public async Task LoadZonesAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Zone file not found: {filePath}");
        
        var fileContent = await File.ReadAllTextAsync(filePath);
        var zones = ParseZonesFromFile(fileContent);
        
        foreach (var zone in zones)
        {
            LoadZone(zone);
        }
    }
    
    /// <summary>
    /// Gets a zone by its virtual number
    /// </summary>
    public Zone? GetZone(int vnum)
    {
        return _zones.TryGetValue(vnum, out var zone) ? zone : null;
    }
    
    /// <summary>
    /// Gets all loaded zones
    /// </summary>
    public IEnumerable<Zone> GetAllZones()
    {
        return _zones.Values;
    }
    
    /// <summary>
    /// Gets the count of loaded zones
    /// </summary>
    public int GetZoneCount()
    {
        return _zones.Count;
    }
    
    /// <summary>
    /// Gets zones that should be reset based on their age and configuration
    /// </summary>
    public IEnumerable<Zone> GetZonesNeedingReset()
    {
        return _zones.Values.Where(zone => ShouldZoneReset(zone));
    }
    
    /// <summary>
    /// Updates a zone's age and last reset time
    /// </summary>
    public void UpdateZone(Zone zone)
    {
        if (zone == null)
            throw new ArgumentNullException(nameof(zone));
            
        _zones.AddOrUpdate(zone.VirtualNumber, zone, (key, oldValue) => zone);
    }
    
    /// <summary>
    /// Parses zones from a complete .zon file content
    /// </summary>
    private List<Zone> ParseZonesFromFile(string fileContent)
    {
        var zones = new List<Zone>();
        
        // Split file content by zone separators (lines starting with #)
        var lines = fileContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var currentZoneData = new List<string>();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Check if this is the start of a new zone (line starting with #) or end marker
            if ((trimmedLine.StartsWith("#") && !trimmedLine.StartsWith("#999999")) && currentZoneData.Count > 0)
            {
                // Parse the previous zone data
                try
                {
                    var zoneDataString = string.Join("\n", currentZoneData);
                    var zone = _parser.ParseZone(zoneDataString);
                    zones.Add(zone);
                }
                catch (ParseException ex)
                {
                    Console.WriteLine($"Error parsing zone: {ex.Message}");
                }
                
                currentZoneData.Clear();
            }
            
            // Skip the end marker line
            if (trimmedLine.StartsWith("#999999"))
                break;
            
            currentZoneData.Add(line);
        }
        
        // Parse the last zone if any data remains
        if (currentZoneData.Count > 0)
        {
            try
            {
                var zoneDataString = string.Join("\n", currentZoneData);
                var zone = _parser.ParseZone(zoneDataString);
                zones.Add(zone);
            }
            catch (ParseException ex)
            {
                Console.WriteLine($"Error parsing final zone: {ex.Message}");
            }
        }
        
        return zones;
    }
    
    /// <summary>
    /// Determines if a zone should be reset
    /// </summary>
    private bool ShouldZoneReset(Zone zone)
    {
        if (zone.ResetMode == ResetMode.Never)
            return false;
            
        if (zone.Age < zone.ResetTime)
            return false;
            
        if (zone.ResetMode == ResetMode.Always)
            return true;
            
        // For WhenEmpty, we'd need to check player count
        // For now, assume empty
        return zone.ResetMode == ResetMode.WhenEmpty;
    }
}