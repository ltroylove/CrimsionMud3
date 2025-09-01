using C3Mud.Core.Networking;
using FluentAssertions;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace C3Mud.Tests.Integration;

/// <summary>
/// Integration tests that verify all networking components work together
/// These tests should all FAIL initially since we haven't implemented the components
/// </summary>
public class NetworkingIntegrationTests : IDisposable
{
    private ITcpServer? _server;
    private const int TestPort = 4001; // Use different port to avoid conflicts

    public void Dispose()
    {
        _server?.StopAsync().Wait(1000);
        _server = null;
    }

    private ITcpServer CreateTcpServer()
    {
        return new C3Mud.Core.Networking.TcpServer();
    }

    [Fact]
    public async Task FullConnection_ShouldHandleCompleteClientSession()
    {
        // Arrange
        _server = CreateTcpServer();
        await _server.StartAsync(TestPort);

        // Act & Assert - Full client session simulation
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", TestPort);
        
        using var stream = client.GetStream();
        
        // Should receive initial telnet negotiation
        var buffer = new byte[1024];
        var bytesRead = await stream.ReadAsync(buffer);
        bytesRead.Should().BeGreaterThan(0);
        
        // Should receive login prompt
        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        response.Should().Contain("name"); // Login prompt
        
        // Send player name
        var nameData = Encoding.UTF8.GetBytes("TestPlayer\r\n");
        await stream.WriteAsync(nameData);
        
        // Should receive password prompt or new player creation
        bytesRead = await stream.ReadAsync(buffer);
        response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        response.Should().MatchRegex(".*(password|new).*", "Should ask for password or indicate new player");
    }

    [Fact]
    public async Task TelnetNegotiation_ShouldHandleStandardSequences()
    {
        // Arrange
        _server = CreateTcpServer();
        await _server.StartAsync(TestPort);

        // Act
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", TestPort);
        using var stream = client.GetStream();

        // Send telnet negotiation
        var telnetSequence = new byte[] { 
            TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.SUPPRESS_GO_AHEAD 
        };
        await stream.WriteAsync(telnetSequence);

        // Assert
        var buffer = new byte[1024];
        var bytesRead = await stream.ReadAsync(buffer);
        
        // Should receive appropriate response
        buffer.Take(bytesRead).Should().Contain(TelnetConstants.IAC);
    }

    [Fact]
    public async Task MultipleClients_ShouldHandleConcurrentConnections()
    {
        // Arrange
        _server = CreateTcpServer();
        await _server.StartAsync(TestPort);
        const int clientCount = 10;

        // Act
        var clientTasks = new List<Task>();
        
        for (int i = 0; i < clientCount; i++)
        {
            clientTasks.Add(SimulateClient(i));
        }

        await Task.WhenAll(clientTasks);

        // Assert
        _server.ActiveConnections.Should().Be(clientCount);
    }

    [Fact]
    public async Task ColorCodes_ShouldBeProcessedEndToEnd()
    {
        // Arrange
        _server = CreateTcpServer();
        await _server.StartAsync(TestPort);

        // Act
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", TestPort);
        using var stream = client.GetStream();
        
        // Complete basic login process (this would need to be implemented)
        await CompleteLogin(stream, "TestPlayer");
        
        // Send command that should return colored text
        var commandData = Encoding.UTF8.GetBytes("look\r\n");
        await stream.WriteAsync(commandData);

        // Assert
        var buffer = new byte[4096];
        var bytesRead = await stream.ReadAsync(buffer);
        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        // Should contain ANSI color codes
        response.Should().MatchRegex(@"\x1B\[[0-9;]*m", "Should contain ANSI escape sequences");
    }

    [Fact]
    public async Task CommandProcessing_ShouldHandleBasicMudCommands()
    {
        // Arrange
        _server = CreateTcpServer();
        await _server.StartAsync(TestPort);

        // Act
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", TestPort);
        using var stream = client.GetStream();
        
        await CompleteLogin(stream, "TestPlayer");
        
        var commands = new[] { "look", "inventory", "score", "who", "help" };
        
        // Assert
        foreach (var command in commands)
        {
            var commandData = Encoding.UTF8.GetBytes($"{command}\r\n");
            await stream.WriteAsync(commandData);
            
            var buffer = new byte[4096];
            var bytesRead = await stream.ReadAsync(buffer);
            bytesRead.Should().BeGreaterThan(0, $"Command '{command}' should return response");
            
            var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            response.Should().NotBeNullOrEmpty($"Command '{command}' should return meaningful response");
        }
    }

