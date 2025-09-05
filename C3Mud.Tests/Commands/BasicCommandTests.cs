using C3Mud.Core.Commands;
using C3Mud.Core.Commands.Basic;
using C3Mud.Core.Networking;
using C3Mud.Core.Players;
using FluentAssertions;
using Moq;
using Xunit;

namespace C3Mud.Tests.Commands;

/// <summary>
/// Tests for basic MUD commands
/// Validates core command functionality and behavior
/// </summary>
public class BasicCommandTests
{
    private readonly Mock<IPlayer> _mockPlayer;
    private readonly Mock<IConnectionDescriptor> _mockConnection;

    public BasicCommandTests()
    {
        _mockConnection = new Mock<IConnectionDescriptor>();
        _mockConnection.Setup(c => c.IsConnected).Returns(true);

        _mockPlayer = new Mock<IPlayer>();
        _mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        _mockPlayer.Setup(p => p.Level).Returns(5);
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Standing);
        _mockPlayer.Setup(p => p.IsConnected).Returns(true);
        _mockPlayer.Setup(p => p.Connection).Returns(_mockConnection.Object);
    }

    #region Look Command Tests

    [Fact]
    public async Task LookCommand_WithoutArguments_ShouldShowRoom()
    {
        // Arrange
        var command = new LookCommand();

        // Act
        await command.ExecuteAsync(_mockPlayer.Object, "", 15);

        // Assert
        _mockPlayer.Verify(p => p.SendFormattedMessageAsync(
            It.Is<string>(s => s.Contains("A Simple Room") && s.Contains("testing room"))), 
            Times.Once);
        
        _mockPlayer.Verify(p => p.SendFormattedMessageAsync(
            It.Is<string>(s => s.Contains("TestPlayer is standing here"))), 
            Times.Once);
    }

    [Fact]
    public async Task LookCommand_WithArguments_ShouldShowVoidWithoutWorldDatabase()
    {
        // Arrange - Using old constructor without WorldDatabase
        var command = new LookCommand();

        // Act
        await command.ExecuteAsync(_mockPlayer.Object, "sword", 15);

        // Assert - Without WorldDatabase, player is in the void
        _mockPlayer.Verify(p => p.SendMessageAsync("You are floating in the void!"), Times.Once);
    }

    [Fact]
    public void LookCommand_Properties_ShouldBeCorrect()
    {
        // Arrange
        var command = new LookCommand();

        // Assert
        command.Name.Should().Be("look");
        command.Aliases.Should().ContainSingle("l");
        command.MinimumPosition.Should().Be(PlayerPosition.Sleeping);
        command.MinimumLevel.Should().Be(1);
        command.IsEnabled.Should().BeTrue();
    }

    #endregion

    #region Quit Command Tests

    [Fact]
    public async Task QuitCommand_WithNormalPlayer_ShouldDisconnect()
    {
        // Arrange
        var command = new QuitCommand();

        // Act
        await command.ExecuteAsync(_mockPlayer.Object, "", 73);

        // Assert
        _mockPlayer.Verify(p => p.SendFormattedMessageAsync(
            It.Is<string>(s => s.Contains("Goodbye, friend.. Come back soon!"))), 
            Times.Once);
        _mockPlayer.Verify(p => p.DisconnectAsync("Player quit the game"), Times.Once);
    }

    [Fact]
    public async Task QuitCommand_WhileFighting_ShouldNotAllowQuit()
    {
        // Arrange
        var command = new QuitCommand();
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Fighting);

        // Act
        await command.ExecuteAsync(_mockPlayer.Object, "", 73);

        // Assert
        _mockPlayer.Verify(p => p.SendMessageAsync("No way! You are fighting for your life!"), Times.Once);
        _mockPlayer.Verify(p => p.DisconnectAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void QuitCommand_Properties_ShouldBeCorrect()
    {
        // Arrange
        var command = new QuitCommand();

        // Assert
        command.Name.Should().Be("quit");
        command.Aliases.Should().ContainSingle("q");
        command.MinimumPosition.Should().Be(PlayerPosition.Stunned);
        command.MinimumLevel.Should().Be(1);
        command.IsEnabled.Should().BeTrue();
    }

    #endregion

    #region Who Command Tests

    [Fact]
    public async Task WhoCommand_ShouldShowPlayerList()
    {
        // Arrange
        var command = new WhoCommand();

        // Act
        await command.ExecuteAsync(_mockPlayer.Object, "", 39);

        // Assert
        _mockPlayer.Verify(p => p.SendFormattedMessageAsync(
            It.Is<string>(s => s.Contains("Players currently online"))), 
            Times.Once);
        
        _mockPlayer.Verify(p => p.SendFormattedMessageAsync(
            It.Is<string>(s => s.Contains("TestPlayer"))), 
            Times.Once);
            
        _mockPlayer.Verify(p => p.SendFormattedMessageAsync(
            It.Is<string>(s => s.Contains("There is") && s.Contains("player online"))), 
            Times.Once);
    }

    [Fact]
    public void WhoCommand_Properties_ShouldBeCorrect()
    {
        // Arrange
        var command = new WhoCommand();

        // Assert
        command.Name.Should().Be("who");
        command.Aliases.Should().BeEmpty();
        command.MinimumPosition.Should().Be(PlayerPosition.Sleeping);
        command.MinimumLevel.Should().Be(1);
        command.IsEnabled.Should().BeTrue();
    }

    #endregion

    #region Score Command Tests

    [Fact]
    public async Task ScoreCommand_ShouldShowPlayerStats()
    {
        // Arrange
        var command = new ScoreCommand();

        // Act
        await command.ExecuteAsync(_mockPlayer.Object, "", 14);

        // Assert
        _mockPlayer.Verify(p => p.SendFormattedMessageAsync(
            It.Is<string>(s => s.Contains("TestPlayer's Character Statistics"))), 
            Times.Once);
    }

    [Fact]
    public void ScoreCommand_Properties_ShouldBeCorrect()
    {
        // Arrange
        var command = new ScoreCommand();

        // Assert
        command.Name.Should().Be("score");
        command.Aliases.Should().Contain("sc");
        command.Aliases.Should().Contain("stat");
        command.MinimumPosition.Should().Be(PlayerPosition.Sleeping);
        command.MinimumLevel.Should().Be(1);
        command.IsEnabled.Should().BeTrue();
    }

    #endregion

    #region Help Command Tests

    [Fact]
    public async Task HelpCommand_WithoutArguments_ShouldShowGeneralHelp()
    {
        // Arrange
        var command = new HelpCommand();

        // Act
        await command.ExecuteAsync(_mockPlayer.Object, "", 38);

        // Assert
        _mockPlayer.Verify(p => p.SendFormattedMessageAsync(
            It.Is<string>(s => s.Contains("C3MUD Help System") && 
                              s.Contains("Basic Commands") &&
                              s.Contains("look") &&
                              s.Contains("quit"))), 
            Times.Once);
    }

    [Fact]
    public async Task HelpCommand_WithLookTopic_ShouldShowLookHelp()
    {
        // Arrange
        var command = new HelpCommand();

        // Act
        await command.ExecuteAsync(_mockPlayer.Object, "look", 38);

        // Assert
        _mockPlayer.Verify(p => p.SendFormattedMessageAsync(
            It.Is<string>(s => s.Contains("LOOK - Examine your surroundings") && 
                              s.Contains("Syntax:"))), 
            Times.Once);
    }

    [Fact]
    public async Task HelpCommand_WithUnknownTopic_ShouldShowNoHelpFound()
    {
        // Arrange
        var command = new HelpCommand();

        // Act
        await command.ExecuteAsync(_mockPlayer.Object, "unknowncommand", 38);

        // Assert
        _mockPlayer.Verify(p => p.SendFormattedMessageAsync(
            It.Is<string>(s => s.Contains("No help available for 'unknowncommand'"))), 
            Times.Once);
    }

    [Theory]
    [InlineData("quit")]
    [InlineData("q")]
    [InlineData("who")]
    [InlineData("score")]
    [InlineData("sc")]
    [InlineData("stat")]
    public async Task HelpCommand_WithKnownTopics_ShouldShowSpecificHelp(string topic)
    {
        // Arrange
        var command = new HelpCommand();

        // Act
        await command.ExecuteAsync(_mockPlayer.Object, topic, 38);

        // Assert
        _mockPlayer.Verify(p => p.SendFormattedMessageAsync(
            It.Is<string>(s => s.Contains("Syntax:"))), 
            Times.Once);
    }

    [Fact]
    public void HelpCommand_Properties_ShouldBeCorrect()
    {
        // Arrange
        var command = new HelpCommand();

        // Assert
        command.Name.Should().Be("help");
        command.Aliases.Should().BeEmpty();
        command.MinimumPosition.Should().Be(PlayerPosition.Sleeping);
        command.MinimumLevel.Should().Be(1);
        command.IsEnabled.Should().BeTrue();
    }

    #endregion

    #region BaseCommand Tests

    [Fact]
    public void BaseCommand_DefaultValues_ShouldBeCorrect()
    {
        // Arrange
        var command = new TestBaseCommand();

        // Assert
        command.Aliases.Should().BeEmpty();
        command.MinimumPosition.Should().Be(PlayerPosition.Standing);
        command.MinimumLevel.Should().Be(1);
        command.AllowMob.Should().BeFalse();
        command.IsEnabled.Should().BeTrue();
    }

    private class TestBaseCommand : BaseCommand
    {
        public override string Name => "test";

        public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
        {
            await Task.CompletedTask;
        }
    }

    #endregion
}