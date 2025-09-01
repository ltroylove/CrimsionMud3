using C3Mud.Core.Players;

namespace C3Mud.Core.Commands.Basic;

/// <summary>
/// Look command - allows players to examine their surroundings
/// Based on original do_look() function from act.informative.c
/// </summary>
public class LookCommand : BaseCommand
{
    public override string Name => "look";
    public override string[] Aliases => new[] { "l" };
    public override PlayerPosition MinimumPosition => PlayerPosition.Sleeping;
    public override int MinimumLevel => 1;

    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            // Look at the room
            await LookAtRoom(player);
        }
        else
        {
            // Look at specific target (not implemented yet - would require world system)
            await SendToPlayerAsync(player, "You don't see that here.");
        }
    }

    private static async Task LookAtRoom(IPlayer player)
    {
        // For now, send a basic room description
        // This will be expanded when we implement the world/room system
        var roomDescription = @"&WA Simple Room&N

You are standing in a basic testing room. This is a placeholder room description
that will be replaced once the world system is implemented. The walls are bare
and the floor is made of stone.

&YObvious exits:&N
  None (world system not yet implemented)

&GPlayers here:&N";

        await SendToPlayerAsync(player, roomDescription, formatted: true);
        
        // For now, just show the current player
        await SendToPlayerAsync(player, $"  {player.Name} is standing here.", formatted: true);
    }
}