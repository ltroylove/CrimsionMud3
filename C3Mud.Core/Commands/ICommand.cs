using C3Mud.Core.Players;

namespace C3Mud.Core.Commands;

/// <summary>
/// Represents a command that can be executed by a player
/// Based on the original command_info structure from parser.h
/// </summary>
public interface ICommand
{
    /// <summary>
    /// The command name (e.g., "look", "quit", "who")
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Alternative names for this command (e.g., "l" for "look")
    /// </summary>
    string[] Aliases { get; }
    
    /// <summary>
    /// Minimum player position required to use this command
    /// </summary>
    PlayerPosition MinimumPosition { get; }
    
    /// <summary>
    /// Minimum player level required to use this command
    /// </summary>
    int MinimumLevel { get; }
    
    /// <summary>
    /// Whether this command can be used by NPCs
    /// </summary>
    bool AllowMob { get; }
    
    /// <summary>
    /// Whether this command is enabled
    /// </summary>
    bool IsEnabled { get; }
    
    /// <summary>
    /// Execute the command
    /// </summary>
    /// <param name="player">The player executing the command</param>
    /// <param name="arguments">Command arguments</param>
    /// <param name="commandId">Original command ID for compatibility</param>
    /// <returns>Task representing the command execution</returns>
    Task ExecuteAsync(IPlayer player, string arguments, int commandId);
}