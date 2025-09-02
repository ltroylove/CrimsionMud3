using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;

namespace C3Mud.Core.Commands.Movement;

/// <summary>
/// West movement command - moves player to the west
/// Command ID 4 in original CircleMUD command table
/// </summary>
public class WestCommand : MovementCommand
{
    public override string Name => "west";
    public override string[] Aliases => new[] { "w" };
    protected override Direction Direction => Direction.West;

    public WestCommand(IWorldDatabase worldDatabase) : base(worldDatabase)
    {
    }
}