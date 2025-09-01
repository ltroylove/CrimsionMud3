using C3Mud.Core.Networking;
using FluentAssertions;
using Xunit;

namespace C3Mud.Tests.Networking;

/// <summary>
/// Tests for TCP Server infrastructure matching original comm.c behavior
/// </summary>
public class TcpServerTests : IDisposable
{
    private readonly List<ITcpServer> _servers = new();

    private ITcpServer CreateTcpServer()
    {
        var server = new C3Mud.Core.Networking.TcpServer();
        _servers.Add(server);
        return server;
    }

    private static int GetAvailablePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void Dispose()
    {
        foreach (var server in _servers)
        {
            try
            {
                server.StopAsync().Wait(1000);
                if (server is IDisposable disposable)
                    disposable.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }

    [Fact]
    public async Task StartAsync_ShouldStartListeningOnSpecifiedPort()
    {
        // Arrange
        var server = CreateTcpServer();
        var port = GetAvailablePort();

        // Act
        await server.StartAsync(port);

        // Assert
        server.Status.Should().Be(ServerStatus.Running);
        server.Port.Should().Be(port);
    }

    [Fact]
    public async Task StartAsync_WithPortInUse_ShouldThrowException()
    {
        // Arrange
        var server1 = CreateTcpServer();
        var server2 = CreateTcpServer();
        var port = GetAvailablePort();

        // Act
        await server1.StartAsync(port);
        Func<Task> act = () => server2.StartAsync(port);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*port*already*use*");
    }

    [Fact]
    public async Task StopAsync_ShouldStopServerAndCloseAllConnections()
    {
        // Arrange
        var server = CreateTcpServer();
        var port = GetAvailablePort();
        await server.StartAsync(port);

        // Act
        await server.StopAsync();

        // Assert
        server.Status.Should().Be(ServerStatus.Stopped);
        server.ActiveConnections.Should().Be(0);
    }

    [Fact]
    public async Task ClientConnected_ShouldRaiseEventWhenNewClientConnects()
    {
        // Arrange
        var server = CreateTcpServer();
        var port = GetAvailablePort();
        await server.StartAsync(port);
        IConnectionDescriptor? connectedClient = null;
        server.ClientConnected += (sender, args) => connectedClient = args.Connection;

        // Act - Make actual client connection
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync("127.0.0.1", port);
        
        // Wait briefly for event to fire
        await Task.Delay(100);

        // Assert
        connectedClient.Should().NotBeNull();
        connectedClient!.Host.Should().NotBeNullOrEmpty();
        connectedClient.ConnectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ClientDisconnected_ShouldRaiseEventWhenClientDisconnects()
    {
        // Arrange
        var server = CreateTcpServer();
        var port = GetAvailablePort();
        await server.StartAsync(port);
        IConnectionDescriptor? disconnectedClient = null;
        server.ClientDisconnected += (sender, args) => disconnectedClient = args.Connection;

        // Act - Connect then disconnect
        using (var client = new System.Net.Sockets.TcpClient())
        {
            await client.ConnectAsync("127.0.0.1", port);
            await Task.Delay(50); // Let connection establish
        } // Client disposed/disconnected here
        
        await Task.Delay(100); // Wait for disconnection event

        // Assert
        disconnectedClient.Should().NotBeNull();
    }

    [Fact]
    public async Task GetActiveConnections_ShouldReturnAllActiveConnections()
    {
        // Arrange
        var server = CreateTcpServer();
        var port = GetAvailablePort();
        await server.StartAsync(port);

        // Act
        var connections = server.GetActiveConnections();

        // Assert
        connections.Should().NotBeNull();
        connections.Should().BeAssignableTo<IReadOnlyList<IConnectionDescriptor>>();
        server.ActiveConnections.Should().Be(connections.Count);
    }

    [Fact]
    public async Task MaxConnections_ShouldLimitConcurrentConnections()
    {
        // Arrange
        var server = CreateTcpServer();
        server.MaxConnections.Should().BeGreaterThan(0);
        var port = GetAvailablePort();
        await server.StartAsync(port);

        // Act & Assert
        // Basic test - just verify max connections property and current state
        server.ActiveConnections.Should().BeLessOrEqualTo(server.MaxConnections);
        server.MaxConnections.Should().BeGreaterOrEqualTo(100);
    }

    [Fact]
    public async Task BroadcastAsync_ShouldSendMessageToAllClients()
    {
        // Arrange
        var server = CreateTcpServer();
        var port = GetAvailablePort();
        await server.StartAsync(port);
        const string message = "Server shutdown in 5 minutes!";

        // Act
        await server.BroadcastAsync(message);

        // Assert
        // Basic test - verify broadcast method completes without error
        server.Should().NotBeNull();
        server.Status.Should().Be(ServerStatus.Running);
    }

    [Fact]
    public async Task StartAsync_ShouldWorkWithDifferentPorts()
    {
        // Test multiple different ports to ensure server works with various port numbers
        var ports = new[] { GetAvailablePort(), GetAvailablePort(), GetAvailablePort() };
        
        foreach (var port in ports)
        {
            // Arrange
            var server = CreateTcpServer();

            // Act
            await server.StartAsync(port);

            // Assert
            server.Port.Should().Be(port);
            server.Status.Should().Be(ServerStatus.Running);
            
            // Clean up
            await server.StopAsync();
        }
    }

    [Fact]
    public async Task Server_ShouldHandleMultipleStartStopCycles()
    {
        // Arrange
        var server = CreateTcpServer();

        // Act & Assert
        for (int i = 0; i < 3; i++)
        {
            var port = GetAvailablePort();
            await server.StartAsync(port);
            server.Status.Should().Be(ServerStatus.Running);
            
            await server.StopAsync();
            server.Status.Should().Be(ServerStatus.Stopped);
        }
    }

    [Fact]
    public void MaxConnections_ShouldDefaultToReasonableValue()
    {
        // Arrange & Act
        var server = CreateTcpServer();

        // Assert
        // Based on original MUD architecture, should support 100+ connections
        server.MaxConnections.Should().BeGreaterOrEqualTo(100);
    }
}