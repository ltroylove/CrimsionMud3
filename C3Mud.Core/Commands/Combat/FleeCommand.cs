using C3Mud.Core.Combat;
using C3Mud.Core.Players;
using C3Mud.Core.World.Services;

namespace C3Mud.Core.Commands.Combat;

/// <summary>
/// Flee command for escaping from combat
/// Stub implementation for TDD Red phase
/// </summary>
public class FleeCommand : BaseCommand
{
    private readonly IWorldDatabase _worldDatabase;
    private readonly ICombatEngine _combatEngine;

    public FleeCommand(IWorldDatabase worldDatabase, ICombatEngine combatEngine)
    {
        _worldDatabase = worldDatabase ?? throw new ArgumentNullException(nameof(worldDatabase));
        _combatEngine = combatEngine ?? throw new ArgumentNullException(nameof(combatEngine));
    }

    public override string Name => "flee";
    public override string[] Aliases => new[] { "run" };

    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        // Check if player is in combat
        if (!player.IsInCombat)
        {
            await player.SendMessageAsync("You are not fighting anyone!");
            return;
        }

        // Attempt to flee using the combat engine
        var success = await _combatEngine.AttemptFleeAsync(player);
        
        if (success)
        {
            await player.SendMessageAsync("You flee head over heels.");
        }
        else
        {
            await player.SendMessageAsync("PANIC! You couldn't escape!");
        }
    }
}