using C3Mud.Core.Combat;
using C3Mud.Core.Players;
using C3Mud.Core.World.Services;

namespace C3Mud.Core.Commands.Combat;

/// <summary>
/// Kill command for initiating combat
/// Stub implementation for TDD Red phase
/// </summary>
public class KillCommand : BaseCommand
{
    private readonly IWorldDatabase _worldDatabase;
    private readonly ICombatEngine _combatEngine;

    public KillCommand(IWorldDatabase worldDatabase, ICombatEngine combatEngine)
    {
        _worldDatabase = worldDatabase ?? throw new ArgumentNullException(nameof(worldDatabase));
        _combatEngine = combatEngine ?? throw new ArgumentNullException(nameof(combatEngine));
    }

    public override string Name => "kill";
    public override string[] Aliases => new[] { "k", "attack" };

    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        // Check if player is already in combat
        if (player.IsInCombat)
        {
            await player.SendMessageAsync("You are already fighting someone!");
            return;
        }

        // Parse target name from arguments
        var targetName = arguments.Trim().Split(' ').FirstOrDefault();
        if (string.IsNullOrEmpty(targetName))
        {
            await player.SendMessageAsync("Kill who?");
            return;
        }

        // Prevent self-attack
        if (targetName.Equals(player.Name, StringComparison.OrdinalIgnoreCase))
        {
            await player.SendMessageAsync("You hit yourself. OUCH!");
            return;
        }

        // Get current room
        var room = _worldDatabase.GetRoom(player.CurrentRoomVnum);
        if (room == null)
        {
            await player.SendMessageAsync("You can't fight here.");
            return;
        }

        // Find target player in the room
        var target = room.Players.FirstOrDefault(p => 
            p.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase) && p != player);
        
        if (target == null)
        {
            await player.SendMessageAsync("They aren't here.");
            return;
        }

        // Attempt to initiate combat
        var success = await _combatEngine.InitiateCombatAsync(player, target);
        if (!success)
        {
            // Combat engine will have sent appropriate error message
            return;
        }

        // Combat initiated successfully - engine already sent messages
    }
}