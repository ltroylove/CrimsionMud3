namespace C3Mud.Core.Networking;

/// <summary>
/// Handles telnet protocol negotiations and processing, maintaining compatibility with classic MUD clients
/// </summary>
public interface ITelnetProtocolHandler
{
    /// <summary>
    /// Process raw data from client and extract command/text
    /// </summary>
    TelnetProcessResult ProcessIncomingData(byte[] data, IConnectionDescriptor connection);
    
    /// <summary>
    /// Process outgoing data and add necessary telnet sequences
    /// </summary>
    byte[] ProcessOutgoingData(string data, IConnectionDescriptor connection);
    
    /// <summary>
    /// Handle telnet negotiation sequence
    /// </summary>
    byte[]? HandleNegotiation(byte[] negotiationData, IConnectionDescriptor connection);
    
    /// <summary>
    /// Convert ANSI color codes to appropriate format for client
    /// </summary>
    string ProcessColorCodes(string text, IConnectionDescriptor connection);
    
    /// <summary>
    /// Enable/disable echo for password input
    /// </summary>
    byte[] SetEcho(bool enabled);
    
    /// <summary>
    /// Get initial telnet negotiation sequence to send to new clients
    /// </summary>
    byte[] GetInitialNegotiation();
}

public class TelnetProcessResult
{
    /// <summary>
    /// Extracted text/command from telnet data
    /// </summary>
    public string? Text { get; set; }
    
    /// <summary>
    /// Whether this is a complete command (ended with newline)
    /// </summary>
    public bool IsComplete { get; set; }
    
    /// <summary>
    /// Any telnet negotiation response needed
    /// </summary>
    public byte[]? NegotiationResponse { get; set; }
    
    /// <summary>
    /// Whether the connection should be closed
    /// </summary>
    public bool ShouldClose { get; set; }
    
    /// <summary>
    /// Whether echo state changed
    /// </summary>
    public bool? EchoStateChanged { get; set; }
}

/// <summary>
/// Telnet protocol constants matching original MUD behavior
/// </summary>
public static class TelnetConstants
{
    // Telnet commands
    public const byte IAC = 255;  // Interpret As Command
    public const byte WILL = 251;
    public const byte WONT = 252;
    public const byte DO = 253;
    public const byte DONT = 254;
    
    // Telnet options
    public const byte ECHO = 1;
    public const byte SUPPRESS_GO_AHEAD = 3;
    public const byte TERMINAL_TYPE = 24;
    public const byte NAWS = 31;  // Negotiate About Window Size
    
    // ANSI color codes (matching original MUD exactly from ansi.h)
    public const string ANSI_NORMAL = "\x1B[0m";           // END
    public const string ANSI_BOLD = "\x1B[1m";             // BOLD
    
    // Basic colors (lowercase MUD codes) - from original CCODE mapping
    public const string ANSI_RED = "\x1B[0;31m";           // &r → RED
    public const string ANSI_GREEN = "\x1B[0;32m";         // &g → GREEN  
    public const string ANSI_YELLOW = "\x1B[0;33m";        // &y → BROWN
    public const string ANSI_BLUE = "\x1B[0;34m";          // &b → BLUE
    public const string ANSI_MAGENTA = "\x1B[0;35m";       // &p → PURPLE
    public const string ANSI_CYAN = "\x1B[0;36m";          // &c → CYAN
    public const string ANSI_WHITE = "\x1B[0;37m";         // &w → GRAY
    
    // Bright colors (uppercase MUD codes) - from original CCODE mapping  
    public const string ANSI_BRIGHT_RED = "\x1B[1;31m";    // &R → LRED
    public const string ANSI_BRIGHT_GREEN = "\x1B[1;32m";  // &G → LGREEN
    public const string ANSI_BRIGHT_YELLOW = "\x1B[1;33m"; // &Y → YELLOW
    public const string ANSI_BRIGHT_BLUE = "\x1B[1;34m";   // &B → LBLUE
    public const string ANSI_BRIGHT_MAGENTA = "\x1B[1;35m";// &P → LPURPLE
    public const string ANSI_BRIGHT_CYAN = "\x1B[1;36m";   // &C → LCYAN
    public const string ANSI_BRIGHT_WHITE = "\x1B[1;37m";  // &W → WHITE
}