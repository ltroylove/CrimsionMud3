using Xunit;
using FluentAssertions;
using C3Mud.Core.Commands;
using C3Mud.Core.Commands.Basic;
using C3Mud.Core.Players;
using C3Mud.Core.Networking;
using Moq;
using System.Diagnostics;

namespace C3Mud.Tests.Commands;

/// <summary>
/// Tests for command processing system with legacy compatibility
/// Ensures exact output matching with original MUD commands
/// </summary>
public class LegacyCommandProcessingTests
{
    [Fact]
    public void LegacyCommandProcessor_Constructor_ShouldInitialize()
    {
        // ARRANGE & ACT - Should fail as LegacyCommandProcessor doesn't exist
        var act = () => new LegacyCommandProcessor();
        
        // ASSERT
        act.Should().NotThrow("LegacyCommandProcessor should be implemented");
    }
    
    [Theory]
    [InlineData("look", typeof(LookCommand))]
    [InlineData("l", typeof(LookCommand))]
    [InlineData("quit", typeof(QuitCommand))]
    [InlineData("q", typeof(QuitCommand))]
    [InlineData("help", typeof(HelpCommand))]
    [InlineData("score", typeof(ScoreCommand))]
    [InlineData("sc", typeof(ScoreCommand))]
    [InlineData("who", typeof(WhoCommand))]
    [InlineData("say", typeof(SayCommand))]
    public async Task ProcessCommandAsync_BasicCommands_ShouldResolveToCorrectCommandTypes(string input, Type expectedCommandType)
    {
        // ARRANGE - Should fail as command processor doesn't exist
        var processor = new LegacyCommandProcessor();
        var mockPlayer = new Mock<IPlayer>();
        var mockConnection = new Mock<IConnectionDescriptor>();
        mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
        
        // ACT
        var act = async () => await processor.ProcessCommandAsync(mockPlayer.Object, input);
        
        // ASSERT
        act.Should().NotThrowAsync("should process basic commands");
        
        // Verify correct command type was executed
        var commandInstance = processor.GetLastExecutedCommand();
        commandInstance.Should().NotBeNull();
        commandInstance.Should().BeOfType(expectedCommandType);
    }
    
    [Fact]
    public async Task ProcessCommandAsync_LookCommand_ShouldOutputExactLegacyFormat()
    {
        // ARRANGE - Should match original look command output exactly
        var processor = new LegacyCommandProcessor();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var mockPlayer = new Mock<IPlayer>();
        mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
        mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        
        var sentMessages = new List<string>();
        mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                     .Callback<string>(msg => sentMessages.Add(msg))
                     .Returns(Task.CompletedTask);
        
        // ACT
        await processor.ProcessCommandAsync(mockPlayer.Object, "look");
        
        // ASSERT - Should match original look output format
        sentMessages.Should().NotBeEmpty("look command should send output");
        var lookOutput = string.Join("", sentMessages);
        
        // Verify legacy format elements (based on original look command output)
        lookOutput.Should().Contain("You are standing", "should show location description");
        lookOutput.Should().ContainAll(new[] { "\r\n" }, "should use proper line endings");
    }
    
    [Fact]
    public async Task ProcessCommandAsync_QuitCommand_ShouldMatchLegacyQuitBehavior()
    {
        // ARRANGE
        var processor = new LegacyCommandProcessor();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var mockPlayer = new Mock<IPlayer>();
        mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
        mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        
        var sentMessages = new List<string>();
        mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                     .Callback<string>(msg => sentMessages.Add(msg))
                     .Returns(Task.CompletedTask);
        
        // ACT
        await processor.ProcessCommandAsync(mockPlayer.Object, "quit");
        
        // ASSERT - Should match original quit message
        var quitOutput = string.Join("", sentMessages);
        quitOutput.Should().Contain("Goodbye", "should show legacy quit message");
        
        // Verify player disconnect was triggered
        mockPlayer.Verify(p => p.DisconnectAsync(It.IsAny<string>()), Times.Once);
    }
    
    [Fact]
    public async Task ProcessCommandAsync_HelpCommand_ShouldShowLegacyHelpFormat()
    {
        // ARRANGE
        var processor = new LegacyCommandProcessor();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var mockPlayer = new Mock<IPlayer>();
        mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
        
        var sentMessages = new List<string>();
        mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                     .Callback<string>(msg => sentMessages.Add(msg))
                     .Returns(Task.CompletedTask);
        
        // ACT
        await processor.ProcessCommandAsync(mockPlayer.Object, "help");
        
        // ASSERT - Should match original help format
        var helpOutput = string.Join("", sentMessages);
        helpOutput.Should().Contain("Available commands:", "should show command list header");
        helpOutput.Should().ContainAll(new[] { "look", "quit", "help", "score", "who" }, 
                                      "should list basic commands");
    }
    
