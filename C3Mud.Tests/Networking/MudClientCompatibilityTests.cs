using C3Mud.Core.Networking;
using FluentAssertions;
using Moq;
using Xunit;
using System.Text;

namespace C3Mud.Tests.Networking;

/// <summary>
/// Tests compatibility with classic MUD clients that would have connected to the original C MUD
/// These tests ensure our C# implementation works with clients like TinyTalk, zMUD, MUSHclient, etc.
/// </summary>
public class MudClientCompatibilityTests
{
    private readonly ITelnetProtocolHandler _telnetHandler;

    public MudClientCompatibilityTests()
    {
        _telnetHandler = new TelnetProtocolHandler();
    }

    #region Classic MUD Client Scenarios

    [Theory]
    [InlineData("TinyTalk")]      // Early Mac MUD client
    [InlineData("SimpleMU")]      // Windows MUD client  
    [InlineData("TinyFugue")]     // Unix MUD client
    [InlineData("MUSHclient")]    // Advanced Windows client
    [InlineData("zMUD")]          // Popular Windows client
    [InlineData("Generic Telnet")] // Basic telnet client
    public void ClassicMudClients_ShouldConnectSuccessfully(string clientType)
    {
        // Simulate connection from various classic MUD clients
        var mockConnection = CreateMockConnection(clientType, "mudclient.example.com");
        
        // Test initial telnet negotiation
        var initialNegotiation = _telnetHandler.GetInitialNegotiation();
        
        // All clients should receive standard MUD telnet sequence
        initialNegotiation.Should().NotBeNull();
        initialNegotiation.Should().Contain(TelnetConstants.IAC);
        initialNegotiation.Should().Contain(TelnetConstants.SUPPRESS_GO_AHEAD);
        
        // Test basic command processing
        var loginCommand = "Testchar\r\n";
        var loginBytes = Encoding.UTF8.GetBytes(loginCommand);
        var result = _telnetHandler.ProcessIncomingData(loginBytes, mockConnection);
        
        result.Text.Should().Be("Testchar");
        result.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void TelnetBasedClients_ShouldHandleNegotiationSequences()
    {
        // Many classic clients send standard telnet negotiation sequences
        var mockConnection = CreateMockConnection("Generic Telnet", "client.example.com");
        
        // Simulate client sending terminal type negotiation
        var clientNegotiation = new byte[]
        {
            TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.TERMINAL_TYPE
        };
        
        var response = _telnetHandler.HandleNegotiation(clientNegotiation, mockConnection);
        
        response.Should().NotBeNull();
        response.Should().Equal(new byte[] 
        { 
            TelnetConstants.IAC, TelnetConstants.DO, TelnetConstants.TERMINAL_TYPE 
        });
    }

    #endregion

    #region ANSI Color Support Compatibility

    [Theory]
    [InlineData("vt100", true)]         // VT100 supports ANSI
    [InlineData("vt102", true)]         // VT102 supports ANSI  
    [InlineData("ansi", true)]          // Generic ANSI terminal
    [InlineData("xterm", true)]         // X Terminal supports ANSI
    [InlineData("dumb", false)]         // Dumb terminal no colors
    [InlineData("tty", false)]          // Basic TTY no colors
    [InlineData("oldclient", false)]    // Identified old client
    public void ClientColorSupport_ShouldBeDetectedProperly(string clientType, bool supportsColor)
    {
        var mockConnection = CreateMockConnection("TestClient", "client.example.com");
        
        // Simulate client type detection (in real implementation this would be more sophisticated)
        if (!supportsColor)
        {
            Mock.Get(mockConnection).Setup(c => c.Host).Returns($"{clientType}.client.com");
        }
        
        var coloredText = "&RRed Text&N &GGreen Text&N";
        var result = _telnetHandler.ProcessColorCodes(coloredText, mockConnection);
        
        if (supportsColor)
        {
            result.Should().Contain("\x1B["); // Should contain ANSI codes
            result.Should().NotContain("&R");
            result.Should().NotContain("&G");
        }
        else
        {
            result.Should().NotContain("\x1B["); // Should not contain ANSI codes
            result.Should().NotContain("&R");
            result.Should().NotContain("&G");
            result.Should().Be("Red Text Green Text"); // Stripped clean
        }
    }

    #endregion

    #region Password Input Security

    [Fact] 
    public void PasswordInput_ShouldDisableEchoSecurely()
    {
        // Critical security test - password input must disable echo like original MUD
        var mockConnection = CreateMockConnection("MUSHclient", "client.example.com");
        
        // When server disables echo for password input
        var echoOff = _telnetHandler.SetEcho(false);
        
        // Should send IAC WILL ECHO (server will echo, hiding client input)
        echoOff.Should().Equal(new byte[] 
        { 
            TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.ECHO 
        });
        
        // When server enables echo for normal input  
        var echoOn = _telnetHandler.SetEcho(true);
        
        // Should send IAC WONT ECHO (server won't echo, client will)
        echoOn.Should().Equal(new byte[] 
        { 
            TelnetConstants.IAC, TelnetConstants.WONT, TelnetConstants.ECHO 
        });
    }

    [Fact]
    public void PasswordInput_WithEmbeddedTelnetSequences_ShouldBeSecure()
    {
        // Test password input with embedded telnet sequences (potential attack)
        var mockConnection = CreateMockConnection("TestClient", "127.0.0.1");
        
        var maliciousPassword = new byte[]
        {
            (byte)'p', (byte)'a', (byte)'s', (byte)'s',
            TelnetConstants.IAC, TelnetConstants.WONT, TelnetConstants.ECHO, // Trying to re-enable echo
            (byte)'w', (byte)'o', (byte)'r', (byte)'d',
            (byte)'\r', (byte)'\n'
        };
        
        var result = _telnetHandler.ProcessIncomingData(maliciousPassword, mockConnection);
        
        // Should extract clean password text and handle telnet sequence separately
        result.Text.Should().Be("password");
        result.IsComplete.Should().BeTrue();
        result.NegotiationResponse.Should().NotBeNull(); // Should respond to embedded negotiation
    }

    #endregion

    #region Line Ending Compatibility  

    [Theory]
    [InlineData("\r\n")]    // Windows/DOS clients
    [InlineData("\n")]      // Unix/Linux clients
    [InlineData("\r")]      // Old Mac clients
    public void DifferentLineEndings_ShouldAllBeHandled(string lineEnding)
    {
        // Original MUD needed to handle clients from different platforms
        var mockConnection = CreateMockConnection("MultiPlatformClient", "client.example.com");
        
        var command = $"north{lineEnding}";
        var commandBytes = Encoding.UTF8.GetBytes(command);
        
        var result = _telnetHandler.ProcessIncomingData(commandBytes, mockConnection);
        
        result.Text.Should().Be("north");
        result.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void MixedLineEndings_ShouldBeHandledGracefully()
    {
        // Some clients might send mixed line endings
        var mockConnection = CreateMockConnection("InconsistentClient", "client.example.com");
        
        var mixedInput = "look\r\nsay hello\ninv\r";
        var inputBytes = Encoding.UTF8.GetBytes(mixedInput);
        
        var result = _telnetHandler.ProcessIncomingData(inputBytes, mockConnection);
        
        // Should handle the complete input without errors
        result.Should().NotBeNull();
        result.ShouldClose.Should().BeFalse();
    }

    #endregion

    #region Binary Data and Corrupted Input

    [Fact]
    public void CorruptedInput_ShouldNotCrashServer()
    {
        // Original MUD needed to be robust against corrupted network input
        var mockConnection = CreateMockConnection("CorruptedClient", "suspicious.example.com");
        
        var corruptedData = new byte[] { 0xFF, 0xFE, 0x00, 0x01, 0xFF, 0xFF, 0x00 };
        
        var result = _telnetHandler.ProcessIncomingData(corruptedData, mockConnection);
        
        // Should not crash and should not close connection
        result.Should().NotBeNull();
        result.ShouldClose.Should().BeFalse();
    }

    [Fact]
    public void InvalidTelnetSequences_ShouldBeHandledSafely()
    {
        // Test malformed telnet sequences
        var mockConnection = CreateMockConnection("MalformedClient", "client.example.com");
        
        var malformedSequences = new byte[]
        {
            TelnetConstants.IAC, // IAC without command
            TelnetConstants.IAC, 200, // IAC with invalid command
            TelnetConstants.IAC, TelnetConstants.DO, // DO without option
            (byte)'h', (byte)'i', (byte)'\r', (byte)'\n'
        };
        
        var result = _telnetHandler.ProcessIncomingData(malformedSequences, mockConnection);
        
        // Should extract the text part safely
        result.Text.Should().Be("hi");
        result.IsComplete.Should().BeTrue();
        result.ShouldClose.Should().BeFalse();
    }

    #endregion

    #region Large Data Handling

    [Fact]
    public void LargeCommands_ShouldRespectOriginalLimits()
    {
        // Original MUD had input length limits to prevent abuse
        var mockConnection = CreateMockConnection("TestClient", "client.example.com");
        
        // Test at original MAX_RAW_INPUT_LENGTH (2048 bytes)
        var largeCommand = new string('x', 2048) + "\r\n";
        var largeBytes = Encoding.UTF8.GetBytes(largeCommand);
        
        var result = _telnetHandler.ProcessIncomingData(largeBytes, mockConnection);
        
        // Should handle up to original limits
        result.Text.Length.Should().Be(2048);
        result.IsComplete.Should().BeTrue();
        result.ShouldClose.Should().BeFalse();
    }

    [Fact]
    public void ExcessivelyLargeInput_ShouldBeHandledGracefully()
    {
        // Test beyond reasonable limits (potential DoS attempt)
        var mockConnection = CreateMockConnection("SuspiciousClient", "attacker.example.com");
        
        var massiveInput = new string('A', 10000) + "\r\n";
        var massiveBytes = Encoding.UTF8.GetBytes(massiveInput);
        
        var result = _telnetHandler.ProcessIncomingData(massiveBytes, mockConnection);
        
        // Should not crash the server
        result.Should().NotBeNull();
        // May truncate or reject, but should not crash
        result.ShouldClose.Should().BeFalse();
    }

    #endregion

    #region Performance Under Client Load

    [Fact]
    public void MultipleClientsSimultaneously_ShouldHandleEfficiently()
    {
        // Simulate multiple clients connecting and sending commands simultaneously
        var clientTasks = new List<Task<bool>>();
        
        for (int i = 0; i < 25; i++)
        {
            clientTasks.Add(Task.Run(() =>
            {
                var mockConnection = CreateMockConnection($"Client{i}", $"client{i}.example.com");
                
                // Each client sends several commands
                for (int j = 0; j < 10; j++)
                {
                    var command = $"say Message {j} from client\r\n";
                    var commandBytes = Encoding.UTF8.GetBytes(command);
                    
                    var result = _telnetHandler.ProcessIncomingData(commandBytes, mockConnection);
                    
                    if (result == null || !result.IsComplete)
                        return false;
                }
                
                return true;
            }));
        }
        
        // All clients should be processed successfully within reasonable time
        var allCompleted = Task.WaitAll(clientTasks.ToArray(), TimeSpan.FromSeconds(10));
        allCompleted.Should().BeTrue("All client operations should complete in reasonable time");
        
        clientTasks.All(t => t.Result).Should().BeTrue("All client commands should be processed successfully");
    }

    #endregion

    #region Output Formatting for Clients

    [Fact]
    public void OutputFormatting_ShouldMatchOriginalMudStyle()
    {
        var mockConnection = CreateMockConnection("TestClient", "client.example.com");
        
        // Test original MUD output formatting patterns
        var mudOutput = "You see:\r\n&Ga wooden sword&N lies here.\r\n&cA blue potion&N sits on the ground.";
        
        var result = _telnetHandler.ProcessOutgoingData(mudOutput, mockConnection);
        var resultString = Encoding.UTF8.GetString(result);
        
        // Should maintain proper line endings for telnet
        resultString.Should().Contain("\r\n");
        
        // Should process color codes
        resultString.Should().NotContain("&G");
        resultString.Should().NotContain("&c");
        resultString.Should().NotContain("&N");
        
        // Should not create double line endings
        resultString.Should().NotContain("\r\r\n");
    }

    [Fact]
    public void PromptHandling_ShouldWorkWithAllClients()
    {
        var mockConnection = CreateMockConnection("TestClient", "client.example.com");
        
        // Original MUD prompt format
        var promptText = "&W<&R100&W/&R100hp &B75&W/&B75m &Y50&W/&Y50mv>&N ";
        
        var result = _telnetHandler.ProcessOutgoingData(promptText, mockConnection);
        var resultString = Encoding.UTF8.GetString(result);
        
        // Prompts should not have line endings (sent separately)
        resultString.Should().NotEndWith("\r\n");
        
        // Color codes should be processed
        resultString.Should().Contain("\x1B[");
        resultString.Should().NotContain("&W");
        resultString.Should().NotContain("&R");
    }

    #endregion

    #region Helper Methods

    private IConnectionDescriptor CreateMockConnection(string clientType, string hostname)
    {
        var mock = new Mock<IConnectionDescriptor>();
        mock.Setup(c => c.Id).Returns($"{clientType}-{Guid.NewGuid():N}");
        mock.Setup(c => c.Host).Returns(hostname);
        mock.Setup(c => c.EchoEnabled).Returns(true);
        mock.Setup(c => c.IsConnected).Returns(true);
        return mock.Object;
    }

    #endregion
}

/// <summary>
/// Protocol-specific tests for telnet option negotiation with different client types
/// </summary>
public class TelnetProtocolNegotiationTests
{
    private readonly ITelnetProtocolHandler _telnetHandler;

    public TelnetProtocolNegotiationTests()
    {
        _telnetHandler = new TelnetProtocolHandler();
    }

    [Fact]
    public void SuppressGoAhead_ShouldBeNegotiatedProperly()
    {
        // Original MUDs always negotiated Suppress Go Ahead for better performance
        var mockConnection = new Mock<IConnectionDescriptor>();
        mockConnection.Setup(c => c.Host).Returns("client.example.com");
        
        var doSuppressGoAhead = new byte[] 
        { 
            TelnetConstants.IAC, TelnetConstants.DO, TelnetConstants.SUPPRESS_GO_AHEAD 
        };
        
        var response = _telnetHandler.HandleNegotiation(doSuppressGoAhead, mockConnection.Object);
        
        response.Should().Equal(new byte[] 
        { 
            TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.SUPPRESS_GO_AHEAD 
        });
    }

    [Fact]
    public void WindowSize_ShouldBeNegotiatedForCompatibleClients()
    {
        // Some advanced clients send window size information
        var mockConnection = new Mock<IConnectionDescriptor>();
        mockConnection.Setup(c => c.Host).Returns("client.example.com");
        
        var willNAWS = new byte[] 
        { 
            TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.NAWS 
        };
        
        var response = _telnetHandler.HandleNegotiation(willNAWS, mockConnection.Object);
        
        response.Should().Equal(new byte[] 
        { 
            TelnetConstants.IAC, TelnetConstants.DO, TelnetConstants.NAWS 
        });
    }

    [Fact]
    public void UnknownOptions_ShouldBeDeclinedGracefully()
    {
        // Clients might try to negotiate unknown options
        var mockConnection = new Mock<IConnectionDescriptor>();
        mockConnection.Setup(c => c.Host).Returns("client.example.com");
        
        const byte unknownOption = 99;
        var willUnknown = new byte[] 
        { 
            TelnetConstants.IAC, TelnetConstants.WILL, unknownOption 
        };
        
        var response = _telnetHandler.HandleNegotiation(willUnknown, mockConnection.Object);
        
        response.Should().Equal(new byte[] 
        { 
            TelnetConstants.IAC, TelnetConstants.DONT, unknownOption 
        });
    }
}