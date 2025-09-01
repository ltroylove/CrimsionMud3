using C3Mud.Core.Networking;
using FluentAssertions;
using Moq;
using Xunit;
using System.Text;

namespace C3Mud.Tests.Networking;

/// <summary>
/// Legacy compatibility validation tests ensuring our C# telnet implementation 
/// matches the exact behavior of the original Crimson-2-MUD C implementation
/// </summary>
public class LegacyCompatibilityValidationTests
{
    private readonly ITelnetProtocolHandler _telnetHandler;
    private readonly Mock<IConnectionDescriptor> _mockConnection;

    public LegacyCompatibilityValidationTests()
    {
        _telnetHandler = new TelnetProtocolHandler();
        _mockConnection = new Mock<IConnectionDescriptor>();
        _mockConnection.Setup(c => c.Id).Returns("legacy-test-connection");
        _mockConnection.Setup(c => c.Host).Returns("127.0.0.1");
        _mockConnection.Setup(c => c.EchoEnabled).Returns(true);
    }

    #region Telnet Protocol Constants Validation

    [Fact]
    public void TelnetConstants_ShouldMatchOriginalMudProtocolValues()
    {
        // Validate against original C MUD protocol constants
        TelnetConstants.IAC.Should().Be(255, "IAC (Interpret As Command) must be 255");
        TelnetConstants.WILL.Should().Be(251, "WILL command must be 251");
        TelnetConstants.WONT.Should().Be(252, "WONT command must be 252");
        TelnetConstants.DO.Should().Be(253, "DO command must be 253");
        TelnetConstants.DONT.Should().Be(254, "DONT command must be 254");
        
        // Telnet options used by original MUD
        TelnetConstants.ECHO.Should().Be(1, "ECHO option must be 1");
        TelnetConstants.SUPPRESS_GO_AHEAD.Should().Be(3, "Suppress Go Ahead must be 3");
        TelnetConstants.TERMINAL_TYPE.Should().Be(24, "Terminal Type must be 24");
        TelnetConstants.NAWS.Should().Be(31, "NAWS (window size) must be 31");
    }

    [Fact]
    public void AnsiColorConstants_ShouldMatchOriginalMudDefinitions()
    {
        // Validate ANSI codes match original ansi.h definitions
        // Original: BLACK	"[0;30m", RED "[0;31m", etc.
        TelnetConstants.ANSI_RED.Should().Be("\x1B[31m", "Red ANSI code must match original");
        TelnetConstants.ANSI_GREEN.Should().Be("\x1B[32m", "Green ANSI code must match original");
        TelnetConstants.ANSI_YELLOW.Should().Be("\x1B[33m", "Yellow ANSI code must match original");
        TelnetConstants.ANSI_BLUE.Should().Be("\x1B[34m", "Blue ANSI code must match original");
        TelnetConstants.ANSI_MAGENTA.Should().Be("\x1B[35m", "Magenta ANSI code must match original");
        TelnetConstants.ANSI_CYAN.Should().Be("\x1B[36m", "Cyan ANSI code must match original");
        TelnetConstants.ANSI_WHITE.Should().Be("\x1B[37m", "White ANSI code must match original");
        TelnetConstants.ANSI_NORMAL.Should().Be("\x1B[0m", "Normal/END code must match original");
    }

    #endregion

    #region Original Color Code Processing Validation

