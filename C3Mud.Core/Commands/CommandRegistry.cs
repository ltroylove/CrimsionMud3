using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using C3Mud.Core.Players;

namespace C3Mud.Core.Commands;

/// <summary>
/// Command registry and dispatcher based on original cmd_info array
/// Manages command registration, lookup, and execution
/// </summary>
public interface ICommandRegistry
{
    /// <summary>
    /// Register a command
    /// </summary>
    void RegisterCommand(ICommand command);
    
    /// <summary>
    /// Find a command by name or alias
    /// </summary>
    ICommand? FindCommand(string commandName);
    
    /// <summary>
    /// Get all registered commands
    /// </summary>
    IReadOnlyCollection<ICommand> GetAllCommands();
    
    /// <summary>
    /// Execute a command
    /// </summary>
    Task<bool> ExecuteCommandAsync(IPlayer player, string commandLine);
}

/// <summary>
/// Implementation of command registry
/// </summary>
public class CommandRegistry : ICommandRegistry
{
    private readonly ConcurrentDictionary<string, ICommand> _commands = new();
    private readonly ConcurrentDictionary<string, ICommand> _aliases = new();
    private readonly ConcurrentDictionary<string, Type> _commandTypes = new();
    private readonly ConcurrentDictionary<string, string> _aliasToCommand = new();
    private readonly ILogger<CommandRegistry> _logger;

    public CommandRegistry(ILogger<CommandRegistry>? logger = null)
    {
        _logger = logger ?? CreateDefaultLogger();
    }
    
