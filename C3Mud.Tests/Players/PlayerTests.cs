using C3Mud.Core.Networking;
using C3Mud.Core.Players;
using FluentAssertions;
using Moq;
using Xunit;

namespace C3Mud.Tests.Players;

/// <summary>
/// Tests for the Player class
/// Validates player functionality and state management
/// </summary>
public class PlayerTests
{
    private readonly Mock<IConnectionDescriptor> _mockConnection;
    private readonly Mock<ITelnetProtocolHandler> _mockTelnetHandler;

    public PlayerTests()
    {
        _mockTelnetHandler = new Mock<ITelnetProtocolHandler>();
        _mockConnection = new Mock<IConnectionDescriptor>();
        _mockConnection.Setup(c => c.IsConnected).Returns(true);
        _mockConnection.Setup(c => c.TelnetHandler).Returns(_mockTelnetHandler.Object);
    }

    [Fact]
    public void Player_WithId_ShouldInitializeCorrectly()
    {
        // Arrange
        var playerId = "test-player-123";

        // Act
        var player = new Player(playerId);

        // Assert
        player.Id.Should().Be(playerId);
        player.Name.Should().Be(string.Empty);
        player.Level.Should().Be(1);
        player.Position.Should().Be(PlayerPosition.Standing);
        player.IsConnected.Should().BeFalse();
        player.Connection.Should().BeNull();
    }

    [Fact]
    public void Player_WithIdAndConnection_ShouldInitializeCorrectly()
    {
        // Arrange
        var playerId = "test-player-456";

        // Act
        var player = new Player(playerId, _mockConnection.Object);

        // Assert
        player.Id.Should().Be(playerId);
        player.Connection.Should().Be(_mockConnection.Object);
        player.IsConnected.Should().BeTrue();
    }

