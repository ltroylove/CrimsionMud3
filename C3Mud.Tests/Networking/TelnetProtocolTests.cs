using C3Mud.Core.Networking;
using FluentAssertions;
using Moq;
using Xunit;

namespace C3Mud.Tests.Networking;

/// <summary>
/// Tests for Telnet protocol compatibility matching original MUD behavior
/// These tests should all FAIL initially since we haven't implemented telnet handling
/// </summary>
public class TelnetProtocolTests
{
    private ITelnetProtocolHandler CreateTelnetHandler()
    {
        return new C3Mud.Core.Networking.TelnetProtocolHandler();
    }

    private Mock<IConnectionDescriptor> CreateMockConnection()
    {
        var mock = new Mock<IConnectionDescriptor>();
        mock.Setup(c => c.Id).Returns("test-connection-1");
        mock.Setup(c => c.Host).Returns("127.0.0.1");
        mock.Setup(c => c.EchoEnabled).Returns(true);
        return mock;
    }

    [Fact]
    public void ProcessIncomingData_WithSimpleText_ShouldExtractCleanText()
    {
        // Arrange
        var handler = CreateTelnetHandler();
        var connection = CreateMockConnection().Object;
        var inputData = "say hello world\r\n"u8.ToArray();

        // Act
        var result = handler.ProcessIncomingData(inputData, connection);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be("say hello world");
        result.IsComplete.Should().BeTrue();
        result.ShouldClose.Should().BeFalse();
    }

    [Fact]
    public void ProcessIncomingData_WithTelnetNegotiation_ShouldExtractTextAndHandleNegotiation()
    {
        // Arrange
        var handler = CreateTelnetHandler();
        var connection = CreateMockConnection().Object;
        // Simulate: "look" with embedded telnet negotiation
        var inputData = new byte[] { 
            // "look" 
            (byte)'l', (byte)'o', (byte)'o', (byte)'k',
            // Telnet: IAC WILL ECHO
            TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.ECHO,
            // "\r\n"
            (byte)'\r', (byte)'\n' 
        };

        // Act
        var result = handler.ProcessIncomingData(inputData, connection);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be("look");
        result.IsComplete.Should().BeTrue();
        result.NegotiationResponse.Should().NotBeNull();
        result.ShouldClose.Should().BeFalse();
    }

