using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;

namespace C3Mud.Core.Commands.Movement;

/// <summary>
/// Down movement command - moves player downward
/// Command ID 6 in original CircleMUD command table
/// </summary>
public class DownCommand : MovementCommand
{
    public override string Name => "down";
    public override string[] Aliases => new[] { "d" };
    protected override Direction Direction => Direction.Down;

    public DownCommand(IWorldDatabase worldDatabase) : base(worldDatabase)
    {
    }
}