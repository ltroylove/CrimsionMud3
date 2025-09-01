using C3Mud.Core.Commands;
using C3Mud.Core.Game;
using C3Mud.Core.Networking;
using C3Mud.Core.Players;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace C3Mud.Tests.Game;

/// <summary>
/// Tests for the GameLoop service
/// Validates game loop functionality and integration
/// </summary>
public class GameLoopTests
{
    private readonly Mock<ITcpServer> _mockTcpServer;
    private readonly Mock<IConnectionManager> _mockConnectionManager;
    private readonly Mock<ICommandRegistry> _mockCommandRegistry;
    private readonly Mock<ILogger<GameLoop>> _mockLogger;
    private readonly Mock<IConnectionDescriptor> _mockConnection;
    private readonly GameLoop _gameLoop;

    public GameLoopTests()
    {
        _mockTcpServer = new Mock<ITcpServer>();
        _mockConnectionManager = new Mock<IConnectionManager>();
        _mockCommandRegistry = new Mock<ICommandRegistry>();
        _mockLogger = new Mock<ILogger<GameLoop>>();
        _mockConnection = new Mock<IConnectionDescriptor>();

        _gameLoop = new GameLoop(
            _mockTcpServer.Object,
            _mockConnectionManager.Object,
            _mockCommandRegistry.Object,
            _mockLogger.Object);

        SetupMockConnection();
    }

    private void SetupMockConnection()
    {
        _mockConnection.Setup(c => c.Id).Returns("test-connection-id");
        _mockConnection.Setup(c => c.IsConnected).Returns(true);
        _mockConnection.Setup(c => c.HasPendingInput).Returns(false);
        _mockConnection.Setup(c => c.Host).Returns("127.0.0.1");
        _mockConnection.Setup(c => c.ConnectedAt).Returns(DateTime.UtcNow);
        _mockConnection.Setup(c => c.ConnectionState).Returns(ConnectionState.Playing);
    }

    [Fact]
    public void GetOnlinePlayers_InitiallyEmpty_ShouldReturnEmptyCollection()
    {
        // Act
        var players = _gameLoop.GetOnlinePlayers();

        // Assert
        players.Should().BeEmpty();
    }

    [Fact]
    public void GetPlayerCount_InitiallyZero_ShouldReturnZero()
    {
        // Act
        var count = _gameLoop.GetPlayerCount();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void Dispose_ShouldLogInformation()
    {
        // Act
        _gameLoop.Dispose();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Game loop disposed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldLogStartingMessage()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100)); // Cancel quickly

        // Act
        try
        {
            await _gameLoop.StartAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation token is triggered
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Game loop starting")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldLogShuttingDownMessage()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        
        // Start and immediately stop
        var startTask = _gameLoop.StartAsync(cts.Token);
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        // Act
        try
        {
            await startTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        await _gameLoop.StopAsync(CancellationToken.None);

        // Assert - The stop message should be logged when cancellation occurs
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("shutting down")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    // Note: Testing the actual game loop execution is challenging with background services
    // More integration testing would be needed for full coverage of the processing loops

    [Fact]
    public void GameLoop_Constructor_ShouldRequireAllDependencies()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new GameLoop(null!, _mockConnectionManager.Object, _mockCommandRegistry.Object, _mockLogger.Object));
            
        Assert.Throws<ArgumentNullException>(() => 
            new GameLoop(_mockTcpServer.Object, null!, _mockCommandRegistry.Object, _mockLogger.Object));
            
        Assert.Throws<ArgumentNullException>(() => 
            new GameLoop(_mockTcpServer.Object, _mockConnectionManager.Object, null!, _mockLogger.Object));
            
        Assert.Throws<ArgumentNullException>(() => 
            new GameLoop(_mockTcpServer.Object, _mockConnectionManager.Object, _mockCommandRegistry.Object, null!));
    }

    [Fact]
    public void GameLoop_WithValidDependencies_ShouldConstructSuccessfully()
    {
        // Act & Assert - Constructor should not throw
        var gameLoop = new GameLoop(
            _mockTcpServer.Object,
            _mockConnectionManager.Object,
            _mockCommandRegistry.Object,
            _mockLogger.Object);

        gameLoop.Should().NotBeNull();
        gameLoop.Dispose();
    }
}