using C3Mud.Core.Equipment.Services;
using C3Mud.Core.Players;
using C3Mud.Core.World.Models;

namespace C3Mud.Core.Commands.Equipment;

/// <summary>
/// Command to remove worn/wielded equipment
/// Based on original CircleMUD do_remove() function
/// </summary>
public class RemoveCommand : BaseCommand
{
    public override string Name => "remove";
    public override string[] Aliases => new[] { "unwield", "unwear" };
    public override PlayerPosition MinimumPosition => PlayerPosition.Standing;
    public override int MinimumLevel => 1;

    public RemoveCommand()
    {
    }

    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            await SendToPlayerAsync(player, "Remove what?");
            return;
        }

        var itemName = arguments.Trim();
        
        // Find the equipped item by name
        EquipmentSlot? targetSlot = null;
        foreach (EquipmentSlot slot in Enum.GetValues<EquipmentSlot>())
        {
            var equippedItem = player.GetEquippedItem(slot);
            if (equippedItem != null && 
                equippedItem.Name.Contains(itemName, StringComparison.OrdinalIgnoreCase))
            {
                targetSlot = slot;
                break;
            }
        }

        if (targetSlot == null)
        {
            await SendToPlayerAsync(player, "You aren't wearing that.");
            return;
        }

        // Use equipment manager to unequip the item
        var equipmentManager = new EquipmentManager(player);
        var result = equipmentManager.UnequipItem(targetSlot.Value);
        
        await SendToPlayerAsync(player, result.Message);
    }
}