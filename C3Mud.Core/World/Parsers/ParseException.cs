namespace C3Mud.Core.World.Parsers;

/// &lt;summary&gt;
/// Exception thrown when parsing world file data fails
/// &lt;/summary&gt;
public class ParseException : Exception
{
    public ParseException(string message) : base(message)
    {
    }
    
    public ParseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}