    [Theory]
    [InlineData("&k", "\x1B[0;30m")]  // BLACK - Original: BLACK "[0;30m"
    [InlineData("&r", "\x1B[0;31m")]  // RED - Original: RED "[0;31m"
    [InlineData("&g", "\x1B[0;32m")]  // GREEN - Original: GREEN "[0;32m"
    [InlineData("&y", "\x1B[0;33m")]  // BROWN - Original: BROWN "[0;33m"
    [InlineData("&b", "\x1B[0;34m")]  // BLUE - Original: BLUE "[0;34m"
    [InlineData("&p", "\x1B[0;35m")]  // PURPLE - Original: PURPLE "[0;35m"
    [InlineData("&c", "\x1B[0;36m")]  // CYAN - Original: CYAN "[0;36m"
    [InlineData("&K", "\x1B[1;30m")]  // DGRAY - Original: DGRAY "[1;30m"
    [InlineData("&R", "\x1B[1;31m")]  // LRED - Original: LRED "[1;31m"
    [InlineData("&G", "\x1B[1;32m")]  // LGREEN - Original: LGREEN "[1;32m"
    [InlineData("&Y", "\x1B[1;33m")]  // YELLOW - Original: YELLOW "[1;33m"
    [InlineData("&B", "\x1B[1;34m")]  // LBLUE - Original: LBLUE "[1;34m"
    [InlineData("&P", "\x1B[1;35m")]  // LPURPLE - Original: LPURPLE "[1;35m"
    [InlineData("&C", "\x1B[1;36m")]  // LCYAN - Original: LCYAN "[1;36m"
    [InlineData("&W", "\x1B[1;37m")]  // WHITE - Original: WHITE "[1;37m"
    [InlineData("&w", "\x1B[0;37m")]  // GRAY - Original: GRAY "[0;37m"
    [InlineData("&E", "\x1B[0m")]     // END - Original: END "[0m"
    public void ProcessColorCodes_ShouldMatchOriginalCColorCodeMappings(string mudCode, string expectedAnsi)
    {
        // This test validates against the original CCODE and ANSI arrays from comm.c:
        // const char CCODE[] = "&krgybpcKRGYBPCWwfuv01234567^E!";
        // const char *ANSI[] = {"&", BLACK, RED, GREEN, BROWN, BLUE, PURPLE, CYAN, DGRAY, LRED, LGREEN, YELLOW,
        //    LBLUE, LPURPLE, LCYAN, WHITE, GRAY, BLINK, UNDERL, INVERSE, BACK_BLACK, BACK_RED,
        //    BACK_GREEN, BACK_BROWN, BACK_BLUE, BACK_PURPLE, BACK_CYAN, BACK_GRAY, NEWLINE, END, "!"};

        // Arrange
        var text = $"Test {mudCode}colored{mudCode} text";

        // Act - Our current implementation only handles basic colors, need to extend
        var result = _telnetHandler.ProcessColorCodes(text, _mockConnection.Object);

        // Assert - This will initially fail until we implement full legacy color support
        result.Should().Contain(expectedAnsi, 
            $"MUD color code '{mudCode}' should be converted to ANSI '{expectedAnsi}' to match original C implementation");
    }

    [Fact]
    public void ProcessColorCodes_WithNestedColors_ShouldHandleSequenceCorrectly()
    {
        // Test complex color sequences like in original MUD
        // Example from original: "&W[&R***&Y***&G***&W] &cPlayerName"
        var text = "&W[&R***&Y***&G***&W] &cPlayerName";
        
        var result = _telnetHandler.ProcessColorCodes(text, _mockConnection.Object);
        
        // Should process all color codes without corruption
        result.Should().NotContain("&W");
        result.Should().NotContain("&R");
        result.Should().NotContain("&Y");
        result.Should().NotContain("&G");
        result.Should().NotContain("&c");
    }

    #endregion

    #region Telnet Negotiation Sequence Validation

    [Fact]
    public void GetInitialNegotiation_ShouldMatchOriginalMudSequence()
    {
        // Act
        var negotiation = _telnetHandler.GetInitialNegotiation();
        
        // Assert - Should match original MUD's initial telnet handshake
        negotiation.Should().NotBeNull();
        negotiation.Should().HaveCountGreaterThan(0);
        
        // Should include: IAC WILL SUPPRESS_GO_AHEAD, IAC WONT ECHO (standard MUD)
        var sequence = negotiation.ToList();
        
        // Check for Suppress Go Ahead
        var sgaIndex = FindTelnetOption(sequence, TelnetConstants.WILL, TelnetConstants.SUPPRESS_GO_AHEAD);
        sgaIndex.Should().BeGreaterOrEqualTo(0, "Should negotiate Suppress Go Ahead like original MUD");
        
        // Check for Echo handling
        var echoIndex = FindTelnetOption(sequence, TelnetConstants.WONT, TelnetConstants.ECHO);
        echoIndex.Should().BeGreaterOrEqualTo(0, "Should handle echo like original MUD");
    }

