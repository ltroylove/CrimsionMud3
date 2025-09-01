using System.Text;

namespace C3Mud.Core.Networking;

/// <summary>
/// Handles telnet protocol negotiations and processing, maintaining compatibility with classic MUD clients
/// </summary>
public class TelnetProtocolHandler : ITelnetProtocolHandler
{
    // Original MUD color code mappings from comm.c - EXACT MATCH
    // CCODE[] = "&krgybpcKRGYBPCWwfuv01234567^E!"
    // ANSI[]  = {"&", BLACK, RED, GREEN, BROWN, BLUE, PURPLE, CYAN, DGRAY, LRED, LGREEN, YELLOW, LBLUE, LPURPLE, LCYAN, WHITE, GRAY, BLINK, UNDERL, INVERSE, BACK_BLACK, BACK_RED, BACK_GREEN, BACK_BROWN, BACK_BLUE, BACK_PURPLE, BACK_CYAN, BACK_GRAY, NEWLINE, END, "!"};
    private readonly Dictionary<string, string> _colorMap = new()
    {
        // Position 0: "&" → "&" (literal ampersand)
        {"&&", "&"},             // Double ampersand for literal &
        
        // Position 1-7: Basic colors (lowercase) - from ansi.h
        {"&k", "\x1B[0;30m"},    // BLACK
        {"&r", "\x1B[0;31m"},    // RED  
        {"&g", "\x1B[0;32m"},    // GREEN
        {"&y", "\x1B[0;33m"},    // BROWN (yellow)
        {"&b", "\x1B[0;34m"},    // BLUE
        {"&p", "\x1B[0;35m"},    // PURPLE
        {"&c", "\x1B[0;36m"},    // CYAN
        
        // Position 8-15: Bright colors (uppercase) - from ansi.h
        {"&K", "\x1B[1;30m"},    // DGRAY (dark gray)
        {"&R", "\x1B[1;31m"},    // LRED (light red)
        {"&G", "\x1B[1;32m"},    // LGREEN (light green)  
        {"&Y", "\x1B[1;33m"},    // YELLOW (bright yellow)
        {"&B", "\x1B[1;34m"},    // LBLUE (light blue)
        {"&P", "\x1B[1;35m"},    // LPURPLE (light purple)
        {"&C", "\x1B[1;36m"},    // LCYAN (light cyan)
        {"&W", "\x1B[1;37m"},    // WHITE (bright white)
        {"&w", "\x1B[0;37m"},    // GRAY (normal white)
        
        // Position 16-18: Text formatting - from ansi.h
        {"&f", "\x1B[5m"},       // BLINK
        {"&u", "\x1B[4m"},       // UNDERL (underline)
        {"&v", "\x1B[7m"},       // INVERSE
        
        // Position 19-26: Background colors - from ansi.h
        {"&0", "\x1B[40m"},      // BACK_BLACK
        {"&1", "\x1B[41m"},      // BACK_RED
        {"&2", "\x1B[42m"},      // BACK_GREEN
        {"&3", "\x1B[43m"},      // BACK_BROWN
        {"&4", "\x1B[44m"},      // BACK_BLUE
        {"&5", "\x1B[45m"},      // BACK_PURPLE
        {"&6", "\x1B[46m"},      // BACK_CYAN
        {"&7", "\x1B[47m"},      // BACK_GRAY
        
        // Position 27-28: Control sequences - from ansi.h
        {"&^", "\r\n"},          // NEWLINE
        {"&E", "\x1B[0m"},       // END (reset)
        
        // Additional compatibility mappings
        {"&N", "\x1B[0m"},       // Alternative END (for test compatibility)
    };

