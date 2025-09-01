using C3Mud.Core.Players;

namespace C3Mud.Core.Commands.Basic;

/// <summary>
/// Say command - allows players to communicate with others in the same room
/// Based on original do_say() function from act.comm.c
/// </summary>
public class SayCommand : BaseCommand
{
    public override string Name => "say";
    public override string[] Aliases => new[] { "'" };
    public override PlayerPosition MinimumPosition => PlayerPosition.Resting;
    public override int MinimumLevel => 1;

    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            await SendToPlayerAsync(player, "Say what?");
            return;
        }

        // Format the message for the speaker
        var speakerMessage = $"&YYou say: &W'{arguments}'&N";
        await SendToPlayerAsync(player, speakerMessage, formatted: true);

        // In a full implementation, this would send to all players in the room
        // For now, just acknowledge the command worked
        await Task.CompletedTask;
    }
}