    [Fact]
    public async Task DisconnectHandling_ShouldCleanupProperly()
    {
        // Arrange
        _server = CreateTcpServer();
        await _server.StartAsync(TestPort);
        
        var initialConnections = _server.ActiveConnections;

        // Act
        using (var client = new TcpClient())
        {
            await client.ConnectAsync("127.0.0.1", TestPort);
            _server.ActiveConnections.Should().Be(initialConnections + 1);
        } // Client disposed/disconnected here

        // Allow time for cleanup
        await Task.Delay(100);

        // Assert
        _server.ActiveConnections.Should().Be(initialConnections);
    }

    [Fact]
    public async Task RateLimiting_ShouldThrottleSpammingClients()
    {
        // Arrange
        _server = CreateTcpServer();
        await _server.StartAsync(TestPort);

        // Act
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", TestPort);
        using var stream = client.GetStream();
        
        await CompleteLogin(stream, "SpammerPlayer");
        
        // Send many commands rapidly
        var responses = new List<string>();
        for (int i = 0; i < 50; i++)
        {
            var commandData = Encoding.UTF8.GetBytes($"say spam{i}\r\n");
            await stream.WriteAsync(commandData);
            
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer);
            if (bytesRead > 0)
            {
                responses.Add(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            }
        }

        // Assert
        // Should eventually receive rate limiting message or no response
        responses.Should().Contain(r => r.Contains("slow") || r.Contains("rate") || r.Contains("spam"));
    }

    [Fact]
    public async Task LargeMessage_ShouldBeHandledCorrectly()
    {
        // Arrange
        _server = CreateTcpServer();
        await _server.StartAsync(TestPort);
        
        // Act
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", TestPort);
        using var stream = client.GetStream();
        
        await CompleteLogin(stream, "TestPlayer");
        
        // Send a very long command
        var longCommand = "say " + new string('X', 2000) + "\r\n";
        var commandData = Encoding.UTF8.GetBytes(longCommand);
        await stream.WriteAsync(commandData);

        // Assert
        var buffer = new byte[8192];
        var bytesRead = await stream.ReadAsync(buffer);
        bytesRead.Should().BeGreaterThan(0);
        
        // Should handle gracefully - either process it or return error message
        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task EchoControl_ShouldWorkForPasswordInput()
    {
        // Arrange
        _server = CreateTcpServer();
        await _server.StartAsync(TestPort);

        // Act
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", TestPort);
        using var stream = client.GetStream();
        
        // Read initial negotiation and login prompt
        var buffer = new byte[1024];
        await stream.ReadAsync(buffer);
        
        // Send name
        var nameData = Encoding.UTF8.GetBytes("TestPlayer\r\n");
        await stream.WriteAsync(nameData);
        
        // Should receive password prompt with echo off sequence
        var bytesRead = await stream.ReadAsync(buffer);
        
        // Assert
        // Should contain telnet echo off sequence
        buffer.Take(bytesRead).Should().Contain(TelnetConstants.IAC);
        buffer.Take(bytesRead).Should().Contain(TelnetConstants.WILL);
        buffer.Take(bytesRead).Should().Contain(TelnetConstants.ECHO);
    }

    // Helper methods
    private async Task SimulateClient(int clientId)
    {
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", TestPort);
        
        using var stream = client.GetStream();
        
        // Basic interaction
        var buffer = new byte[1024];
        await stream.ReadAsync(buffer); // Read welcome/negotiation
        
        var nameData = Encoding.UTF8.GetBytes($"Client{clientId}\r\n");
        await stream.WriteAsync(nameData);
        
        await stream.ReadAsync(buffer); // Read response
    }

    private async Task CompleteLogin(NetworkStream stream, string playerName)
    {
        var buffer = new byte[1024];
        
        // Read initial prompt
        await stream.ReadAsync(buffer);
        
        // Send name
        var nameData = Encoding.UTF8.GetBytes($"{playerName}\r\n");
        await stream.WriteAsync(nameData);
        
        // Read password prompt or new player prompt
        await stream.ReadAsync(buffer);
        
        // For testing, assume new player or simple password
        var passwordData = Encoding.UTF8.GetBytes("testpass\r\n");
        await stream.WriteAsync(passwordData);
        
        // Read game login confirmation
        await stream.ReadAsync(buffer);
    }
}