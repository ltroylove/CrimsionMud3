using C3Mud.Core.Commands;
using C3Mud.Core.Players;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace C3Mud.Tests.Commands;

/// <summary>
/// Tests for the CommandRegistry system
/// Validates command registration, lookup, and execution
/// </summary>
public class CommandRegistryTests
{
    private readonly Mock<ILogger<CommandRegistry>> _mockLogger;
    private readonly CommandRegistry _registry;
    private readonly Mock<IPlayer> _mockPlayer;

    public CommandRegistryTests()
    {
        _mockLogger = new Mock<ILogger<CommandRegistry>>();
        _registry = new CommandRegistry(_mockLogger.Object);
        _mockPlayer = new Mock<IPlayer>();
        _mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        _mockPlayer.Setup(p => p.Level).Returns(10);
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Standing);
    }

    [Fact]
    public void RegisterCommand_WithValidCommand_ShouldRegisterSuccessfully()
    {
        // Arrange
        var command = new TestCommand();

        // Act
        _registry.RegisterCommand(command);

        // Assert
        var foundCommand = _registry.FindCommand("test");
        foundCommand.Should().Be(command);
    }

    [Fact]
    public void RegisterCommand_WithNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => _registry.RegisterCommand(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FindCommand_WithExactMatch_ShouldReturnCommand()
    {
        // Arrange
        var command = new TestCommand();
        _registry.RegisterCommand(command);

        // Act
        var result = _registry.FindCommand("test");

        // Assert
        result.Should().Be(command);
    }

    [Fact]
    public void FindCommand_WithAlias_ShouldReturnCommand()
    {
        // Arrange
        var command = new TestCommand();
        _registry.RegisterCommand(command);

        // Act
        var result = _registry.FindCommand("t");

        // Assert
        result.Should().Be(command);
    }

    [Fact]
    public void FindCommand_WithAbbreviation_ShouldReturnCommand()
    {
        // Arrange
        var command = new TestCommand();
        _registry.RegisterCommand(command);

        // Act
        var result = _registry.FindCommand("te");

        // Assert
        result.Should().Be(command);
    }

    [Fact]
    public void FindCommand_WithNonExistentCommand_ShouldReturnNull()
    {
        // Act
        var result = _registry.FindCommand("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindCommand_WithEmptyString_ShouldReturnNull()
    {
        // Act
        var result = _registry.FindCommand("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetAllCommands_WithMultipleCommands_ShouldReturnDistinctCommands()
    {
        // Arrange
        var command1 = new TestCommand();
        var command2 = new SecondTestCommand();
        _registry.RegisterCommand(command1);
        _registry.RegisterCommand(command2);

        // Act
        var commands = _registry.GetAllCommands();

        // Assert
        commands.Should().HaveCount(2);
        commands.Should().Contain(command1);
        commands.Should().Contain(command2);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithValidCommand_ShouldExecuteSuccessfully()
    {
        // Arrange
        var command = new TestCommand();
        _registry.RegisterCommand(command);

        // Act
        var result = await _registry.ExecuteCommandAsync(_mockPlayer.Object, "test argument");

        // Assert
        result.Should().BeTrue();
        command.WasExecuted.Should().BeTrue();
        command.ExecutedArguments.Should().Be("argument");
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithInvalidCommand_ShouldReturnFalse()
    {
        // Act
        var result = await _registry.ExecuteCommandAsync(_mockPlayer.Object, "nonexistent");

        // Assert
        result.Should().BeFalse();
        _mockPlayer.Verify(p => p.SendMessageAsync("Huh? 'nonexistent' is not a command."), Times.Once);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithDisabledCommand_ShouldReturnFalse()
    {
        // Arrange
        var command = new DisabledTestCommand();
        _registry.RegisterCommand(command);

        // Act
        var result = await _registry.ExecuteCommandAsync(_mockPlayer.Object, "disabled");

        // Assert
        result.Should().BeFalse();
        _mockPlayer.Verify(p => p.SendMessageAsync("That command is currently disabled."), Times.Once);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithInsufficientLevel_ShouldReturnFalse()
    {
        // Arrange
        var command = new HighLevelTestCommand();
        _registry.RegisterCommand(command);
        _mockPlayer.Setup(p => p.Level).Returns(5); // Below required level

        // Act
        var result = await _registry.ExecuteCommandAsync(_mockPlayer.Object, "highlevel");

        // Assert
        result.Should().BeFalse();
        _mockPlayer.Verify(p => p.SendMessageAsync("You don't have the experience to use that command."), Times.Once);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithInsufficientPosition_ShouldReturnFalse()
    {
        // Arrange
        var command = new TestCommand();
        _registry.RegisterCommand(command);
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Sleeping);

        // Act
        var result = await _registry.ExecuteCommandAsync(_mockPlayer.Object, "test");

        // Assert
        result.Should().BeFalse();
        _mockPlayer.Verify(p => p.SendMessageAsync("You can't do that while sleeping."), Times.Once);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithEmptyCommand_ShouldReturnFalse()
    {
        // Act
        var result = await _registry.ExecuteCommandAsync(_mockPlayer.Object, "");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithNullPlayer_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _registry.ExecuteCommandAsync(null!, "test"));
    }

    [Fact]
    public void AutoRegisterCommands_ShouldLogInformation()
    {
        // Act
        _registry.AutoRegisterCommands();

        // Assert
        // Verify that logging occurred (implementation will depend on actual command types in assembly)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Auto-registered")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    // Test command implementations for testing
    private class TestCommand : ICommand
    {
        public string Name => "test";
        public string[] Aliases => new[] { "t" };
        public PlayerPosition MinimumPosition => PlayerPosition.Standing;
        public int MinimumLevel => 1;
        public bool AllowMob => false;
        public bool IsEnabled => true;

        public bool WasExecuted { get; private set; }
        public string ExecutedArguments { get; private set; } = string.Empty;

        public async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
        {
            WasExecuted = true;
            ExecutedArguments = arguments;
            await Task.CompletedTask;
        }
    }

    private class SecondTestCommand : ICommand
    {
        public string Name => "second";
        public string[] Aliases => Array.Empty<string>();
        public PlayerPosition MinimumPosition => PlayerPosition.Standing;
        public int MinimumLevel => 1;
        public bool AllowMob => false;
        public bool IsEnabled => true;

        public async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
        {
            await Task.CompletedTask;
        }
    }

    private class DisabledTestCommand : ICommand
    {
        public string Name => "disabled";
        public string[] Aliases => Array.Empty<string>();
        public PlayerPosition MinimumPosition => PlayerPosition.Standing;
        public int MinimumLevel => 1;
        public bool AllowMob => false;
        public bool IsEnabled => false;

        public async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
        {
            await Task.CompletedTask;
        }
    }

    private class HighLevelTestCommand : ICommand
    {
        public string Name => "highlevel";
        public string[] Aliases => Array.Empty<string>();
        public PlayerPosition MinimumPosition => PlayerPosition.Standing;
        public int MinimumLevel => 10;
        public bool AllowMob => false;
        public bool IsEnabled => true;

        public async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
        {
            await Task.CompletedTask;
        }
    }
}