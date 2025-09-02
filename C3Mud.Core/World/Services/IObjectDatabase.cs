using C3Mud.Core.World.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Interface for object database that stores and retrieves object templates
/// Provides fast O(1) lookup for object templates by virtual number
/// </summary>
public interface IObjectDatabase
{
    /// <summary>
    /// Loads an object template into the database
    /// If an object with the same VirtualNumber already exists, it will be replaced
    /// </summary>
    /// <param name="obj">Object template to load</param>
    void LoadObject(WorldObject obj);
    
    /// <summary>
    /// Retrieves an object template by virtual number
    /// </summary>
    /// <param name="virtualNumber">Virtual number of the object to retrieve</param>
    /// <returns>Object template if found, null otherwise</returns>
    WorldObject? GetObject(int virtualNumber);
    
    /// <summary>
    /// Gets all loaded object templates
    /// </summary>
    /// <returns>Collection of all object templates</returns>
    IEnumerable<WorldObject> GetAllObjects();
    
    /// <summary>
    /// Gets the count of loaded object templates
    /// </summary>
    int ObjectCount { get; }
    
    /// <summary>
    /// Clears all object templates from the database
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Checks if an object template exists in the database
    /// </summary>
    /// <param name="virtualNumber">Virtual number to check</param>
    /// <returns>True if object exists, false otherwise</returns>
    bool ObjectExists(int virtualNumber);
    
    /// <summary>
    /// Loads multiple object templates from a .obj file asynchronously
    /// </summary>
    /// <param name="filePath">Path to the .obj file</param>
    /// <returns>Task representing the async operation</returns>
    Task LoadObjectsAsync(string filePath);
}