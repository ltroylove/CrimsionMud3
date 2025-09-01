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

        // TODO: PLACEHOLDER - This only sends to current player, not others in room
        // FAILING TESTS: LegacyCommandProcessingTests.ProcessCommandAsync_SayCommand_ShouldHandleSayWithLegacyFormat
        // Expected: "You say" in output but test may be checking message delivery
        // REQUIRED FIXES:
        // 1. Get list of all players in current room
        // 2. Send "PlayerName says: 'message'" to all other players in room
        // 3. Send "You say: 'message'" confirmation to speaker
        // 4. Match exact message format from original MUD
        // 5. Handle room broadcasting properly
        // 6. Add proper color codes matching legacy format
        // 7. Handle empty/whitespace arguments properly
        
        // Format the message for the speaker
        var speakerMessage = $"&YYou say: &W'{arguments}'&N";
        await SendToPlayerAsync(player, speakerMessage, formatted: true);

        // TODO: MISSING - Send to all players in the room
        // In a full implementation, this would send to all players in the room:
        // var roomMessage = $"&Y{player.Name} says: &W'{arguments}'&N";
        // foreach (var otherPlayer in GetPlayersInRoom(player.CurrentRoom))
        // {
        //     if (otherPlayer != player)
        //         await SendToPlayerAsync(otherPlayer, roomMessage, formatted: true);
        // }
        await Task.CompletedTask;
    }
}