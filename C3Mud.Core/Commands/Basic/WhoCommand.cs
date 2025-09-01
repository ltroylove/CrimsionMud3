using C3Mud.Core.Players;

namespace C3Mud.Core.Commands.Basic;

/// <summary>
/// Who command - shows list of players currently online
/// Based on original do_who() function from act.informative.c
/// </summary>
public class WhoCommand : BaseCommand
{
    public override string Name => "who";
    public override string[] Aliases => Array.Empty<string>();
    public override PlayerPosition MinimumPosition => PlayerPosition.Sleeping;
    public override int MinimumLevel => 1;

    // This will need to be injected when we have a proper DI container
    // For now, we'll use a static approach
    private static readonly List<IPlayer> _onlinePlayers = new();

    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        await ShowWhoList(player);
    }

    private static async Task ShowWhoList(IPlayer player)
    {
        var whoHeader = @"&W
Players currently online:
------------------------&N";

        await SendToPlayerAsync(player, whoHeader, formatted: true);

        // For now, just show the requesting player
        // This will be expanded to show all online players when we have player management
        var playerLine = $"&Y[{player.Level,2}] &W{player.Name}&N";
        await SendToPlayerAsync(player, playerLine, formatted: true);

        var whoFooter = $@"
&WThere is &Y1 &Wplayer online.&N";

        await SendToPlayerAsync(player, whoFooter, formatted: true);
    }

    /// <summary>
    /// Add a player to the online list (temporary static method)
    /// This will be replaced by proper player management system
    /// </summary>
    public static void AddPlayer(IPlayer player)
    {
        if (!_onlinePlayers.Contains(player))
        {
            _onlinePlayers.Add(player);
        }
    }

    /// <summary>
    /// Remove a player from the online list (temporary static method)
    /// </summary>
    public static void RemovePlayer(IPlayer player)
    {
        _onlinePlayers.Remove(player);
    }

    /// <summary>
    /// Get count of online players
    /// </summary>
    public static int GetOnlineCount()
    {
        return _onlinePlayers.Count;
    }
}