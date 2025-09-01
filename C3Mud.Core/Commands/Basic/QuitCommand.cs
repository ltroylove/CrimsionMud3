using C3Mud.Core.Players;

namespace C3Mud.Core.Commands.Basic;

/// <summary>
/// Quit command - allows players to leave the game
/// Based on original do_quit() function
/// </summary>
public class QuitCommand : BaseCommand
{
    public override string Name => "quit";
    public override string[] Aliases => new[] { "q" };
    public override PlayerPosition MinimumPosition => PlayerPosition.Stunned;
    public override int MinimumLevel => 1;

    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        // Check if player is in combat (when combat system is implemented)
        // For now, allow quit from any position above stunned
        
        if (player.Position == PlayerPosition.Fighting)
        {
            await SendToPlayerAsync(player, "No way! You are fighting for your life!");
            return;
        }

        // Send goodbye message
        await SendToPlayerAsync(player, "&WGoodbye, friend.. Come back soon!&N", formatted: true);
        
        // Disconnect the player
        await player.DisconnectAsync("Player quit the game");
    }
}