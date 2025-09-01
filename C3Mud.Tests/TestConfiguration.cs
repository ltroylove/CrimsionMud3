using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using C3Mud.Core.Networking;

namespace C3Mud.Tests;

/// <summary>
/// Test configuration and test doubles for dependency injection
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Configure services for testing - when implementations are ready
    /// </summary>
    public static IServiceCollection ConfigureTestServices(this IServiceCollection services)
    {
        // Configure logging for tests
        services.AddLogging(builder => 
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Register networking services (when implemented)
        // services.AddScoped<ITcpServer, TcpServer>();
        // services.AddScoped<ITelnetProtocolHandler, TelnetProtocolHandler>();
        // services.AddScoped<IConnectionManager, ConnectionManager>();

        return services;
    }

    /// <summary>
    /// Test constants matching original MUD behavior
    /// </summary>
    public static class TestConstants
    {
        public const int DefaultTestPort = 4001;
        public const int MaxTestConnections = 100;
        public const int CommandTimeoutMs = 100;
        public const int ConnectionTimeoutMs = 5000;
        
        // Original MUD color codes for testing
        public static readonly Dictionary<string, string> MudColorCodes = new()
        {
            { "&N", "\x1B[0m" },  // Normal
            { "&R", "\x1B[31m" }, // Red  
            { "&G", "\x1B[32m" }, // Green
            { "&Y", "\x1B[33m" }, // Yellow
            { "&B", "\x1B[34m" }, // Blue
            { "&M", "\x1B[35m" }, // Magenta
            { "&C", "\x1B[36m" }, // Cyan
            { "&W", "\x1B[37m" }, // White
        };
        
        // Standard MUD commands for testing
        public static readonly string[] BasicCommands = 
        {
            "look", "north", "south", "east", "west", "up", "down",
            "inventory", "score", "who", "help", "quit", "save",
            "say", "tell", "chat", "gossip"
        };
    }
}