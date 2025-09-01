using System.Text;

namespace C3Mud.Core.Commands;

/// <summary>
/// Command parsing utilities based on original parser.c functions
/// Handles command line parsing and argument extraction
/// </summary>
public static class CommandParser
{
    /// <summary>
    /// Parse a command line into command and arguments
    /// Based on original argument_interpreter() function
    /// </summary>
    /// <param name="input">Raw input from player</param>
    /// <returns>Tuple of (command, arguments)</returns>
    public static (string Command, string Arguments) ParseCommandLine(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (string.Empty, string.Empty);

        input = input.Trim();
        
        var spaceIndex = input.IndexOf(' ');
        if (spaceIndex == -1)
        {
            return (input.ToLowerInvariant(), string.Empty);
        }

        var command = input[..spaceIndex].ToLowerInvariant();
        var arguments = input[(spaceIndex + 1)..].Trim();
        
        return (command, arguments);
    }

    /// <summary>
    /// Extract one argument from an argument string
    /// Based on original one_argument() function
    /// </summary>
    /// <param name="arguments">Argument string</param>
    /// <returns>Tuple of (firstArgument, remainingArguments)</returns>
    public static (string FirstArgument, string RemainingArguments) OneArgument(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            return (string.Empty, string.Empty);

        arguments = arguments.Trim();
        
        // Handle quoted arguments
        if (arguments.StartsWith('"'))
        {
            var endQuote = arguments.IndexOf('"', 1);
            if (endQuote != -1)
            {
                var quotedArg = arguments[1..endQuote];
                var remaining = arguments.Length > endQuote + 1 ? arguments[(endQuote + 1)..].Trim() : string.Empty;
                return (quotedArg, remaining);
            }
        }

        var spaceIndex = arguments.IndexOf(' ');
        if (spaceIndex == -1)
        {
            return (arguments, string.Empty);
        }

        var firstArg = arguments[..spaceIndex];
        var remainingArgs = arguments[(spaceIndex + 1)..].Trim();
        
        return (firstArg, remainingArgs);
    }

    /// <summary>
    /// Split argument string into two parts at first space
    /// Based on original half_chop() function
    /// </summary>
    /// <param name="input">Input string</param>
    /// <returns>Tuple of (firstHalf, secondHalf)</returns>
    public static (string FirstHalf, string SecondHalf) HalfChop(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (string.Empty, string.Empty);

        var trimmed = input.Trim();
        var spaceIndex = trimmed.IndexOf(' ');
        
        if (spaceIndex == -1)
            return (trimmed, string.Empty);
            
        return (trimmed[..spaceIndex], trimmed[(spaceIndex + 1)..].TrimStart());
    }

    /// <summary>
    /// Check if a string is an abbreviation of another string
    /// Based on original is_abbrev() function
    /// </summary>
    /// <param name="abbreviation">Potential abbreviation</param>
    /// <param name="fullString">Full string to match against</param>
    /// <returns>True if abbreviation matches</returns>
    public static bool IsAbbreviation(string abbreviation, string fullString)
    {
        if (string.IsNullOrEmpty(abbreviation) || string.IsNullOrEmpty(fullString))
            return false;

        if (abbreviation.Length > fullString.Length)
            return false;

        return fullString.StartsWith(abbreviation, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if a string contains only numbers
    /// Based on original is_number() function
    /// </summary>
    /// <param name="str">String to check</param>
    /// <returns>True if string is numeric</returns>
    public static bool IsNumber(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return false;

        return str.All(c => char.IsDigit(c) || c == '-' || c == '+');
    }

    /// <summary>
    /// Search for a string in a list with optional exact matching
    /// Based on original search_block() function
    /// </summary>
    /// <param name="argument">String to search for</param>
    /// <param name="list">List of strings to search in</param>
    /// <param name="exact">Whether to require exact match</param>
    /// <returns>Index of match, or -1 if not found</returns>
    public static int SearchBlock(string argument, string[] list, bool exact = false)
    {
        if (string.IsNullOrEmpty(argument) || list == null || list.Length == 0)
            return -1;

        for (int i = 0; i < list.Length; i++)
        {
            if (exact)
            {
                if (string.Equals(argument, list[i], StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            else
            {
                if (IsAbbreviation(argument, list[i]))
                    return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Check if an argument is a "fill word" that should be ignored
    /// Based on original fill_word() function
    /// Common fill words: "a", "an", "the", "at", "in", "on", etc.
    /// </summary>
    /// <param name="argument">Argument to check</param>
    /// <returns>True if argument is a fill word</returns>
    public static bool IsFillWord(string argument)
    {
        if (string.IsNullOrEmpty(argument))
            return false;

        var fillWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "at", "in", "on", "with", "by", "for", "of", "to", "from"
        };

        return fillWords.Contains(argument);
    }
}