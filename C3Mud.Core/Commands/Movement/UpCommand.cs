using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;

namespace C3Mud.Core.Commands.Movement;

/// <summary>
/// Up movement command - moves player upward
/// Command ID 5 in original CircleMUD command table
/// </summary>
public class UpCommand : MovementCommand
{
    public override string Name => "up";
    public override string[] Aliases => new[] { "u" };
    protected override Direction Direction => Direction.Up;

    public UpCommand(IWorldDatabase worldDatabase) : base(worldDatabase)
    {
    }
}