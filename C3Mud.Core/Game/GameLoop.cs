using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using C3Mud.Core.Commands;
using C3Mud.Core.Commands.Movement;
using C3Mud.Core.Networking;
using C3Mud.Core.Players;
using C3Mud.Core.World.Services;

namespace C3Mud.Core.Game;

/// <summary>
/// Main game loop service - handles player input processing and game updates
/// Based on the original MUD game loop from comm.c
/// </summary>
public class GameLoop : BackgroundService
{
    private readonly ITcpServer _tcpServer;
    private readonly IConnectionManager _connectionManager;
    private readonly ICommandRegistry _commandRegistry;
    private readonly IWorldDatabase? _worldDatabase;
    private readonly ILogger<GameLoop> _logger;
    private readonly ConcurrentDictionary<string, IPlayer> _players = new();

    public GameLoop(
        ITcpServer tcpServer,
        IConnectionManager connectionManager,
        ICommandRegistry commandRegistry,
        ILogger<GameLoop> logger,
        IWorldDatabase? worldDatabase = null)
    {
        _tcpServer = tcpServer;
        _connectionManager = connectionManager;
        _commandRegistry = commandRegistry;
        _worldDatabase = worldDatabase;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Game loop starting...");

        // Register basic commands
        RegisterBasicCommands();

        var gameLoopTasks = new List<Task>
        {
            ProcessPlayerInputTask(stoppingToken),
            ProcessGameUpdatesTask(stoppingToken),
            ProcessConnectionsTask(stoppingToken)
        };

        try
        {
            await Task.WhenAll(gameLoopTasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Game loop shutting down...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Game loop encountered an error");
            throw;
        }
    }

    /// <summary>
    /// Register all basic commands with the command registry
    /// </summary>
    private void RegisterBasicCommands()
    {
        try
        {
            // Register basic commands
            _commandRegistry.RegisterCommand(new Commands.Basic.LookCommand());
            _commandRegistry.RegisterCommand(new Commands.Basic.QuitCommand());
            _commandRegistry.RegisterCommand(new Commands.Basic.WhoCommand());
            _commandRegistry.RegisterCommand(new Commands.Basic.ScoreCommand());
            _commandRegistry.RegisterCommand(new Commands.Basic.HelpCommand());
            _commandRegistry.RegisterCommand(new Commands.Basic.SayCommand());

            var basicCommandCount = 6;

            // Register movement commands if WorldDatabase is available
            if (_worldDatabase != null)
            {
                _commandRegistry.RegisterCommand(new NorthCommand(_worldDatabase));
                _commandRegistry.RegisterCommand(new SouthCommand(_worldDatabase));
                _commandRegistry.RegisterCommand(new EastCommand(_worldDatabase));
                _commandRegistry.RegisterCommand(new WestCommand(_worldDatabase));
                _commandRegistry.RegisterCommand(new UpCommand(_worldDatabase));
                _commandRegistry.RegisterCommand(new DownCommand(_worldDatabase));
                
                var movementCommandCount = 6;
                _logger.LogInformation("Registered {TotalCommandCount} commands ({BasicCommandCount} basic + {MovementCommandCount} movement)", 
                    basicCommandCount + movementCommandCount, basicCommandCount, movementCommandCount);
            }
            else
            {
                _logger.LogWarning("WorldDatabase not available - movement commands not registered");
                _logger.LogInformation("Registered {CommandCount} basic commands", basicCommandCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register basic commands");
        }
    }

    /// <summary>
    /// Process player input continuously
    /// </summary>
    private async Task ProcessPlayerInputTask(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connections = _connectionManager.GetAllConnections();
                var inputTasks = new List<Task>();

                foreach (var connection in connections)
                {
                    if (connection.IsConnected && connection.HasPendingInput)
                    {
                        inputTasks.Add(ProcessConnectionInput(connection));
                    }
                }

                if (inputTasks.Count > 0)
                {
                    await Task.WhenAll(inputTasks);
                }

                // Small delay to prevent busy waiting
                await Task.Delay(10, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in player input processing task");
                await Task.Delay(100, stoppingToken);
            }
        }
    }

    /// <summary>
    /// Process input from a specific connection
    /// </summary>
    private async Task ProcessConnectionInput(IConnectionDescriptor connection)
    {
        try
        {
            var input = await connection.ReadInputAsync();
            if (!string.IsNullOrWhiteSpace(input))
            {
                var player = GetOrCreatePlayer(connection);
                await _commandRegistry.ExecuteCommandAsync(player, input.Trim());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing input for connection {ConnectionId}", connection.Id);
        }
    }

    /// <summary>
    /// Get existing player or create new one for connection
    /// </summary>
    private IPlayer GetOrCreatePlayer(IConnectionDescriptor connection)
    {
        var connectionId = connection.Id;
        
        if (_players.TryGetValue(connectionId, out var existingPlayer))
        {
            return existingPlayer;
        }

        // Create new player
        var player = new Player(connectionId, connection)
        {
            Name = $"Player-{connectionId[..8]}", // Use first 8 chars of connection ID as temp name
            Level = 1,
            Position = PlayerPosition.Standing
        };

        _players.TryAdd(connectionId, player);
        
        // Send welcome message
        _ = Task.Run(async () =>
        {
            await SendWelcomeMessage(player);
        });

        _logger.LogInformation("Created new player: {PlayerId}", connectionId);
        return player;
    }

    /// <summary>
    /// Send welcome message to new players
    /// </summary>
    private static async Task SendWelcomeMessage(IPlayer player)
    {
        var welcomeMessage = @"&W
=====================================
    Welcome to C3MUD (Development)
=====================================

&YThis is a modern C# rewrite of Crimson-2-MUD&N
&KDevelopment Version - Many features coming soon&N

&CType '&Whelp&C' for a list of available commands.&N
&CType '&Wlook&C' to examine your surroundings.&N
&CType '&Wquit&C' when you want to leave.&N

&GEnjoy your stay!&N
";

        await player.SendFormattedMessageAsync(welcomeMessage);
    }

    /// <summary>
    /// Process game updates (combat, regeneration, etc.)
    /// </summary>
    private async Task ProcessGameUpdatesTask(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Game update logic will go here
                // - Combat rounds
                // - HP/MP regeneration  
                // - Spell effects expiration
                // - NPC AI updates
                // - Zone resets
                
                // For now, just cleanup disconnected players periodically
                await CleanupDisconnectedPlayers();

                // Update every second
                await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in game updates task");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    /// <summary>
    /// Process connection management
    /// </summary>
    private async Task ProcessConnectionsTask(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Cleanup stale connections
                await _connectionManager.CleanupConnectionsAsync();

                // Cleanup every 30 seconds
                await Task.Delay(30000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in connection management task");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    /// <summary>
    /// Remove players who have disconnected
    /// </summary>
    private async Task CleanupDisconnectedPlayers()
    {
        var disconnectedPlayers = _players
            .Where(kvp => !kvp.Value.IsConnected)
            .ToList();

        foreach (var (playerId, player) in disconnectedPlayers)
        {
            _players.TryRemove(playerId, out _);
            _logger.LogInformation("Removed disconnected player: {PlayerId}", playerId);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Get all currently online players
    /// </summary>
    public IReadOnlyCollection<IPlayer> GetOnlinePlayers()
    {
        return _players.Values.Where(p => p.IsConnected).ToList().AsReadOnly();
    }

    /// <summary>
    /// Get player count
    /// </summary>
    public int GetPlayerCount()
    {
        return _players.Count(kvp => kvp.Value.IsConnected);
    }

    public override void Dispose()
    {
        _logger.LogInformation("Game loop disposed");
        base.Dispose();
    }
}