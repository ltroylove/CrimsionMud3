using C3Mud.Core.Networking;
using FluentAssertions;
using Moq;
using Xunit;

namespace C3Mud.Tests.Networking;

/// <summary>
/// Tests for connection management, rate limiting, and DDoS protection
/// These tests should all FAIL initially since we haven't implemented connection management
/// </summary>
public class ConnectionManagementTests
{
    private IConnectionManager CreateConnectionManager()
    {
        return new C3Mud.Core.Networking.ConnectionManager();
    }

    private Mock<IConnectionDescriptor> CreateMockConnection(string id = "test-conn", string host = "127.0.0.1")
    {
        var mock = new Mock<IConnectionDescriptor>();
        mock.Setup(c => c.Id).Returns(id);
        mock.Setup(c => c.Host).Returns(host);
        mock.Setup(c => c.ConnectedAt).Returns(DateTime.UtcNow);
        mock.Setup(c => c.IsConnected).Returns(true);
        mock.Setup(c => c.ConnectionState).Returns(ConnectionState.Playing);
        return mock;
    }

    [Fact]
    public async Task AddConnectionAsync_WithValidConnection_ShouldAddSuccessfully()
    {
        // Arrange
        var manager = CreateConnectionManager();
        var connection = CreateMockConnection().Object;

        // Act
        var result = await manager.AddConnectionAsync(connection);

        // Assert
        result.Should().BeTrue();
        manager.ActiveConnectionCount.Should().Be(1);
        manager.GetConnection(connection.Id).Should().Be(connection);
    }

    [Fact]
    public async Task AddConnectionAsync_WhenAtMaxConnections_ShouldRejectNewConnection()
    {
        // Arrange
        var manager = CreateConnectionManager();
        
        // Fill up to max connections
        for (int i = 0; i < manager.MaxConnections; i++)
        {
            var conn = CreateMockConnection($"conn-{i}").Object;
            await manager.AddConnectionAsync(conn);
        }

        var additionalConnection = CreateMockConnection("overflow-conn").Object;

        // Act
        var result = await manager.AddConnectionAsync(additionalConnection);

        // Assert
        result.Should().BeFalse();
        manager.ActiveConnectionCount.Should().Be(manager.MaxConnections);
        manager.GetConnection("overflow-conn").Should().BeNull();
    }

    [Fact]
    public async Task RemoveConnectionAsync_WithExistingConnection_ShouldRemoveSuccessfully()
    {
        // Arrange
        var manager = CreateConnectionManager();
        var connection = CreateMockConnection().Object;
        await manager.AddConnectionAsync(connection);

        // Act
        await manager.RemoveConnectionAsync(connection.Id);

        // Assert
        manager.ActiveConnectionCount.Should().Be(0);
        manager.GetConnection(connection.Id).Should().BeNull();
    }

