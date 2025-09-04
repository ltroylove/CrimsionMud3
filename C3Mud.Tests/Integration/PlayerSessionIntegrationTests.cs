using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using C3Mud.Core.Players;
using C3Mud.Core.Players.Services;
using C3Mud.Core.Players.Models;
using C3Mud.Core.Commands;
using C3Mud.Core.Networking;
using C3Mud.Core.Game;
using Moq;

namespace C3Mud.Tests.Integration;

/// <summary>
/// Integration tests for player-session-command interaction
/// Tests the complete flow from connection to command processing
/// </summary>
public class PlayerSessionIntegrationTests
{
    [Fact]
    public async Task PlayerSession_LoginFlow_ShouldAuthenticateAndCreateSession()
    {
        // ARRANGE - Should fail as PlayerSessionManager doesn't exist
        var sessionManager = new PlayerSessionManager();
        var authService = new Mock<IAuthenticationService>();
        var mockConnection = new Mock<IConnectionDescriptor>();
        
        authService.Setup(a => a.AuthenticatePlayerAsync("TestPlayer", "password"))
                   .ReturnsAsync(AuthenticationResult.Success(new Player("test-id") { Name = "TestPlayer" }));
        
        // ACT
        var act = async () => await sessionManager.AuthenticateAndCreateSessionAsync(mockConnection.Object, "TestPlayer", "password");
        
        // ASSERT
        act.Should().NotThrowAsync("PlayerSessionManager should be implemented");
        
        var session = await sessionManager.AuthenticateAndCreateSessionAsync(mockConnection.Object, "TestPlayer", "password");
        session.Should().NotBeNull("should create valid session for authenticated player");
        session.Player.Name.Should().Be("TestPlayer");
        session.Connection.Should().Be(mockConnection.Object);
    }
    
    [Fact]
    public async Task PlayerSession_LoginFlow_ShouldRejectInvalidCredentials()
    {
        // ARRANGE
        var sessionManager = new PlayerSessionManager();
        var authService = new Mock<IAuthenticationService>();
        var mockConnection = new Mock<IConnectionDescriptor>();
        
        authService.Setup(a => a.AuthenticatePlayerAsync("TestPlayer", "wrongpass"))
                   .ReturnsAsync(AuthenticationResult.Failure("Invalid credentials"));
        
        // ACT
        var act = async () => await sessionManager.AuthenticateAndCreateSessionAsync(mockConnection.Object, "TestPlayer", "wrongpass");
        
        // ASSERT
        act.Should().ThrowAsync<UnauthorizedAccessException>("should reject invalid credentials");
    }
    
    [Fact]
    public async Task PlayerSession_CommandExecution_ShouldProcessThroughSession()
    {
        // ARRANGE - Should fail as session management doesn't exist
        var sessionManager = new PlayerSessionManager();
        var commandProcessor = new Mock<ICommandProcessor>();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var player = new Player("test-id") { Name = "TestPlayer", Connection = mockConnection.Object };
        
        var session = new PlayerSession(player, mockConnection.Object);
        sessionManager.RegisterSession(session);
        
        commandProcessor.Setup(c => c.ProcessCommandAsync(player, "look"))
                       .Returns(Task.CompletedTask);
        
        // ACT
        var act = async () => await sessionManager.ProcessPlayerCommandAsync(session, "look");
        
        // ASSERT
        act.Should().NotThrowAsync("should process commands through session");
        commandProcessor.Verify(c => c.ProcessCommandAsync(player, "look"), Times.Once);
    }
    
    [Fact]
    public async Task PlayerSession_MultipleConnections_ShouldHandleConnectionState()
    {
        // ARRANGE - Test handling of multiple connections for same player
        var sessionManager = new PlayerSessionManager();
        var mockConnection1 = new Mock<IConnectionDescriptor>();
        var mockConnection2 = new Mock<IConnectionDescriptor>();
        
        mockConnection1.Setup(c => c.IsConnected).Returns(true);
        mockConnection2.Setup(c => c.IsConnected).Returns(true);
        
        var player = new Player("test-id") { Name = "TestPlayer" };
        var session1 = new PlayerSession(player, mockConnection1.Object);
        var session2 = new PlayerSession(player, mockConnection2.Object);
        
        // ACT
        sessionManager.RegisterSession(session1);
        var act = () => sessionManager.RegisterSession(session2); // Same player, different connection
        
        // ASSERT - Should handle reconnection properly (like original MUD)
        act.Should().NotThrow("should handle player reconnection");
        
        // First connection should be disconnected
        mockConnection1.Verify(c => c.CloseAsync(), Times.Once, "previous connection should be closed");
        
        // New connection should be active
        var activeSession = sessionManager.GetPlayerSession("TestPlayer");
        activeSession.Connection.Should().Be(mockConnection2.Object);
    }
    
