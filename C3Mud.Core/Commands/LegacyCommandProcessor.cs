using C3Mud.Core.Commands.Basic;
using C3Mud.Core.Players;
using Microsoft.Extensions.Logging;

namespace C3Mud.Core.Commands;

/// <summary>
/// Legacy-compatible command processor matching original MUD behavior
/// </summary>
public class LegacyCommandProcessor : ICommandProcessor
{
    private readonly CommandRegistry _commandRegistry;
    private readonly LegacyCommandParser _parser;
    private readonly ILogger<LegacyCommandProcessor> _logger;
    private ICommand? _lastExecutedCommand;
    
    public LegacyCommandProcessor(ILogger<LegacyCommandProcessor>? logger = null)
    {
        _commandRegistry = new CommandRegistry();
        _parser = new LegacyCommandParser();
        _logger = logger ?? CreateDefaultLogger();
        
        RegisterDefaultCommands();
    }
    
    /// <summary>
    /// Processes a command input from a player
    /// </summary>
    public async Task ProcessCommandAsync(IPlayer player, string input)
    {
        try
        {
            // Parse the command input
            var parsedCommand = _parser.ParseCommand(input);
            
            if (parsedCommand == null)
            {
                await SendUnknownCommandMessage(player);
                return;
            }
            
            // Resolve command type
            var commandType = _commandRegistry.ResolveCommand(parsedCommand.CommandName);
            
            if (commandType == null)
            {
                await SendUnknownCommandMessage(player);
                return;
            }
            
            // Create command instance
            var command = CreateCommandInstance(commandType);
            
            if (command == null)
            {
                await SendErrorMessage(player, "Command execution failed");
                return;
            }
            
            // TODO: MISSING BUSINESS LOGIC - No command validation or restrictions
            // REQUIRED FUNCTIONALITY:
            // 1. Check if player has required level for command
            // 2. Check if player is in correct position (sitting, standing, fighting, etc.)
            // 3. Validate command can be executed in current game state
            // 4. Handle command cooldowns and restrictions
            // 5. Check if player is silenced/muted
            // 6. Validate player is not linkless/void
            // 7. Handle immortal-only commands and permissions
            
            _lastExecutedCommand = command;
            
            // Execute the command with original command ID (for compatibility)
            var commandId = GetOriginalCommandId(parsedCommand.CommandName);
            await command.ExecuteAsync(player, parsedCommand.Arguments, commandId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command for player {PlayerName}: {Input}", player.Name, input);
            await SendErrorMessage(player, "An error occurred processing your command");
        }
    }
    
    /// <summary>
    /// Gets the last executed command (for testing)
    /// </summary>
    public ICommand? GetLastExecutedCommand()
    {
        return _lastExecutedCommand;
    }
    
    /// <summary>
    /// Gets registered command aliases
    /// </summary>
    public Dictionary<string, string> GetRegisteredAliases()
    {
        return _commandRegistry.GetAliases();
    }
    
    /// <summary>
    /// Registers default commands matching original MUD
    /// </summary>
    private void RegisterDefaultCommands()
    {
        // Register basic commands
        _commandRegistry.RegisterCommand("look", typeof(LookCommand));
        _commandRegistry.RegisterCommand("quit", typeof(QuitCommand));
        _commandRegistry.RegisterCommand("help", typeof(HelpCommand));
        _commandRegistry.RegisterCommand("score", typeof(ScoreCommand));
        _commandRegistry.RegisterCommand("who", typeof(WhoCommand));
        _commandRegistry.RegisterCommand("say", typeof(SayCommand));
        
        // Register aliases (matching original MUD)
        _commandRegistry.RegisterAlias("l", "look");
        _commandRegistry.RegisterAlias("loo", "look");
        _commandRegistry.RegisterAlias("q", "quit");
        _commandRegistry.RegisterAlias("qui", "quit");
        _commandRegistry.RegisterAlias("sc", "score");
        _commandRegistry.RegisterAlias("'", "say");
    }
    
    /// <summary>
    /// Creates a command instance from type
    /// </summary>
    private ICommand? CreateCommandInstance(Type commandType)
    {
        try
        {
            return Activator.CreateInstance(commandType) as ICommand;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create command instance: {CommandType}", commandType.Name);
            return null;
        }
    }
    
    /// <summary>
    /// Sends unknown command message (matching original MUD)
    /// </summary>
    private async Task SendUnknownCommandMessage(IPlayer player)
    {
        if (player.Connection != null)
        {
            await player.Connection.SendDataAsync("Huh?\r\n");
        }
    }
    
    /// <summary>
    /// Sends error message to player
    /// </summary>
    private async Task SendErrorMessage(IPlayer player, string message)
    {
        if (player.Connection != null)
        {
            await player.Connection.SendDataAsync($"{message}\r\n");
        }
    }
    
    /// <summary>
    /// Gets original command ID for compatibility with legacy systems
    /// </summary>
    private static int GetOriginalCommandId(string commandName)
    {
        return commandName.ToLowerInvariant() switch
        {
            "look" => 15,
            "say" => 17,
            "quit" => 73,
            "who" => 39,
            "score" => 14,
            "help" => 38,
            _ => -1 // Unknown command
        };
    }
    
    /// <summary>
    /// Creates a default logger if none provided
    /// </summary>
    private static ILogger<LegacyCommandProcessor> CreateDefaultLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        return loggerFactory.CreateLogger<LegacyCommandProcessor>();
    }
}

/// <summary>
/// Interface for command processor
/// </summary>
public interface ICommandProcessor
{
    Task ProcessCommandAsync(IPlayer player, string input);
}

/// <summary>
/// Parsed command data
/// </summary>
public class ParsedCommand
{
    public string CommandName { get; init; } = string.Empty;
    public string Arguments { get; init; } = string.Empty;
}

/// <summary>
/// Legacy command parser matching original MUD parsing behavior
/// </summary>
public class LegacyCommandParser
{
    /// <summary>
    /// Parses command input into command name and arguments
    /// </summary>
    public ParsedCommand? ParseCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;
            
        // Trim whitespace and normalize (like original parser.c)
        input = input.Trim();
        
        if (string.IsNullOrEmpty(input))
            return null;
            
        // Split into command and arguments
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length == 0)
            return null;
            
        var commandName = parts[0].ToLower(); // Original MUD normalized to lowercase
        var arguments = parts.Length > 1 ? parts[1] : string.Empty;
        
        return new ParsedCommand
        {
            CommandName = commandName,
            Arguments = arguments
        };
    }
}