    /// <summary>
    /// Creates a default logger if none provided
    /// </summary>
    private static ILogger<CommandRegistry> CreateDefaultLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        return loggerFactory.CreateLogger<CommandRegistry>();
    }

    /// <summary>
    /// Register a command and all its aliases
    /// </summary>
    public void RegisterCommand(ICommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var commandName = command.Name.ToLowerInvariant();
        
        // Register main command name
        _commands.TryAdd(commandName, command);
        
        // Register all aliases
        foreach (var alias in command.Aliases)
        {
            var aliasKey = alias.ToLowerInvariant();
            _aliases.TryAdd(aliasKey, command);
        }
        
        _logger.LogDebug("Registered command: {CommandName} with {AliasCount} aliases", 
            command.Name, command.Aliases.Length);
    }

    /// <summary>
    /// Find command by name or alias, supporting abbreviation matching
    /// Based on original command lookup logic
    /// </summary>
    public ICommand? FindCommand(string commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName))
            return null;

        var lowerCommandName = commandName.ToLowerInvariant();
        
        // Try exact match first
        if (_commands.TryGetValue(lowerCommandName, out var exactCommand))
            return exactCommand;
            
        if (_aliases.TryGetValue(lowerCommandName, out var exactAlias))
            return exactAlias;

        // Try abbreviation matching for command names
        var commandMatches = _commands
            .Where(kvp => CommandParser.IsAbbreviation(lowerCommandName, kvp.Key))
            .ToList();
            
        if (commandMatches.Count == 1)
            return commandMatches[0].Value;
            
        // Try abbreviation matching for aliases
        var aliasMatches = _aliases
            .Where(kvp => CommandParser.IsAbbreviation(lowerCommandName, kvp.Key))
            .ToList();
            
        if (aliasMatches.Count == 1)
            return aliasMatches[0].Value;

        // If multiple matches, return null (ambiguous command)
        return null;
    }

    /// <summary>
    /// Get all registered commands
    /// </summary>
    public IReadOnlyCollection<ICommand> GetAllCommands()
    {
        return _commands.Values.Distinct().ToList().AsReadOnly();
    }

    /// <summary>
    /// Execute a command line, parsing and dispatching to appropriate command
    /// Based on original command_interpreter() function
    /// </summary>
    public async Task<bool> ExecuteCommandAsync(IPlayer player, string commandLine)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));
            
        if (string.IsNullOrWhiteSpace(commandLine))
            return false;

        try
        {
            // Parse command line
            var (commandName, arguments) = CommandParser.ParseCommandLine(commandLine);
            
            if (string.IsNullOrEmpty(commandName))
                return false;

            // Find command
            var command = FindCommand(commandName);
            if (command == null)
            {
                await player.SendMessageAsync($"Huh? '{commandName}' is not a command.");
                return false;
            }

            // Check if command is enabled
            if (!command.IsEnabled)
            {
                await player.SendMessageAsync("That command is currently disabled.");
                return false;
            }

            // Check player position requirements
            if (player.Position < command.MinimumPosition)
            {
                await player.SendMessageAsync($"You can't do that while {GetPositionDescription(player.Position)}.");
                return false;
            }

            // Check level requirements
            if (player.Level < command.MinimumLevel)
            {
                await player.SendMessageAsync("You don't have the experience to use that command.");
                return false;
            }

            // Execute the command
            var originalCommandId = GetOriginalCommandId(command.Name);
            await command.ExecuteAsync(player, arguments, originalCommandId);
            
            _logger.LogDebug("Player {PlayerName} executed command: {CommandName} {Arguments}", 
                player.Name, command.Name, arguments);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command: {CommandLine} for player {PlayerName}", 
                commandLine, player?.Name ?? "Unknown");
            
            await player.SendMessageAsync("An error occurred while executing that command.");
            return false;
        }
    }

    /// <summary>
    /// Auto-register all commands from the current assembly
    /// </summary>
    public void AutoRegisterCommands()
    {
        var commandTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ICommand).IsAssignableFrom(t))
            .ToList();

        foreach (var commandType in commandTypes)
        {
            try
            {
                if (Activator.CreateInstance(commandType) is ICommand command)
                {
                    RegisterCommand(command);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-register command type: {CommandType}", commandType.Name);
            }
        }
        
        _logger.LogInformation("Auto-registered {CommandCount} command types", commandTypes.Count);
    }
    
    /// <summary>
    /// Register a command by name and type (for legacy compatibility)
    /// </summary>
    public void RegisterCommand(string commandName, Type commandType)
    {
        if (string.IsNullOrEmpty(commandName))
            throw new ArgumentException("Command name cannot be null or empty", nameof(commandName));
            
        if (commandType == null)
            throw new ArgumentNullException(nameof(commandType));
            
        if (!typeof(ICommand).IsAssignableFrom(commandType))
            throw new ArgumentException("Command type must implement ICommand", nameof(commandType));
            
        var lowerCommandName = commandName.ToLowerInvariant();
        _commandTypes.TryAdd(lowerCommandName, commandType);
        
        _logger.LogDebug("Registered command type: {CommandName} -> {CommandType}", commandName, commandType.Name);
    }
    
    /// <summary>
    /// Register a command alias
    /// </summary>
    public void RegisterAlias(string alias, string commandName)
    {
        if (string.IsNullOrEmpty(alias))
            throw new ArgumentException("Alias cannot be null or empty", nameof(alias));
            
        if (string.IsNullOrEmpty(commandName))
            throw new ArgumentException("Command name cannot be null or empty", nameof(commandName));
            
        var lowerAlias = alias.ToLowerInvariant();
        var lowerCommandName = commandName.ToLowerInvariant();
        
        _aliasToCommand.TryAdd(lowerAlias, lowerCommandName);
        
        _logger.LogDebug("Registered alias: {Alias} -> {CommandName}", alias, commandName);
    }
    
    /// <summary>
    /// Resolve command type by name (with alias support)
    /// </summary>
    public Type? ResolveCommand(string commandName)
    {
        if (string.IsNullOrEmpty(commandName))
            return null;
            
        var lowerCommandName = commandName.ToLowerInvariant();
        
        // Try direct command lookup
        if (_commandTypes.TryGetValue(lowerCommandName, out var commandType))
            return commandType;
            
        // Try alias lookup
        if (_aliasToCommand.TryGetValue(lowerCommandName, out var actualCommandName))
        {
            return _commandTypes.TryGetValue(actualCommandName, out commandType) ? commandType : null;
        }
        
        return null;
    }
    
    /// <summary>
    /// Get all registered aliases
    /// </summary>
    public Dictionary<string, string> GetAliases()
    {
        return new Dictionary<string, string>(_aliasToCommand);
    }

    private static string GetPositionDescription(PlayerPosition position)
    {
        return position switch
        {
            PlayerPosition.Dead => "dead",
            PlayerPosition.MortallyWounded => "mortally wounded",
            PlayerPosition.Incapacitated => "incapacitated", 
            PlayerPosition.Stunned => "stunned",
            PlayerPosition.Sleeping => "sleeping",
            PlayerPosition.Resting => "resting",
            PlayerPosition.Sitting => "sitting",
            PlayerPosition.Fighting => "fighting",
            PlayerPosition.Standing => "standing",
            _ => "in an unknown position"
        };
    }

    /// <summary>
    /// Get original command ID for compatibility with legacy systems
    /// Maps command names to their original numeric IDs from parser.h
    /// </summary>
    private static int GetOriginalCommandId(string commandName)
    {
        return commandName.ToLowerInvariant() switch
        {
            "north" => 1,
            "east" => 2,
            "south" => 3,
            "west" => 4,
            "up" => 5,
            "down" => 6,
            "look" => 15,
            "say" => 17,
            "inventory" => 20,
            "quit" => 73,
            "who" => 39,
            "score" => 14,
            "help" => 38,
            "time" => 76,
            _ => -1 // Unknown command
        };
    }
}