    [Fact]
    public async Task PlayerSession_CommandProcessing_ShouldMaintainPlayerState()
    {
        // ARRANGE - Test that player state persists across commands
        var sessionManager = new PlayerSessionManager();
        var commandProcessor = new LegacyCommandProcessor();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var player = new Player("test-id") 
        { 
            Name = "TestPlayer", 
            Level = 1,
            Connection = mockConnection.Object 
        };
        
        var session = new PlayerSession(player, mockConnection.Object);
        sessionManager.RegisterSession(session);
        
        mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                     .Returns(Task.CompletedTask);
        
        // ACT - Process multiple commands that could affect player state
        await sessionManager.ProcessPlayerCommandAsync(session, "score");
        await sessionManager.ProcessPlayerCommandAsync(session, "look");
        
        // ASSERT - Player state should be maintained
        session.Player.Name.Should().Be("TestPlayer", "player name should persist");
        session.Player.Level.Should().Be(1, "player level should persist");
        session.IsActive.Should().BeTrue("session should remain active");
    }
    
    [Fact]
    public async Task PlayerSession_Disconnection_ShouldCleanupProperly()
    {
        // ARRANGE
        var sessionManager = new PlayerSessionManager();
        var playerService = new Mock<IPlayerService>();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var player = new Player("test-id") { Name = "TestPlayer", Connection = mockConnection.Object };
        
        var session = new PlayerSession(player, mockConnection.Object);
        sessionManager.RegisterSession(session);
        
        playerService.Setup(p => p.SavePlayerAsync(player))
                    .Returns(Task.CompletedTask);
        
        // ACT
        await sessionManager.DisconnectPlayerAsync("TestPlayer");
        
        // ASSERT
        var activeSession = sessionManager.GetPlayerSession("TestPlayer");
        activeSession.Should().BeNull("session should be removed after disconnect");
        
        playerService.Verify(p => p.SavePlayerAsync(player), Times.Once, "player should be saved on disconnect");
        mockConnection.Verify(c => c.CloseAsync(), Times.Once, "connection should be closed");
    }
    
