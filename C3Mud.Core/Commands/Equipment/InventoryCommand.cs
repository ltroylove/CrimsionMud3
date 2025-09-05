using C3Mud.Core.Equipment.Services;
using C3Mud.Core.Players;

namespace C3Mud.Core.Commands.Equipment;

/// <summary>
/// Command to display player's inventory
/// Based on original CircleMUD do_inventory() function
/// </summary>
public class InventoryCommand : BaseCommand
{
    public override string Name => "inventory";
    public override string[] Aliases => new[] { "i", "inv" };
    public override PlayerPosition MinimumPosition => PlayerPosition.Sleeping;
    public override int MinimumLevel => 1;

    public InventoryCommand()
    {
    }

    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        var inventoryManager = new InventoryManager(player);
        var inventoryDisplay = inventoryManager.GetInventoryDisplay();
        await SendToPlayerAsync(player, inventoryDisplay);
    }
}