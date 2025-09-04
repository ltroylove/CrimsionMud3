using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;

namespace C3Mud.Core.Commands.Movement;

/// <summary>
/// North movement command - moves player to the north
/// Command ID 1 in original CircleMUD command table
/// </summary>
public class NorthCommand : MovementCommand
{
    public override string Name => "north";
    public override string[] Aliases => new[] { "n" };
    protected override Direction Direction => Direction.North;

    public NorthCommand(IWorldDatabase worldDatabase) : base(worldDatabase)
    {
    }
}