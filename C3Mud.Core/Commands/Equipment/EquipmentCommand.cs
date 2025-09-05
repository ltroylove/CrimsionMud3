using C3Mud.Core.Equipment.Services;
using C3Mud.Core.Players;

namespace C3Mud.Core.Commands.Equipment;

/// <summary>
/// Command to display player's equipment
/// Based on original CircleMUD do_equipment() function
/// </summary>
public class EquipmentCommand : BaseCommand
{
    public override string Name => "equipment";
    public override string[] Aliases => new[] { "eq", "equ" };
    public override PlayerPosition MinimumPosition => PlayerPosition.Sleeping;
    public override int MinimumLevel => 1;

    public EquipmentCommand()
    {
    }

    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        var equipmentManager = new EquipmentManager(player);
        var equipmentDisplay = equipmentManager.GetEquipmentDisplay();
        await SendToPlayerAsync(player, equipmentDisplay);
    }
}