    [Theory]
    [InlineData(TelnetConstants.DO, TelnetConstants.ECHO, TelnetConstants.WILL, TelnetConstants.ECHO)]
    [InlineData(TelnetConstants.DO, TelnetConstants.SUPPRESS_GO_AHEAD, TelnetConstants.WILL, TelnetConstants.SUPPRESS_GO_AHEAD)]
    [InlineData(TelnetConstants.DONT, TelnetConstants.ECHO, TelnetConstants.WONT, TelnetConstants.ECHO)]
    public void HandleNegotiation_ShouldRespondLikeOriginalMud(
        byte incomingCommand, byte incomingOption, 
        byte expectedResponseCommand, byte expectedResponseOption)
    {
        // Test telnet negotiation responses match original behavior
        var negotiationData = new byte[] { TelnetConstants.IAC, incomingCommand, incomingOption };
        
        var response = _telnetHandler.HandleNegotiation(negotiationData, _mockConnection.Object);
        
        response.Should().NotBeNull();
        response.Should().Equal(new byte[] { TelnetConstants.IAC, expectedResponseCommand, expectedResponseOption });
    }

    #endregion

    #region Connection State and Echo Control Validation

    [Fact]
    public void SetEcho_ShouldFollowOriginalMudEchoProtocol()
    {
        // Original MUD echo behavior for password input:
        // - Disable echo: Server sends IAC WILL ECHO (server will echo)
        // - Enable echo: Server sends IAC WONT ECHO (server won't echo, client will)
        
        var disableEcho = _telnetHandler.SetEcho(false);  // For password input
        disableEcho.Should().Equal(new byte[] { TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.ECHO });
        
        var enableEcho = _telnetHandler.SetEcho(true);   // For normal input
        enableEcho.Should().Equal(new byte[] { TelnetConstants.IAC, TelnetConstants.WONT, TelnetConstants.ECHO });
    }

    #endregion

    #region Data Processing and Buffer Handling

    [Fact]
    public void ProcessIncomingData_WithTelnetCommandSequence_ShouldExtractCleanly()
    {
        // Test processing data with embedded telnet commands (like original MUD handles)
        var inputBytes = new List<byte>();
        inputBytes.AddRange(Encoding.UTF8.GetBytes("north"));
        inputBytes.AddRange(new byte[] { TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.TERMINAL_TYPE });
        inputBytes.AddRange(Encoding.UTF8.GetBytes("\r\n"));
        
        var result = _telnetHandler.ProcessIncomingData(inputBytes.ToArray(), _mockConnection.Object);
        
        result.Text.Should().Be("north");
        result.IsComplete.Should().BeTrue();
        result.NegotiationResponse.Should().NotBeNull();
    }

    [Fact]
    public void ProcessOutgoingData_ShouldHandleLineEndingsLikeOriginal()
    {
        // Original MUD uses \r\n for telnet compatibility
        var text = "You are standing in a room.\nExits: north, south";
        
        var result = _telnetHandler.ProcessOutgoingData(text, _mockConnection.Object);
        var resultString = Encoding.UTF8.GetString(result);
        
        // Should convert \n to \r\n but not double-convert
        resultString.Should().Contain("\r\n");
        resultString.Should().NotContain("\r\r\n");
    }

    [Theory]
    [InlineData("say Hello world\r\n", "say Hello world", true)]
    [InlineData("say Hello world\n", "say Hello world", true)]
    [InlineData("say Hello world\r", "say Hello world", true)]
    [InlineData("say Hello world", "say Hello world", false)]
    public void ProcessIncomingData_WithVariousLineEndings_ShouldHandleLikeOriginal(
        string input, string expectedText, bool expectedComplete)
    {
        // Original MUD handled various line ending styles from different clients
        var inputBytes = Encoding.UTF8.GetBytes(input);
        
        var result = _telnetHandler.ProcessIncomingData(inputBytes, _mockConnection.Object);
        
        result.Text.Should().Be(expectedText);
        result.IsComplete.Should().Be(expectedComplete);
    }

    #endregion

