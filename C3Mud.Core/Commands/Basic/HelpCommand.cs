using C3Mud.Core.Players;

namespace C3Mud.Core.Commands.Basic;

/// <summary>
/// Help command - provides help information for commands and topics
/// Based on original do_help() function
/// </summary>
public class HelpCommand : BaseCommand
{
    public override string Name => "help";
    public override string[] Aliases => Array.Empty<string>();
    public override PlayerPosition MinimumPosition => PlayerPosition.Sleeping;
    public override int MinimumLevel => 1;

    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            await ShowGeneralHelp(player);
        }
        else
        {
            await ShowSpecificHelp(player, arguments.Trim());
        }
    }

    private static async Task ShowGeneralHelp(IPlayer player)
    {
        var generalHelp = @"&W
C3MUD Help System
=================

&YBasic Commands:&N
  &Wlook&N (l)     - Look at your surroundings
  &Wquit&N (q)     - Leave the game
  &Wwho&N          - See who else is online
  &Wscore&N (sc)   - View your character statistics
  &Whelp&N <topic> - Get help on a specific topic

&YCommunication:&N
  &Wsay&N <message> - Say something to others in the room

&YMovement:&N
  &Wnorth&N, &Wsouth&N, &Weast&N, &Wwest&N, &Wup&N, &Wdown&N
  (Movement commands - world system coming soon)

&YFor more specific help, type: &Whelp <command name>&N
&YExample: help look&N

&KNote: This is an early development version of C3MUD.
Many features are still being implemented.&N";

        await SendToPlayerAsync(player, generalHelp, formatted: true);
    }

    private static async Task ShowSpecificHelp(IPlayer player, string topic)
    {
        var helpText = topic.ToLowerInvariant() switch
        {
            "look" or "l" => GetLookHelp(),
            "quit" or "q" => GetQuitHelp(),
            "who" => GetWhoHelp(),
            "score" or "sc" or "stat" => GetScoreHelp(),
            "say" => GetSayHelp(),
            "help" => GetHelpHelp(),
            _ => GetNoHelpFound(topic)
        };

        await SendToPlayerAsync(player, helpText, formatted: true);
    }

    private static string GetLookHelp()
    {
        return @"&W
LOOK - Examine your surroundings

&YSyntax:&N
  look              - Look at the current room
  look <target>     - Look at a specific object or person

&YExamples:&N
  look              - Shows room description and contents
  look sword        - Examine a sword (when items are implemented)
  look bob          - Look at player named Bob

&YAliases:&N l

The look command is your primary way of gathering information about
your environment in the MUD.&N";
    }

    private static string GetQuitHelp()
    {
        return @"&W
QUIT - Leave the game

&YSyntax:&N
  quit              - Disconnect from the game

&YAliases:&N q

The quit command safely disconnects you from the game. Your character
will be saved automatically (when the save system is implemented).

&RNote:&N You cannot quit while fighting!&N";
    }

    private static string GetWhoHelp()
    {
        return @"&W
WHO - See who is online

&YSyntax:&N
  who               - List all players currently online

The who command shows you information about all players currently
connected to the game, including their level and name.&N";
    }

    private static string GetScoreHelp()
    {
        return @"&W
SCORE - View character statistics

&YSyntax:&N
  score             - Show your character's current status

&YAliases:&N sc, stat

The score command displays detailed information about your character
including level, health, mana, experience, and other vital statistics.&N";
    }

    private static string GetSayHelp()
    {
        return @"&W
SAY - Speak to others in your room

&YSyntax:&N
  say <message>     - Say something to everyone in the room

&YExamples:&N
  say Hello everyone!
  say How is everyone doing?

The say command allows you to communicate with other players in the
same room as you.&N";
    }

    private static string GetHelpHelp()
    {
        return @"&W
HELP - Get help information

&YSyntax:&N
  help              - Show general help
  help <topic>      - Get help on specific topic

&YExamples:&N
  help              - General command list
  help look         - Specific help for the look command
  help combat       - Combat help (when implemented)

The help system provides information about commands, game mechanics,
and other topics to help you play the game.&N";
    }

    private static string GetNoHelpFound(string topic)
    {
        return $@"&W
No help available for '{topic}'.

&YTry:&N
  &Whelp&N          - General help and command list
  &Whelp look&N     - Help for the look command
  &Whelp quit&N     - Help for the quit command
  &Whelp who&N      - Help for the who command
  &Whelp score&N    - Help for the score command

&KMore help topics will be added as the game develops.&N";
    }
}