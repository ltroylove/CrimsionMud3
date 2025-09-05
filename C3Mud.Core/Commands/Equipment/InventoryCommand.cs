using C3Mud.Core.Equipment.Services;
using C3Mud.Core.Players;

namespace C3Mud.Core.Commands.Equipment;

/// <summary>
/// Command to display player's inventory
/// Based on original CircleMUD do_inventory() function
/// </summary>
public class InventoryCommand : BaseCommand
{
    private readonly IInventoryManager _inventoryManager;

    public override string Name => "inventory";
    public override string[] Aliases => new[] { "i", "inv" };
    public override PlayerPosition MinimumPosition => PlayerPosition.Sleeping;
    public override int MinimumLevel => 1;

    public InventoryCommand(IInventoryManager inventoryManager)
    {
        _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
    }

    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        var inventoryDisplay = _inventoryManager.GetInventoryDisplay();
        await SendToPlayerAsync(player, inventoryDisplay);
    }
}