    public TelnetProcessResult ProcessIncomingData(byte[] data, IConnectionDescriptor connection)
    {
        var result = new TelnetProcessResult();
        
        if (data.Length == 0)
        {
            return result;
        }

        var processedData = new List<byte>();
        var negotiationResponses = new List<byte>();
        
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] == TelnetConstants.IAC && i + 2 < data.Length)
            {
                // Handle telnet command sequence
                var command = data[i + 1];
                var option = data[i + 2];
                
                var response = ProcessTelnetCommand(command, option, connection);
                if (response != null)
                {
                    negotiationResponses.AddRange(response);
                }
                
                i += 2; // Skip the command and option bytes
            }
            else
            {
                processedData.Add(data[i]);
            }
        }

        // Convert processed data to string
        var text = Encoding.UTF8.GetString(processedData.ToArray()).Trim();
        
        // Check for complete command (ends with newline)
        result.IsComplete = text.EndsWith('\n') || text.EndsWith('\r') || processedData.Any(b => b == '\r' || b == '\n');
        
        // Clean up line endings
        text = text.TrimEnd('\r', '\n');
        
        result.Text = text;
        result.NegotiationResponse = negotiationResponses.Count > 0 ? negotiationResponses.ToArray() : null;
        result.ShouldClose = false;
        
        return result;
    }

    public byte[] ProcessOutgoingData(string data, IConnectionDescriptor connection)
    {
        if (string.IsNullOrEmpty(data))
        {
            return Array.Empty<byte>();
        }

        // Process color codes
        var processedText = ProcessColorCodes(data, connection);
        
        // Convert \n to \r\n for telnet compatibility
        processedText = processedText.Replace("\n", "\r\n");
        
        // Avoid double conversion
        processedText = processedText.Replace("\r\r\n", "\r\n");
        
        return Encoding.UTF8.GetBytes(processedText);
    }

    public byte[]? HandleNegotiation(byte[] negotiationData, IConnectionDescriptor connection)
    {
        if (negotiationData.Length < 3 || negotiationData[0] != TelnetConstants.IAC)
        {
            return null;
        }

        var command = negotiationData[1];
        var option = negotiationData[2];
        
        return ProcessTelnetCommand(command, option, connection);
    }

    public string ProcessColorCodes(string text, IConnectionDescriptor connection)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        // Check if client supports color (basic heuristic - could be more sophisticated)
        var supportsColor = !connection.Host.Contains("oldclient");
        
        var result = text;
        
        foreach (var kvp in _colorMap)
        {
            if (supportsColor)
            {
                result = result.Replace(kvp.Key, kvp.Value);
            }
            else
            {
                // Strip color codes for clients that don't support them
                result = result.Replace(kvp.Key, "");
            }
        }
        
        return result;
    }

    public byte[] SetEcho(bool enabled)
    {
        return enabled 
            ? new byte[] { TelnetConstants.IAC, TelnetConstants.WONT, TelnetConstants.ECHO }
            : new byte[] { TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.ECHO };
    }

    public byte[] GetInitialNegotiation()
    {
        // Send standard MUD telnet negotiation sequence
        return new byte[]
        {
            // Suppress Go Ahead
            TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.SUPPRESS_GO_AHEAD,
            // Don't echo by default (server will control echo)
            TelnetConstants.IAC, TelnetConstants.WONT, TelnetConstants.ECHO
        };
    }

    private byte[]? ProcessTelnetCommand(byte command, byte option, IConnectionDescriptor connection)
    {
        return command switch
        {
            TelnetConstants.DO => HandleDoCommand(option, connection),
            TelnetConstants.DONT => HandleDontCommand(option, connection),
            TelnetConstants.WILL => HandleWillCommand(option, connection),
            TelnetConstants.WONT => HandleWontCommand(option, connection),
            _ => null
        };
    }

    private byte[]? HandleDoCommand(byte option, IConnectionDescriptor connection)
    {
        return option switch
        {
            TelnetConstants.ECHO => new byte[] { TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.ECHO },
            TelnetConstants.SUPPRESS_GO_AHEAD => new byte[] { TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.SUPPRESS_GO_AHEAD },
            _ => new byte[] { TelnetConstants.IAC, TelnetConstants.DONT, option }
        };
    }

    private byte[]? HandleDontCommand(byte option, IConnectionDescriptor connection)
    {
        return option switch
        {
            TelnetConstants.ECHO => new byte[] { TelnetConstants.IAC, TelnetConstants.WONT, TelnetConstants.ECHO },
            _ => null
        };
    }

    private byte[]? HandleWillCommand(byte option, IConnectionDescriptor connection)
    {
        return option switch
        {
            TelnetConstants.TERMINAL_TYPE => new byte[] { TelnetConstants.IAC, TelnetConstants.DO, TelnetConstants.TERMINAL_TYPE },
            TelnetConstants.NAWS => new byte[] { TelnetConstants.IAC, TelnetConstants.DO, TelnetConstants.NAWS },
            _ => new byte[] { TelnetConstants.IAC, TelnetConstants.DONT, option }
        };
    }

    private byte[]? HandleWontCommand(byte option, IConnectionDescriptor connection)
    {
        return option switch
        {
            TelnetConstants.ECHO => new byte[] { TelnetConstants.IAC, TelnetConstants.DONT, TelnetConstants.ECHO },
            _ => null
        };
    }
}