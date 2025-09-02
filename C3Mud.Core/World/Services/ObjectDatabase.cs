using C3Mud.Core.World.Models;
using C3Mud.Core.World.Parsers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Thread-safe in-memory database for object templates
/// Provides fast O(1) lookup using Dictionary storage
/// </summary>
public class ObjectDatabase : IObjectDatabase
{
    private readonly ConcurrentDictionary<int, WorldObject> _objects = new();
    private readonly ObjectFileParser _parser;
    
    /// <summary>
    /// Initializes a new ObjectDatabase with empty object template storage
    /// </summary>
    public ObjectDatabase()
    {
        _parser = new ObjectFileParser();
    }
    
    /// <summary>
    /// Gets the count of loaded object templates
    /// </summary>
    public int ObjectCount => _objects.Count;
    
    /// <summary>
    /// Loads an object template into the database
    /// If an object with the same VirtualNumber already exists, it will be replaced
    /// </summary>
    /// <param name="obj">Object template to load</param>
    /// <exception cref="ArgumentNullException">Thrown when obj is null</exception>
    public void LoadObject(WorldObject obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));
            
        _objects.AddOrUpdate(obj.VirtualNumber, obj, (key, oldValue) => obj);
    }
    
    /// <summary>
    /// Retrieves an object template by virtual number
    /// </summary>
    /// <param name="virtualNumber">Virtual number of the object to retrieve</param>
    /// <returns>Object template if found, null otherwise</returns>
    public WorldObject? GetObject(int virtualNumber)
    {
        return _objects.TryGetValue(virtualNumber, out var obj) ? obj : null;
    }
    
    /// <summary>
    /// Gets all loaded object templates
    /// </summary>
    /// <returns>Collection of all object templates</returns>
    public IEnumerable<WorldObject> GetAllObjects()
    {
        return _objects.Values.ToList(); // Return a copy to avoid concurrency issues
    }
    
    /// <summary>
    /// Clears all object templates from the database
    /// </summary>
    public void Clear()
    {
        _objects.Clear();
    }
    
    /// <summary>
    /// Checks if an object template exists in the database
    /// </summary>
    /// <param name="virtualNumber">Virtual number to check</param>
    /// <returns>True if object exists, false otherwise</returns>
    public bool ObjectExists(int virtualNumber)
    {
        return _objects.ContainsKey(virtualNumber);
    }
    
    /// <summary>
    /// Loads multiple object templates efficiently
    /// </summary>
    /// <param name="objects">Collection of objects to load</param>
    public void LoadObjects(IEnumerable<WorldObject> objects)
    {
        if (objects == null)
            throw new ArgumentNullException(nameof(objects));
            
        foreach (var obj in objects)
        {
            if (obj != null)
                LoadObject(obj);
        }
    }
    
    /// <summary>
    /// Gets object templates by type
    /// </summary>
    /// <param name="objectType">Type of objects to retrieve</param>
    /// <returns>Collection of objects of the specified type</returns>
    public IEnumerable<WorldObject> GetObjectsByType(ObjectType objectType)
    {
        return _objects.Values.Where(obj => obj.ObjectType == objectType).ToList();
    }
    
    /// <summary>
    /// Gets object templates within a virtual number range
    /// </summary>
    /// <param name="minVnum">Minimum virtual number (inclusive)</param>
    /// <param name="maxVnum">Maximum virtual number (inclusive)</param>
    /// <returns>Collection of objects in the specified range</returns>
    public IEnumerable<WorldObject> GetObjectsInRange(int minVnum, int maxVnum)
    {
        return _objects.Values
            .Where(obj => obj.VirtualNumber >= minVnum && obj.VirtualNumber <= maxVnum)
            .OrderBy(obj => obj.VirtualNumber)
            .ToList();
    }
    
    /// <summary>
    /// Loads multiple object templates from a .obj file asynchronously
    /// Parses the entire .obj file and loads all object templates into memory
    /// </summary>
    /// <param name="filePath">Path to the .obj file</param>
    /// <returns>Task representing the async operation</returns>
    public async Task LoadObjectsAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Object file not found: {filePath}");

        try
        {
            var fileContent = await File.ReadAllTextAsync(filePath);
            var objects = _parser.ParseFile(fileContent);
            
            foreach (var obj in objects)
            {
                LoadObject(obj);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load objects from {filePath}: {ex.Message}", ex);
        }
    }
}