using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;

namespace C3Mud.Core.Commands.Movement;

/// <summary>
/// East movement command - moves player to the east
/// Command ID 2 in original CircleMUD command table
/// </summary>
public class EastCommand : MovementCommand
{
    public override string Name => "east";
    public override string[] Aliases => new[] { "e" };
    protected override Direction Direction => Direction.East;

    public EastCommand(IWorldDatabase worldDatabase) : base(worldDatabase)
    {
    }
}