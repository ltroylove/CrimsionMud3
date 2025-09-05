using C3Mud.Core.Equipment.Services;
using C3Mud.Core.Players;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;

namespace C3Mud.Core.Commands.Equipment;

/// <summary>
/// Command to wear or wield equipment items
/// Based on original CircleMUD do_wear() and do_wield() functions
/// </summary>
public class WearCommand : BaseCommand
{
    private readonly IEquipmentManager _equipmentManager;
    private readonly IWorldDatabase _worldDatabase;

    public override string Name => "wear";
    public override string[] Aliases => new[] { "wield", "hold" };
    public override PlayerPosition MinimumPosition => PlayerPosition.Standing;
    public override int MinimumLevel => 1;

    public WearCommand(IEquipmentManager equipmentManager, IWorldDatabase worldDatabase)
    {
        _equipmentManager = equipmentManager ?? throw new ArgumentNullException(nameof(equipmentManager));
        _worldDatabase = worldDatabase ?? throw new ArgumentNullException(nameof(worldDatabase));
    }

    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            await SendToPlayerAsync(player, "Wear what?");
            return;
        }

        var itemName = arguments.Trim();
        var inventory = player.GetInventory();
        var item = inventory.FirstOrDefault(obj => obj.Name.Contains(itemName, StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            await SendToPlayerAsync(player, "You don't have that item.");
            return;
        }

        // Determine the appropriate slot based on item type
        var slot = DetermineEquipmentSlot(item);
        if (slot == null)
        {
            await SendToPlayerAsync(player, "You can't wear that.");
            return;
        }

        // Use equipment manager to equip the item
        var result = _equipmentManager.EquipItem(item, slot.Value);
        
        await SendToPlayerAsync(player, result.Message);
    }

    private EquipmentSlot? DetermineEquipmentSlot(WorldObject item)
    {
        // Auto-determine based on item type
        if (item.ObjectType == ObjectType.WEAPON)
            return EquipmentSlot.Wield;
        if (item.ObjectType == ObjectType.ARMOR)
            return EquipmentSlot.Body; // Default to body, could be enhanced
        if (item.ObjectType == ObjectType.WORN)
            return EquipmentSlot.FingerRight; // Default for jewelry

        return null;
    }
}