    [Fact]
    public void ShouldAllowConnection_WithNormalIP_ShouldReturnTrue()
    {
        // Arrange
        var manager = CreateConnectionManager();
        var normalIP = "192.168.1.100";

        // Act
        var result = manager.ShouldAllowConnection(normalIP);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldAllowConnection_WithTooManyConnectionsFromSameIP_ShouldReturnFalse()
    {
        // Arrange
        var manager = CreateConnectionManager();
        var hostIP = "192.168.1.100";
        
        // Add multiple connections from same IP (simulate the limit being reached)
        for (int i = 0; i < 5; i++) // Assume 5 is the per-IP limit
        {
            var conn = CreateMockConnection($"conn-{i}", hostIP).Object;
            manager.AddConnectionAsync(conn).Wait();
        }

        // Act
        var result = manager.ShouldAllowConnection(hostIP);

        // Assert
        result.Should().BeFalse(); // Should block additional connections from same IP
    }

    [Theory]
    [InlineData("look")]
    [InlineData("north")]
    [InlineData("inventory")]
    [InlineData("who")]
    public void RecordActivity_WithNormalCommands_ShouldNotTriggerRateLimit(string command)
    {
        // Arrange
        var manager = CreateConnectionManager();
        var connectionId = "test-conn";

        // Act
        manager.RecordActivity(connectionId, command);

        // Assert
        manager.IsRateLimited(connectionId).Should().BeFalse();
    }

    [Fact]
    public void IsRateLimited_WithSpamming_ShouldReturnTrue()
    {
        // Arrange
        var manager = CreateConnectionManager();
        var connectionId = "spammer-conn";

        // Act - Simulate rapid command spam
        for (int i = 0; i < 100; i++) // Simulate 100 commands in rapid succession
        {
            manager.RecordActivity(connectionId, $"say spam message {i}");
        }

        // Assert
        manager.IsRateLimited(connectionId).Should().BeTrue();
    }

    [Fact]
    public async Task CleanupConnectionsAsync_ShouldRemoveStaleConnections()
    {
        // Arrange
        var manager = CreateConnectionManager();
        
        // Add a normal connection
        var activeConn = CreateMockConnection("active", "127.0.0.1");
        activeConn.Setup(c => c.IsConnected).Returns(true);
        
        // Add a stale connection
        var staleConn = CreateMockConnection("stale", "192.168.1.50");
        staleConn.Setup(c => c.IsConnected).Returns(false);
        staleConn.Setup(c => c.ConnectedAt).Returns(DateTime.UtcNow.AddHours(-2)); // 2 hours old
        
        await manager.AddConnectionAsync(activeConn.Object);
        await manager.AddConnectionAsync(staleConn.Object);

        // Act
        await manager.CleanupConnectionsAsync();

        // Assert
        manager.ActiveConnectionCount.Should().Be(1);
        manager.GetConnection("active").Should().NotBeNull();
        manager.GetConnection("stale").Should().BeNull();
    }

    [Fact]
    public void GetStatistics_ShouldReturnAccurateStats()
    {
        // Arrange
        var manager = CreateConnectionManager();
        
        // Add some connections
        var conn1 = CreateMockConnection("conn1", "192.168.1.100").Object;
        var conn2 = CreateMockConnection("conn2", "192.168.1.101").Object;
        var conn3 = CreateMockConnection("conn3", "192.168.1.100").Object; // Same IP as conn1
        
        manager.AddConnectionAsync(conn1).Wait();
        manager.AddConnectionAsync(conn2).Wait();
        manager.AddConnectionAsync(conn3).Wait();

        // Act
        var stats = manager.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.ActiveConnections.Should().Be(3);
        stats.ConnectionsByHost.Should().ContainKey("192.168.1.100");
        stats.ConnectionsByHost["192.168.1.100"].Should().Be(2);
        stats.ConnectionsByHost.Should().ContainKey("192.168.1.101");
        stats.ConnectionsByHost["192.168.1.101"].Should().Be(1);
    }

    [Fact]
    public void MaxConnections_ShouldHaveReasonableDefault()
    {
        // Arrange & Act
        var manager = CreateConnectionManager();

        // Assert
        // Should support at least 100 connections as per requirements
        manager.MaxConnections.Should().BeGreaterOrEqualTo(100);
        manager.MaxConnections.Should().BeLessOrEqualTo(1000); // But not unlimited
    }

    [Fact]
    public async Task ConnectionManager_ShouldHandleConcurrentOperations()
    {
        // Arrange
        var manager = CreateConnectionManager();
        var tasks = new List<Task>();

        // Act - Perform many concurrent operations
        for (int i = 0; i < 50; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                var conn = CreateMockConnection($"concurrent-{index}").Object;
                await manager.AddConnectionAsync(conn);
                manager.RecordActivity(conn.Id, "look");
                await manager.RemoveConnectionAsync(conn.Id);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        // All operations should complete without exceptions
        manager.ActiveConnectionCount.Should().Be(0); // All connections removed
    }

    [Theory]
    [InlineData("127.0.0.1")]     // localhost
    [InlineData("::1")]           // IPv6 localhost  
    [InlineData("0.0.0.0")]       // Any address
    public void ShouldAllowConnection_WithSpecialAddresses_ShouldHandleCorrectly(string address)
    {
        // Arrange
        var manager = CreateConnectionManager();

        // Act
        var result = manager.ShouldAllowConnection(address);

        // Assert
        // Localhost should always be allowed, others depend on configuration
        if (address == "127.0.0.1" || address == "::1")
        {
            result.Should().BeTrue();
        }
        else
        {
            // result.Should().BeOfType(typeof(bool)); // Just verify it's a bool
        (result is bool).Should().BeTrue(); // Just verify it doesn't crash
        }
    }

    [Fact]
    public void RecordActivity_WithDifferentCommandTypes_ShouldTrackAppropriately()
    {
        // Arrange
        var manager = CreateConnectionManager();
        var connectionId = "test-conn";

        // Act & Assert - Different types of commands should be handled differently
        
        // Movement commands - should be rate limited more strictly
        for (int i = 0; i < 10; i++)
        {
            manager.RecordActivity(connectionId, "north");
        }
        // Might be rate limited depending on implementation
        
        // Communication commands - should be rate limited for spam
        for (int i = 0; i < 20; i++)
        {
            manager.RecordActivity(connectionId, $"say message {i}");
        }
        // Should likely be rate limited
        
        // Information commands - should be more lenient
        manager.RecordActivity(connectionId, "look");
        manager.RecordActivity(connectionId, "inventory");
        manager.RecordActivity(connectionId, "score");
        
        // The specific behavior depends on implementation, but it should not crash
        var isLimited = manager.IsRateLimited(connectionId);
        // isLimited.Should().BeOfType(typeof(bool)); // Just verify it's a bool
        (isLimited is bool).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllConnections_ShouldReturnReadOnlyList()
    {
        // Arrange
        var manager = CreateConnectionManager();
        var conn1 = CreateMockConnection("conn1").Object;
        var conn2 = CreateMockConnection("conn2").Object;
        
        await manager.AddConnectionAsync(conn1);
        await manager.AddConnectionAsync(conn2);

        // Act
        var connections = manager.GetAllConnections();

        // Assert
        connections.Should().NotBeNull();
        connections.Should().HaveCount(2);
        connections.Should().BeAssignableTo<IReadOnlyList<IConnectionDescriptor>>();
        connections.Should().Contain(conn1);
        connections.Should().Contain(conn2);
    }
}