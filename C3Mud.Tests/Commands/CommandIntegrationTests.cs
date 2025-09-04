using FluentAssertions;
using C3Mud.Core.Commands.Basic;
using C3Mud.Core.Players;
using C3Mud.Core.Players.Models;
using C3Mud.Core.Networking;
using C3Mud.Core.Services;
using Moq;
using System.Text;
using Xunit;

namespace C3Mud.Tests.Commands;

/// <summary>
/// TDD Red Phase - Tests for complete command integration with real data
/// These tests SHOULD FAIL initially to verify placeholder functionality removal
/// </summary>
public class CommandIntegrationTests
{
    private readonly Mock<IPlayer> _mockPlayer;
    private readonly Mock<IConnectionDescriptor> _mockConnection;
    private readonly List<string> _sentMessages;

    public CommandIntegrationTests()
    {
        _mockPlayer = new Mock<IPlayer>();
        _mockConnection = new Mock<IConnectionDescriptor>();
        _sentMessages = new List<string>();

        _mockPlayer.Setup(p => p.Connection).Returns(_mockConnection.Object);
        _mockConnection.Setup(c => c.SendDataAsync(It.IsAny<string>()))
                      .Callback<string>(msg => _sentMessages.Add(msg))
                      .Returns(Task.CompletedTask);

        // Set up player message methods to forward to connection
        _mockPlayer.Setup(p => p.SendMessageAsync(It.IsAny<string>()))
                  .Callback<string>(msg => _sentMessages.Add(msg + "\r\n"))
                  .Returns(Task.CompletedTask);
        _mockPlayer.Setup(p => p.SendFormattedMessageAsync(It.IsAny<string>()))
                  .Callback<string>(msg => _sentMessages.Add(msg + "\r\n"))
                  .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// TDD Red Phase Test - ScoreCommand should show real player data from LegacyPlayerFileData
    /// This test SHOULD FAIL initially because ScoreCommand uses placeholder hardcoded values
    /// </summary>
    [Fact]
    public async Task ScoreCommand_WithRealPlayerData_ShowsCorrectStats()
    {
        // ARRANGE
        var realPlayerData = new LegacyPlayerFileData
        {
            Name = "Testchar",
            Level = 25,
            Points = new LegacyCharPointData
            {
                Hit = 180,
                MaxHit = 200,
                Mana = 85,
                MaxMana = 100,
                Move = 45,
                MaxMove = 150,
                Experience = 50000,
                Gold = 2500,
                Armor = 5
            },
            Abilities = new LegacyCharAbilityData
            {
                Strength = 18,
                Intelligence = 16,
                Wisdom = 14,
                Dexterity = 17,
                Constitution = 19,
                Charisma = 13,
                StrengthAdd = 50  // 18/50 strength
            },
            Alignment = 100
        };

        _mockPlayer.Setup(p => p.Name).Returns("Testchar");
        _mockPlayer.Setup(p => p.Level).Returns(25);
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Standing);
        _mockPlayer.Setup(p => p.LegacyPlayerFileData).Returns(realPlayerData);

        var scoreCommand = new ScoreCommand();

        // ACT
        await scoreCommand.ExecuteAsync(_mockPlayer.Object, "", 1);

        // ASSERT - Should show REAL player data, not hardcoded placeholders
        var output = string.Join("", _sentMessages);
        
        // Should show REAL hit points from LegacyPlayerFileData, not hardcoded "100/100"
        output.Should().Contain("Hit Points:&N    180/200", 
            "should show actual hit points from LegacyPlayerFileData");
        
        // Should show REAL mana from LegacyPlayerFileData, not hardcoded "50/50"
        output.Should().Contain("Mana Points:&N   85/100", 
            "should show actual mana from LegacyPlayerFileData");
        
        // Should show REAL movement from LegacyPlayerFileData, not hardcoded "100/100"
        output.Should().Contain("Move Points:&N   45/150", 
            "should show actual movement from LegacyPlayerFileData");
        
        // Should show REAL experience from LegacyPlayerFileData, not hardcoded "0"
        output.Should().Contain("Experience:&N    50000", 
            "should show actual experience from LegacyPlayerFileData");
        
        // Should show REAL armor class from LegacyPlayerFileData
        output.Should().Contain("Armor Class:&N   5", 
            "should show actual AC from LegacyPlayerFileData");
        
        // Should show REAL alignment from LegacyPlayerFileData, not hardcoded "Neutral (0)"
        output.Should().Contain("&wGood&N (100)", 
            "should show actual alignment from LegacyPlayerFileData");

        // Should NOT contain placeholder development messages
        output.Should().NotContain("Note: Full character statistics will be available", 
            "should remove placeholder development messages");
        
        output.Should().NotContain("&KNote:", 
            "should remove all development notes");
    }