    [Fact]
    public void ProcessIncomingData_WithIncompleteCommand_ShouldReturnIncompleteResult()
    {
        // Arrange
        var handler = CreateTelnetHandler();
        var connection = CreateMockConnection().Object;
        var inputData = "look nort"u8.ToArray(); // No newline

        // Act
        var result = handler.ProcessIncomingData(inputData, connection);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be("look nort");
        result.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void HandleNegotiation_WithEchoRequest_ShouldReturnAppropriateResponse()
    {
        // Arrange
        var handler = CreateTelnetHandler();
        var connection = CreateMockConnection().Object;
        var negotiationData = new byte[] { TelnetConstants.IAC, TelnetConstants.DO, TelnetConstants.ECHO };

        // Act
        var response = handler.HandleNegotiation(negotiationData, connection);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain(TelnetConstants.IAC);
        // Should respond with WILL ECHO or WONT ECHO based on MUD convention
    }

    [Fact]
    public void GetInitialNegotiation_ShouldReturnMudCompatibleSequence()
    {
        // Arrange
        var handler = CreateTelnetHandler();

        // Act
        var negotiation = handler.GetInitialNegotiation();

        // Assert
        negotiation.Should().NotBeNull();
        negotiation.Should().NotBeEmpty();
        // Should include standard MUD telnet options like suppress go-ahead
        negotiation.Should().Contain(TelnetConstants.IAC);
        negotiation.Should().Contain(TelnetConstants.SUPPRESS_GO_AHEAD);
    }

    [Fact]
    public void SetEcho_WithEnabled_ShouldReturnEchoOnSequence()
    {
        // Arrange
        var handler = CreateTelnetHandler();

        // Act
        var sequence = handler.SetEcho(true);

        // Assert
        sequence.Should().NotBeNull();
        sequence.Should().Equal(new byte[] { 
            TelnetConstants.IAC, TelnetConstants.WONT, TelnetConstants.ECHO 
        });
    }

    [Fact]
    public void SetEcho_WithDisabled_ShouldReturnEchoOffSequence()
    {
        // Arrange
        var handler = CreateTelnetHandler();

        // Act
        var sequence = handler.SetEcho(false);

        // Assert
        sequence.Should().NotBeNull();
        sequence.Should().Equal(new byte[] { 
            TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.ECHO 
        });
    }

    [Theory]
    [InlineData("&r", TelnetConstants.ANSI_RED)]            // &r → RED (basic red)
    [InlineData("&g", TelnetConstants.ANSI_GREEN)]          // &g → GREEN (basic green)
    [InlineData("&y", TelnetConstants.ANSI_YELLOW)]         // &y → BROWN (basic yellow)
    [InlineData("&b", TelnetConstants.ANSI_BLUE)]           // &b → BLUE (basic blue)
    [InlineData("&R", TelnetConstants.ANSI_BRIGHT_RED)]     // &R → LRED (bright red)
    [InlineData("&G", TelnetConstants.ANSI_BRIGHT_GREEN)]   // &G → LGREEN (bright green)  
    [InlineData("&Y", TelnetConstants.ANSI_BRIGHT_YELLOW)]  // &Y → YELLOW (bright yellow)
    [InlineData("&B", TelnetConstants.ANSI_BRIGHT_BLUE)]    // &B → LBLUE (bright blue)
    [InlineData("&N", TelnetConstants.ANSI_NORMAL)]         // &N → END (reset)
    public void ProcessColorCodes_ShouldConvertMudColorsToAnsi(string mudColor, string expectedAnsi)
    {
        // Arrange
        var handler = CreateTelnetHandler();
        var connection = CreateMockConnection().Object;
        var text = $"This is {mudColor}colored{mudColor} text";

        // Act
        var result = handler.ProcessColorCodes(text, connection);

        // Assert
        result.Should().Contain(expectedAnsi);
        result.Should().NotContain(mudColor); // Original codes should be replaced
    }

    [Fact]
    public void ProcessColorCodes_WithNoColorSupport_ShouldStripColorCodes()
    {
        // Arrange
        var handler = CreateTelnetHandler();
        var connection = CreateMockConnection().Object;
        // Simulate client that doesn't support color
        Mock.Get(connection).Setup(c => c.Host).Returns("oldclient");
        var text = "This is &Rred&N text";

        // Act
        var result = handler.ProcessColorCodes(text, connection);

        // Assert
        result.Should().Be("This is red text");
        result.Should().NotContain("&R");
        result.Should().NotContain("&N");
    }

    [Fact]
    public void ProcessOutgoingData_ShouldHandleNewlinesCorrectly()
    {
        // Arrange
        var handler = CreateTelnetHandler();
        var connection = CreateMockConnection().Object;
        var text = "Line 1\nLine 2\nLine 3";

        // Act
        var result = handler.ProcessOutgoingData(text, connection);

        // Assert
        result.Should().NotBeNull();
        // Should convert \n to \r\n for telnet compatibility
        var resultString = System.Text.Encoding.UTF8.GetString(result);
        resultString.Should().Contain("\r\n");
        resultString.Should().NotContain("\n\n"); // No double conversions
    }

    [Fact]
    public void ProcessIncomingData_WithLargeBinaryData_ShouldHandleGracefully()
    {
        // Arrange
        var handler = CreateTelnetHandler();
        var connection = CreateMockConnection().Object;
        var largeData = new byte[8192]; // 8KB of data
        Random.Shared.NextBytes(largeData);

        // Act
        var result = handler.ProcessIncomingData(largeData, connection);

        // Assert
        result.Should().NotBeNull();
        // Should not crash and should handle binary data gracefully
        result.ShouldClose.Should().BeFalse();
    }

    [Fact]
    public void HandleNegotiation_WithUnknownOption_ShouldReturnRefusal()
    {
        // Arrange
        var handler = CreateTelnetHandler();
        var connection = CreateMockConnection().Object;
        var unknownOption = new byte[] { TelnetConstants.IAC, TelnetConstants.DO, 99 }; // Unknown option

        // Act
        var response = handler.HandleNegotiation(unknownOption, connection);

        // Assert
        response.Should().NotBeNull();
        response.Should().Equal(new byte[] { 
            TelnetConstants.IAC, TelnetConstants.DONT, 99 
        });
    }

    [Theory]
    [InlineData("\r\n")]     // Windows style
    [InlineData("\n")]       // Unix style
    [InlineData("\r")]       // Mac style
    public void ProcessIncomingData_WithDifferentLineEndings_ShouldHandleAll(string lineEnding)
    {
        // Arrange
        var handler = CreateTelnetHandler();
        var connection = CreateMockConnection().Object;
        var command = $"north{lineEnding}";
        var inputData = System.Text.Encoding.UTF8.GetBytes(command);

        // Act
        var result = handler.ProcessIncomingData(inputData, connection);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be("north");
        result.IsComplete.Should().BeTrue();
    }
}