using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;

namespace C3Mud.Core.Commands.Movement;

/// <summary>
/// South movement command - moves player to the south
/// Command ID 3 in original CircleMUD command table
/// </summary>
public class SouthCommand : MovementCommand
{
    public override string Name => "south";
    public override string[] Aliases => new[] { "s" };
    protected override Direction Direction => Direction.South;

    public SouthCommand(IWorldDatabase worldDatabase) : base(worldDatabase)
    {
    }
}