    /// <summary>
    /// TDD Red Phase Test - HelpCommand should show "Available commands:" format
    /// This test SHOULD FAIL initially because HelpCommand shows custom format
    /// </summary>
    [Fact]
    public async Task HelpCommand_ShowsLegacyFormat_AvailableCommands()
    {
        // ARRANGE
        var helpCommand = new HelpCommand();

        // ACT
        await helpCommand.ExecuteAsync(_mockPlayer.Object, "", 1);

        // ASSERT - Should match legacy format with "Available commands:" header
        var output = string.Join("", _sentMessages);
        
        // Should start with "Available commands:" not "C3MUD Help System"
        output.Should().Contain("Available commands:", 
            "should show legacy help format starting with 'Available commands:'");
        
        // Should list commands in simple format, not fancy categorized format
        output.Should().ContainAll(new[] { "look", "quit", "help", "score", "who", "say" }, 
            "should list all basic commands");
        
        // Should NOT contain custom development help format
        output.Should().NotContain("C3MUD Help System", 
            "should not show custom development help header");
        
        output.Should().NotContain("=================", 
            "should not show fancy formatting in basic help");
        
        output.Should().NotContain("Basic Commands:", 
            "should not categorize commands in basic help");
        
        output.Should().NotContain("Note: This is an early development", 
            "should remove development notes");
    }

    /// <summary>
    /// TDD Red Phase Test - SayCommand should broadcast to room
    /// This test SHOULD FAIL initially because SayCommand only sends to current player
    /// </summary>
    [Fact]
    public async Task SayCommand_BroadcastsToRoom_AllPlayersReceiveMessage()
    {
        // ARRANGE
        var sayCommand = new SayCommand();
        
        // Setup current room and other players in room
        var currentRoom = 20385;
        _mockPlayer.Setup(p => p.CurrentRoomVnum).Returns(currentRoom);
        _mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        _mockPlayer.Setup(p => p.Id).Returns("testplayer");
        
        // Mock other players in the room
        var otherPlayer1 = new Mock<IPlayer>();
        var otherPlayer2 = new Mock<IPlayer>();
        var otherPlayersMessages1 = new List<string>();
        var otherPlayersMessages2 = new List<string>();
        
        otherPlayer1.Setup(p => p.Id).Returns("player1");
        otherPlayer1.Setup(p => p.IsConnected).Returns(true);
        otherPlayer1.Setup(p => p.SendFormattedMessageAsync(It.IsAny<string>()))
                   .Callback<string>(msg => otherPlayersMessages1.Add(msg))
                   .Returns(Task.CompletedTask);
                   
        otherPlayer2.Setup(p => p.Id).Returns("player2");
        otherPlayer2.Setup(p => p.IsConnected).Returns(true);
        otherPlayer2.Setup(p => p.SendFormattedMessageAsync(It.IsAny<string>()))
                   .Callback<string>(msg => otherPlayersMessages2.Add(msg))
                   .Returns(Task.CompletedTask);

        // Set up room player manager
        var mockRoomManager = new Mock<IRoomPlayerManager>();
        var roomPlayers = new List<IPlayer> { _mockPlayer.Object, otherPlayer1.Object, otherPlayer2.Object };
        mockRoomManager.Setup(rm => rm.GetPlayersInRoom(currentRoom))
                      .Returns(roomPlayers);
        
        // Inject the room manager into SayCommand
        SayCommand.SetRoomPlayerManager(mockRoomManager.Object);

        // ACT
        await sayCommand.ExecuteAsync(_mockPlayer.Object, "Hello everyone!", 1);

        // ASSERT
        
        // Speaker should see "You say" format
        var speakerOutput = string.Join("", _sentMessages);
        speakerOutput.Should().Contain("&YYou say, &W'Hello everyone!'&N", 
            "speaker should see their own say message");
        
        // Other players should receive the message
        var totalMessagesCount = _sentMessages.Count + otherPlayersMessages1.Count + otherPlayersMessages2.Count;
        totalMessagesCount.Should().BeGreaterThan(1, 
            "should send messages to multiple players in room");
        
        // Verify other players got the message in correct format
        otherPlayersMessages1.Should().NotBeEmpty("first other player should receive message");
        otherPlayersMessages2.Should().NotBeEmpty("second other player should receive message");
        
        otherPlayersMessages1[0].Should().Contain("&YTestPlayer says, &W'Hello everyone!'&N", 
            "other players should see speaker name in say message");
        otherPlayersMessages2[0].Should().Contain("&YTestPlayer says, &W'Hello everyone!'&N", 
            "other players should see speaker name in say message");
    }

