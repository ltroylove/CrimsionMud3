using C3Mud.Core.Networking;

namespace C3Mud.Core.Players;

/// <summary>
/// Represents a player in the MUD
/// Based on original char_data structure from structs.h
/// </summary>
public interface IPlayer
{
    /// <summary>
    /// Player's unique ID
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Player's name
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Player's current level
    /// </summary>
    int Level { get; }
    
    /// <summary>
    /// Player's current position
    /// </summary>
    PlayerPosition Position { get; set; }
    
    /// <summary>
    /// Whether player is connected
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Player's connection descriptor
    /// </summary>
    IConnectionDescriptor? Connection { get; }
    
    /// <summary>
    /// Send a message to the player
    /// </summary>
    Task SendMessageAsync(string message);
    
    /// <summary>
    /// Send a formatted message to the player with color codes
    /// </summary>
    Task SendFormattedMessageAsync(string message);
    
    /// <summary>
    /// Disconnect the player
    /// </summary>
    Task DisconnectAsync(string reason = "Goodbye!");
}