    [Fact]
    public void IsConnected_WithNullConnection_ShouldReturnFalse()
    {
        // Arrange
        var player = new Player("test-id");

        // Act & Assert
        player.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void IsConnected_WithDisconnectedConnection_ShouldReturnFalse()
    {
        // Arrange
        _mockConnection.Setup(c => c.IsConnected).Returns(false);
        var player = new Player("test-id", _mockConnection.Object);

        // Act & Assert
        player.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void IsConnected_WithConnectedConnection_ShouldReturnTrue()
    {
        // Arrange
        _mockConnection.Setup(c => c.IsConnected).Returns(true);
        var player = new Player("test-id", _mockConnection.Object);

        // Act & Assert
        player.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task SendMessageAsync_WithConnectedPlayer_ShouldSendToConnection()
    {
        // Arrange
        var player = new Player("test-id", _mockConnection.Object);
        var message = "Hello, world!";

        // Act
        await player.SendMessageAsync(message);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync("Hello, world!\r\n"), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WithDisconnectedPlayer_ShouldNotSend()
    {
        // Arrange
        _mockConnection.Setup(c => c.IsConnected).Returns(false);
        var player = new Player("test-id", _mockConnection.Object);

        // Act
        await player.SendMessageAsync("test message");

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_WithNullConnection_ShouldNotThrow()
    {
        // Arrange
        var player = new Player("test-id");

        // Act & Assert - Should not throw
        await player.SendMessageAsync("test message");
    }

    [Fact]
    public async Task SendFormattedMessageAsync_WithConnectedPlayer_ShouldProcessColors()
    {
        // Arrange
        var player = new Player("test-id", _mockConnection.Object);
        var message = "&RHello &Gworld!&N";
        var processedMessage = "\x1B[0;31mHello \x1B[0;32mworld!\x1B[0m";
        
        _mockTelnetHandler.Setup(h => h.ProcessColorCodes(message, _mockConnection.Object))
            .Returns(processedMessage);

        // Act
        await player.SendFormattedMessageAsync(message);

        // Assert
        _mockTelnetHandler.Verify(h => h.ProcessColorCodes(message, _mockConnection.Object), Times.Once);
        _mockConnection.Verify(c => c.SendDataAsync(processedMessage + "\r\n"), Times.Once);
    }

    [Fact]
    public async Task SendFormattedMessageAsync_WithDisconnectedPlayer_ShouldNotSend()
    {
        // Arrange
        _mockConnection.Setup(c => c.IsConnected).Returns(false);
        var player = new Player("test-id", _mockConnection.Object);

        // Act
        await player.SendFormattedMessageAsync("&Rtest&N");

        // Assert
        _mockTelnetHandler.Verify(h => h.ProcessColorCodes(It.IsAny<string>(), It.IsAny<IConnectionDescriptor>()), Times.Never);
        _mockConnection.Verify(c => c.SendDataAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DisconnectAsync_WithConnectedPlayer_ShouldSendReasonAndClose()
    {
        // Arrange
        var player = new Player("test-id", _mockConnection.Object);
        var reason = "Goodbye!";

        // Act
        await player.DisconnectAsync(reason);

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync("Goodbye!\r\n"), Times.Once);
        _mockConnection.Verify(c => c.CloseAsync(), Times.Once);
    }

    [Fact]
    public async Task DisconnectAsync_WithDefaultReason_ShouldUseDefault()
    {
        // Arrange
        var player = new Player("test-id", _mockConnection.Object);

        // Act
        await player.DisconnectAsync();

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync("Goodbye!\r\n"), Times.Once);
        _mockConnection.Verify(c => c.CloseAsync(), Times.Once);
    }

    [Fact]
    public async Task DisconnectAsync_WithDisconnectedPlayer_ShouldNotSend()
    {
        // Arrange
        _mockConnection.Setup(c => c.IsConnected).Returns(false);
        var player = new Player("test-id", _mockConnection.Object);

        // Act
        await player.DisconnectAsync();

        // Assert
        _mockConnection.Verify(c => c.SendDataAsync(It.IsAny<string>()), Times.Never);
        _mockConnection.Verify(c => c.CloseAsync(), Times.Never);
    }

    [Fact]
    public void PlayerProperties_ShouldBeSettable()
    {
        // Arrange
        var player = new Player("test-id");

        // Act
        player.Name = "TestPlayer";
        player.Level = 10;
        player.Position = PlayerPosition.Resting;
        player.Connection = _mockConnection.Object;

        // Assert
        player.Name.Should().Be("TestPlayer");
        player.Level.Should().Be(10);
        player.Position.Should().Be(PlayerPosition.Resting);
        player.Connection.Should().Be(_mockConnection.Object);
    }

    [Fact]
    public void Equals_WithSameId_ShouldReturnTrue()
    {
        // Arrange
        var player1 = new Player("same-id");
        var player2 = new Player("same-id");

        // Act & Assert
        player1.Equals(player2).Should().BeTrue();
        (player1 == player2).Should().BeFalse(); // Reference equality
    }

    [Fact]
    public void Equals_WithDifferentId_ShouldReturnFalse()
    {
        // Arrange
        var player1 = new Player("id-1");
        var player2 = new Player("id-2");

        // Act & Assert
        player1.Equals(player2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithSameId_ShouldBeEqual()
    {
        // Arrange
        var player1 = new Player("same-id");
        var player2 = new Player("same-id");

        // Act & Assert
        player1.GetHashCode().Should().Be(player2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var player = new Player("test-id")
        {
            Name = "TestPlayer",
            Level = 5
        };

        // Act
        var result = player.ToString();

        // Assert
        result.Should().Be("Player[test-id]: TestPlayer (Level 5)");
    }

    [Theory]
    [InlineData(PlayerPosition.Dead)]
    [InlineData(PlayerPosition.MortallyWounded)]
    [InlineData(PlayerPosition.Incapacitated)]
    [InlineData(PlayerPosition.Stunned)]
    [InlineData(PlayerPosition.Sleeping)]
    [InlineData(PlayerPosition.Resting)]
    [InlineData(PlayerPosition.Sitting)]
    [InlineData(PlayerPosition.Fighting)]
    [InlineData(PlayerPosition.Standing)]
    public void Position_AllValues_ShouldBeSupported(PlayerPosition position)
    {
        // Arrange
        var player = new Player("test-id");

        // Act
        player.Position = position;

        // Assert
        player.Position.Should().Be(position);
    }
}