    #region Performance and Binary Data Handling

    [Fact]
    public void ProcessIncomingData_WithLargeBuffer_ShouldHandleEfficientlyLikeOriginal()
    {
        // Original MUD had buffer limits (MAX_RAW_INPUT_LENGTH = 2048)
        // Test our implementation handles similar data volumes efficiently
        
        var largeCommand = new string('a', 2000) + "\r\n";
        var inputBytes = Encoding.UTF8.GetBytes(largeCommand);
        
        var result = _telnetHandler.ProcessIncomingData(inputBytes, _mockConnection.Object);
        
        result.Should().NotBeNull();
        result.Text.Length.Should().Be(2000);  // Should handle up to original limits
        result.IsComplete.Should().BeTrue();
        result.ShouldClose.Should().BeFalse();
    }

    [Fact]
    public void ProcessIncomingData_WithBinaryData_ShouldNotCorruptLikeOriginal()
    {
        // Original MUD needed to handle occasional binary data gracefully
        var binaryData = new byte[] { 0, 1, 2, 255, 254, 253, (byte)'h', (byte)'i', (byte)'\r', (byte)'\n' };
        
        var result = _telnetHandler.ProcessIncomingData(binaryData, _mockConnection.Object);
        
        result.Should().NotBeNull();
        result.ShouldClose.Should().BeFalse();  // Should not crash or close connection
    }

    #endregion

    #region Helper Methods

    private int FindTelnetOption(List<byte> sequence, byte command, byte option)
    {
        for (int i = 0; i < sequence.Count - 2; i++)
        {
            if (sequence[i] == TelnetConstants.IAC && 
                sequence[i + 1] == command && 
                sequence[i + 2] == option)
            {
                return i;
            }
        }
        return -1;
    }

    #endregion
}

/// <summary>
/// Extended validation tests for complete original MUD color code compatibility
/// </summary>
public class LegacyColorCodeValidationTests
{
    [Fact]
    public void OriginalColorCodeMappings_ShouldBeDocumented()
    {
        // This documents the complete original color code mappings from comm.c
        var originalMappings = new Dictionary<char, string>
        {
            // From original CCODE: "&krgybpcKRGYBPCWwfuv01234567^E!"
            // From original ANSI array indices:
            ['k'] = "[0;30m",    // BLACK
            ['r'] = "[0;31m",    // RED
            ['g'] = "[0;32m",    // GREEN
            ['y'] = "[0;33m",    // BROWN (yellow)
            ['b'] = "[0;34m",    // BLUE
            ['p'] = "[0;35m",    // PURPLE
            ['c'] = "[0;36m",    // CYAN
            ['K'] = "[1;30m",    // DGRAY (dark gray)
            ['R'] = "[1;31m",    // LRED (light red)
            ['G'] = "[1;32m",    // LGREEN (light green)
            ['Y'] = "[1;33m",    // YELLOW (bright yellow)
            ['B'] = "[1;34m",    // LBLUE (light blue)
            ['P'] = "[1;35m",    // LPURPLE (light purple)
            ['C'] = "[1;36m",    // LCYAN (light cyan)
            ['W'] = "[1;37m",    // WHITE (bright white)
            ['w'] = "[0;37m",    // GRAY (normal white)
            ['f'] = "[5m",       // BLINK
            ['u'] = "[4m",       // UNDERL (underline)
            ['v'] = "[7m",       // INVERSE
            ['0'] = "[40m",      // BACK_BLACK
            ['1'] = "[41m",      // BACK_RED
            ['2'] = "[42m",      // BACK_GREEN
            ['3'] = "[43m",      // BACK_BROWN
            ['4'] = "[44m",      // BACK_BLUE
            ['5'] = "[45m",      // BACK_PURPLE
            ['6'] = "[46m",      // BACK_CYAN
            ['7'] = "[47m",      // BACK_GRAY
            ['^'] = "\r\n",      // NEWLINE
            ['E'] = "[0m",       // END (reset)
        };

        // This serves as reference for implementing complete compatibility
        originalMappings.Should().NotBeEmpty();
        originalMappings.Should().HaveCount(30); // 30 different codes in original
    }
}