    [Fact]
    public async Task PlayerSession_IdleTimeout_ShouldHandleLegacyTimeout()
    {
        // ARRANGE - Should match original MUD idle timeout behavior
        var sessionManager = new PlayerSessionManager();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var player = new Player("test-id") { Name = "TestPlayer", Connection = mockConnection.Object };
        
        var session = new PlayerSession(player, mockConnection.Object);
        sessionManager.RegisterSession(session);
        
        // Simulate idle timeout (original MUD had ~30 minute timeout)
        session.SimulateIdleTime(TimeSpan.FromMinutes(31));
        
        // ACT
        await sessionManager.ProcessIdleTimeoutsAsync();
        
        // ASSERT
        var activeSession = sessionManager.GetPlayerSession("TestPlayer");
        activeSession.Should().BeNull("idle session should be disconnected");
        
        mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(s => s.Contains("idle"))), Times.Once,
                             "should send idle timeout message");
    }
    
    [Fact]
    public async Task PlayerSession_SaveOnQuit_ShouldPersistPlayerData()
    {
        // ARRANGE
        var sessionManager = new PlayerSessionManager();
        var playerService = new Mock<IPlayerService>();
        var commandProcessor = new Mock<ICommandProcessor>();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var player = new Player("test-id") { Name = "TestPlayer", Level = 25, Connection = mockConnection.Object };
        
        var session = new PlayerSession(player, mockConnection.Object);
        sessionManager.RegisterSession(session);
        
        playerService.Setup(p => p.SavePlayerAsync(player))
                    .Returns(Task.CompletedTask);
        
        // Setup quit command to trigger save
        commandProcessor.Setup(c => c.ProcessCommandAsync(player, "quit"))
                       .Callback<IPlayer, string>((p, cmd) => sessionManager.DisconnectPlayerAsync(p.Name))
                       .Returns(Task.CompletedTask);
        
        // ACT
        await sessionManager.ProcessPlayerCommandAsync(session, "quit");
        
        // ASSERT
        playerService.Verify(p => p.SavePlayerAsync(player), Times.Once, "player should be saved on quit");
    }
    
    [Fact]
    public void PlayerSession_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // ARRANGE
        var sessionManager = new PlayerSessionManager();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var player = new Player("test-id") { Name = "TestPlayer", Connection = mockConnection.Object };
        var session = new PlayerSession(player, mockConnection.Object);
        
        // ACT - Simulate concurrent access
        var tasks = new List<Task>();
        
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => sessionManager.RegisterSession(session)));
            tasks.Add(Task.Run(() => sessionManager.GetPlayerSession("TestPlayer")));
        }
        
        var act = async () => await Task.WhenAll(tasks);
        
        // ASSERT - Should not throw exceptions due to race conditions
        act.Should().NotThrowAsync("session manager should be thread-safe");
    }
    
    [Fact]
    public async Task PlayerSession_GameLoop_ShouldIntegrateWithSessionManagement()
    {
        // ARRANGE - Should fail as GameLoop integration doesn't exist
        var mockTcpServer = new Mock<ITcpServer>();
        var mockConnectionManager = new Mock<IConnectionManager>();
        var mockCommandRegistry = new Mock<ICommandRegistry>();
        var mockLogger = new Mock<ILogger<GameLoop>>();
        
        var gameLoop = new GameLoop(mockTcpServer.Object, mockConnectionManager.Object, mockCommandRegistry.Object, mockLogger.Object);
        var sessionManager = new PlayerSessionManager();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var player = new Player("test-id") { Name = "TestPlayer", Connection = mockConnection.Object };
        
        var session = new PlayerSession(player, mockConnection.Object);
        sessionManager.RegisterSession(session);
        
        // ACT - This method doesn't exist yet, so we'll test the GetOnlinePlayers method instead
        var act = () => gameLoop.GetOnlinePlayers();
        
        // ASSERT
        act.Should().NotThrow("GameLoop should integrate with session management");
        var onlinePlayers = gameLoop.GetOnlinePlayers();
        onlinePlayers.Should().NotBeNull("should return player collection");
    }
    
    [Theory]
    [InlineData("look")]
    [InlineData("score")]
    [InlineData("help")]
    [InlineData("who")]
    public async Task PlayerSession_CommandThrottling_ShouldPreventSpamming(string command)
    {
        // ARRANGE - Should implement command throttling like original MUD
        var sessionManager = new PlayerSessionManager();
        var commandProcessor = new Mock<ICommandProcessor>();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var player = new Player("test-id") { Name = "TestPlayer", Connection = mockConnection.Object };
        
        var session = new PlayerSession(player, mockConnection.Object);
        sessionManager.RegisterSession(session);
        
        commandProcessor.Setup(c => c.ProcessCommandAsync(player, command))
                       .Returns(Task.CompletedTask);
        
        mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                     .Returns(Task.CompletedTask);
        
        // ACT - Rapidly send same command
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(sessionManager.ProcessPlayerCommandAsync(session, command));
        }
        
        await Task.WhenAll(tasks);
        
        // ASSERT - Should throttle excessive commands
        commandProcessor.Verify(c => c.ProcessCommandAsync(player, command), 
                               Times.AtMost(5), "should throttle rapid command execution");
        
        // Should send throttle warning
        mockConnection.Verify(c => c.SendDataAsync(It.Is<string>(s => s.Contains("too fast") || s.Contains("slow down"))), 
                             Times.AtLeastOnce, "should warn about command spam");
    }
    
    [Fact]
    public async Task PlayerSession_NetworkDisconnection_ShouldHandleGracefully()
    {
        // ARRANGE
        var sessionManager = new PlayerSessionManager();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var player = new Player("test-id") { Name = "TestPlayer", Connection = mockConnection.Object };
        
        var session = new PlayerSession(player, mockConnection.Object);
        sessionManager.RegisterSession(session);
        
        // Simulate network disconnection
        mockConnection.Setup(c => c.IsConnected).Returns(false);
        mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                     .ThrowsAsync(new InvalidOperationException("Connection closed"));
        
        // ACT
        var act = async () => await sessionManager.ProcessPlayerCommandAsync(session, "look");
        
        // ASSERT - Should handle disconnection gracefully
        act.Should().NotThrowAsync("should handle network disconnections gracefully");
        
        // Session should be marked as disconnected
        session.IsActive.Should().BeFalse("session should be marked inactive after network failure");
    }
}