    /// <summary>
    /// TDD Red Phase Test - All commands should have NO TODO comments
    /// This test SHOULD FAIL initially because commands contain placeholder TODOs
    /// </summary>
    [Fact]
    public void Commands_NoPlaceholderTODOs_AllIntegrationsComplete()
    {
        // ARRANGE & ACT - Read all command files and check for TODOs
        var commandFiles = new[]
        {
            @"C:\Projects\C3Mud\C3Mud.Core\Commands\Basic\ScoreCommand.cs",
            @"C:\Projects\C3Mud\C3Mud.Core\Commands\Basic\HelpCommand.cs",
            @"C:\Projects\C3Mud\C3Mud.Core\Commands\Basic\SayCommand.cs"
        };

        var foundTodos = new List<string>();

        foreach (var file in commandFiles)
        {
            if (File.Exists(file))
            {
                var content = File.ReadAllText(file);
                var lines = content.Split('\n');
                
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("TODO:") || lines[i].Contains("PLACEHOLDER"))
                    {
                        foundTodos.Add($"{Path.GetFileName(file)}:{i + 1} - {lines[i].Trim()}");
                    }
                }
            }
        }

        // ASSERT - Should have NO TODOs remaining after integration
        foundTodos.Should().BeEmpty(
            $"All TODO comments should be removed after integration. Found: {string.Join(", ", foundTodos)}");
    }

    /// <summary>
    /// TDD Red Phase Test - ScoreCommand should not show placeholder messages
    /// This test SHOULD FAIL initially because ScoreCommand shows development notes
    /// </summary>
    [Fact]
    public async Task ScoreCommand_NoPlaceholderMessages_ProductionReady()
    {
        // ARRANGE
        _mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        _mockPlayer.Setup(p => p.Level).Returns(10);
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Standing);
        
        var scoreCommand = new ScoreCommand();

        // ACT
        await scoreCommand.ExecuteAsync(_mockPlayer.Object, "", 1);

        // ASSERT - Should NOT contain any placeholder or development messages
        var output = string.Join("", _sentMessages);
        
        output.Should().NotContain("Note:", "should not show development notes");
        output.Should().NotContain("will be available once", "should not show 'will be available' messages");
        output.Should().NotContain("&KNote:", "should not show colored development notes");
        output.Should().NotContain("character system is fully implemented", "should not reference implementation status");
    }

    /// <summary>
    /// TDD Red Phase Test - HelpCommand should not show development messages
    /// This test SHOULD FAIL initially because HelpCommand shows development notes
    /// </summary>
    [Fact]
    public async Task HelpCommand_NoPlaceholderMessages_ProductionReady()
    {
        // ARRANGE
        var helpCommand = new HelpCommand();

        // ACT
        await helpCommand.ExecuteAsync(_mockPlayer.Object, "", 1);

        // ASSERT - Should NOT contain development messages
        var output = string.Join("", _sentMessages);
        
        output.Should().NotContain("early development version", "should not mention development version");
        output.Should().NotContain("features are still being implemented", "should not mention implementation status");
        output.Should().NotContain("coming soon", "should not mention features coming soon");
        output.Should().NotContain("&KNote:", "should not show development notes");
    }

    /// <summary>
    /// TDD Red Phase Test - Verify IPlayer interface has LegacyPlayerFileData property
    /// This test SHOULD FAIL initially because IPlayer doesn't have this property yet
    /// </summary>
    [Fact]
    public void IPlayer_HasLegacyPlayerFileDataProperty_ForScoreIntegration()
    {
        // ARRANGE & ACT
        var playerType = typeof(IPlayer);
        var legacyDataProperty = playerType.GetProperty("LegacyPlayerFileData");

        // ASSERT - IPlayer should have LegacyPlayerFileData property for score integration
        legacyDataProperty.Should().NotBeNull("IPlayer should have LegacyPlayerFileData property for score command integration");
        legacyDataProperty!.PropertyType.Should().Be(typeof(LegacyPlayerFileData), "property should return LegacyPlayerFileData struct");
    }

    /// <summary>
    /// Performance Test - Command execution should remain fast after integration
    /// </summary>
    [Fact]
    public async Task CommandIntegration_MaintainsPerformance_UnderTargetTime()
    {
        // ARRANGE
        var scoreCommand = new ScoreCommand();
        _mockPlayer.Setup(p => p.Name).Returns("TestPlayer");
        _mockPlayer.Setup(p => p.Level).Returns(25);
        _mockPlayer.Setup(p => p.Position).Returns(PlayerPosition.Standing);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // ACT - Execute command multiple times to test average performance
        for (int i = 0; i < 10; i++)
        {
            await scoreCommand.ExecuteAsync(_mockPlayer.Object, "", 1);
            _sentMessages.Clear(); // Clear for next iteration
        }

        stopwatch.Stop();

        // ASSERT - Should maintain performance target of <10ms per command
        var averageMs = stopwatch.ElapsedMilliseconds / 10.0;
        averageMs.Should().BeLessThan(10, "command execution should stay under 10ms target");
    }
}