    [Fact]
    public async Task ProcessCommandAsync_ScoreCommand_ShouldShowLegacyScoreFormat()
    {
        // ARRANGE
        var processor = new LegacyCommandProcessor();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var mockPlayer = new Mock<IPlayer>();
        mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
        mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        mockPlayer.Setup(p => p.Level).Returns(25);
        
        var sentMessages = new List<string>();
        mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                     .Callback<string>(msg => sentMessages.Add(msg))
                     .Returns(Task.CompletedTask);
        
        // ACT
        await processor.ProcessCommandAsync(mockPlayer.Object, "score");
        
        // ASSERT - Should match original score display format
        var scoreOutput = string.Join("", sentMessages);
        scoreOutput.Should().ContainAll(new[] { "TestPlayer", "Level: 25" }, 
                                       "should show player name and level");
        scoreOutput.Should().Contain("Hit Points:", "should show hit points");
        scoreOutput.Should().Contain("Experience:", "should show experience");
    }
    
    [Fact]
    public async Task ProcessCommandAsync_WhoCommand_ShouldShowConnectedPlayersInLegacyFormat()
    {
        // ARRANGE
        var processor = new LegacyCommandProcessor();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var mockPlayer = new Mock<IPlayer>();
        mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
        
        var sentMessages = new List<string>();
        mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                     .Callback<string>(msg => sentMessages.Add(msg))
                     .Returns(Task.CompletedTask);
        
        // ACT
        await processor.ProcessCommandAsync(mockPlayer.Object, "who");
        
        // ASSERT - Should match original who command format
        var whoOutput = string.Join("", sentMessages);
        whoOutput.Should().Contain("Players currently online:", "should show who header");
        whoOutput.Should().Contain("Total players online:", "should show player count");
    }
    
    [Theory]
    [InlineData("say Hello everyone!")]
    [InlineData("say")]
    [InlineData("'Testing the say command")]
    public async Task ProcessCommandAsync_SayCommand_ShouldHandleSayWithLegacyFormat(string input)
    {
        // ARRANGE
        var processor = new LegacyCommandProcessor();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var mockPlayer = new Mock<IPlayer>();
        mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
        mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        
        var sentMessages = new List<string>();
        mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                     .Callback<string>(msg => sentMessages.Add(msg))
                     .Returns(Task.CompletedTask);
        
        // ACT
        await processor.ProcessCommandAsync(mockPlayer.Object, input);
        
        // ASSERT - Should match original say behavior
        var output = string.Join("", sentMessages);
        
        if (input.Length <= 3 || input == "say") // Empty say
        {
            output.Should().Contain("Say what?", "should prompt for message when no text provided");
        }
        else
        {
            output.Should().Contain("You say", "should show say confirmation to sender");
        }
    }
    
    [Theory]
    [InlineData("nonexistentcommand")]
    [InlineData("xyz")]
    [InlineData("badcmd")]
    public async Task ProcessCommandAsync_UnknownCommand_ShouldShowLegacyErrorMessage(string unknownCommand)
    {
        // ARRANGE
        var processor = new LegacyCommandProcessor();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var mockPlayer = new Mock<IPlayer>();
        mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
        
        var sentMessages = new List<string>();
        mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                     .Callback<string>(msg => sentMessages.Add(msg))
                     .Returns(Task.CompletedTask);
        
        // ACT
        await processor.ProcessCommandAsync(mockPlayer.Object, unknownCommand);
        
        // ASSERT - Should match original unknown command message
        var output = string.Join("", sentMessages);
        output.Should().Contain("Huh?", "should show legacy unknown command message");
    }
    
    [Theory]
    [InlineData("  look  ")]  // Leading/trailing spaces
    [InlineData("\tlook\t")] // Tabs
    [InlineData("LOOK")]     // Case variations
    [InlineData("Look")]
    public async Task ProcessCommandAsync_WhitespaceAndCaseHandling_ShouldMatchLegacyBehavior(string input)
    {
        // ARRANGE - Should handle whitespace/case like original parser
        var processor = new LegacyCommandProcessor();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var mockPlayer = new Mock<IPlayer>();
        mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
        
        // ACT
        var act = async () => await processor.ProcessCommandAsync(mockPlayer.Object, input);
        
        // ASSERT - Should process successfully after normalization
        act.Should().NotThrowAsync("should handle whitespace and case variations");
        
        var commandInstance = processor.GetLastExecutedCommand();
        commandInstance.Should().BeOfType<LookCommand>("should resolve to look command regardless of formatting");
    }
    
    [Fact]
    public async Task ProcessCommandAsync_EmptyInput_ShouldHandleGracefully()
    {
        // ARRANGE
        var processor = new LegacyCommandProcessor();
        var mockConnection = new Mock<IConnectionDescriptor>();
        var mockPlayer = new Mock<IPlayer>();
        mockPlayer.Setup(p => p.Connection).Returns(mockConnection.Object);
        
        // ACT
        var act = async () => await processor.ProcessCommandAsync(mockPlayer.Object, "");
        
        // ASSERT - Should handle empty input like original (usually just ignore)
        act.Should().NotThrowAsync("should handle empty input gracefully");
    }
    
    [Fact]
    public void CommandAliases_BasicCommands_ShouldMatchLegacyAliases()
    {
        // ARRANGE - Should fail as alias system doesn't exist
        var processor = new LegacyCommandProcessor();
        
        // ACT & ASSERT - Verify legacy aliases are registered
        processor.GetRegisteredAliases().Should().ContainKeys(new[] 
        {
            "l",     // look
            "q",     // quit
            "sc",    // score
            "'",     // say
            "qui",   // partial quit
            "loo"    // partial look
        }, "should register all legacy command aliases");
    }
}