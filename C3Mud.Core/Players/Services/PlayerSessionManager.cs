using C3Mud.Core.Networking;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace C3Mud.Core.Players.Services;

/// <summary>
/// Manages player sessions and their connections
/// </summary>
public class PlayerSessionManager
{
    private readonly ConcurrentDictionary<string, PlayerSession> _sessions = new();
    private readonly ILogger<PlayerSessionManager> _logger;
    
    public PlayerSessionManager(ILogger<PlayerSessionManager>? logger = null)
    {
        _logger = logger ?? CreateDefaultLogger();
    }
    
    /// <summary>
    /// Authenticates a player and creates a session
    /// </summary>
    public async Task<PlayerSession> AuthenticateAndCreateSessionAsync(IConnectionDescriptor connection, string username, string password)
    {
        // This would integrate with AuthenticationService
        // For now, create a basic implementation
        var player = new Player(Guid.NewGuid().ToString())
        {
            Name = username,
            Connection = connection
        };
        
        var session = new PlayerSession(player, connection);
        RegisterSession(session);
        
        return session;
    }
    
    /// <summary>
    /// Registers a player session
    /// </summary>
    public void RegisterSession(PlayerSession session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));
            
        var playerName = session.Player.Name;
        
        // Handle reconnection - close existing connection
        if (_sessions.TryGetValue(playerName, out var existingSession))
        {
            _ = Task.Run(() => existingSession.Connection.CloseAsync());
            _logger.LogInformation("Player {PlayerName} reconnected, closing previous connection", playerName);
        }
        
        _sessions.AddOrUpdate(playerName, session, (_, _) => session);
        _logger.LogInformation("Registered session for player: {PlayerName}", playerName);
    }
    
    /// <summary>
    /// Gets a player session by name
    /// </summary>
    public PlayerSession? GetPlayerSession(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            return null;
            
        return _sessions.TryGetValue(playerName, out var session) && session.IsActive ? session : null;
    }
    
    /// <summary>
    /// Processes a command for a player session
    /// </summary>
    public async Task ProcessPlayerCommandAsync(PlayerSession session, string command)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));
            
        if (!session.IsActive)
            return;
            
        // Command throttling (prevent spam)
        if (session.ShouldThrottleCommand())
        {
            await session.Connection.SendDataAsync("You are typing too fast, slow down!\r\n");
            return;
        }
        
        // Would integrate with command processor here
        // For now, basic implementation
        await ProcessBasicCommand(session.Player, command);
    }
    
    /// <summary>
    /// Disconnects a player and cleans up session
    /// </summary>
    public async Task DisconnectPlayerAsync(string playerName)
    {
        if (_sessions.TryRemove(playerName, out var session))
        {
            await session.Connection.CloseAsync();
            _logger.LogInformation("Disconnected player: {PlayerName}", playerName);
        }
    }
    
    /// <summary>
    /// Processes idle timeouts
    /// </summary>
    public async Task ProcessIdleTimeoutsAsync()
    {
        var idleTimeout = TimeSpan.FromMinutes(30); // Like original MUD
        var now = DateTime.UtcNow;
        
        var idleSessions = _sessions.Values
            .Where(s => s.IsActive && (now - s.LastActivity) > idleTimeout)
            .ToList();
            
        foreach (var session in idleSessions)
        {
            await session.Connection.SendDataAsync("You have been idle too long and have been disconnected.\r\n");
            await DisconnectPlayerAsync(session.Player.Name);
        }
    }
    
    private async Task ProcessBasicCommand(IPlayer player, string command)
    {
        // Basic command processing for testing
        var normalizedCommand = command.Trim().ToLower();
        
        switch (normalizedCommand)
        {
            case "look":
            case "l":
                await player.SendMessageAsync("You are standing in a test room.\r\n");
                break;
                
            case "quit":
            case "q":
                await player.DisconnectAsync("Goodbye!");
                break;
                
            default:
                await player.SendMessageAsync("Huh?\r\n");
                break;
        }
    }
    
    private static ILogger<PlayerSessionManager> CreateDefaultLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        return loggerFactory.CreateLogger<PlayerSessionManager>();
    }
}

/// <summary>
/// Represents an active player session
/// </summary>
public class PlayerSession
{
    private DateTime _lastCommandTime = DateTime.MinValue;
    private int _commandCount = 0;
    
    public IPlayer Player { get; }
    public IConnectionDescriptor Connection { get; }
    public DateTime LastActivity { get; private set; } = DateTime.UtcNow;
    public bool IsActive => Connection.IsConnected;
    
    public PlayerSession(IPlayer player, IConnectionDescriptor connection)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    /// <summary>
    /// Simulates idle time for testing
    /// </summary>
    public void SimulateIdleTime(TimeSpan idleTime)
    {
        LastActivity = DateTime.UtcNow - idleTime;
    }
    
    /// <summary>
    /// Checks if command should be throttled
    /// </summary>
    public bool ShouldThrottleCommand()
    {
        var now = DateTime.UtcNow;
        
        if (now - _lastCommandTime < TimeSpan.FromSeconds(1))
        {
            _commandCount++;
            if (_commandCount > 5) // Allow 5 rapid commands then throttle
            {
                return true;
            }
        }
        else
        {
            _commandCount = 0;
        }
        
        _lastCommandTime = now;
        LastActivity = now;
        return false;
    }
}