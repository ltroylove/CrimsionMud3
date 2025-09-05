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
    private readonly IWorldDatabase _worldDatabase;

    public override string Name => "wear";
    public override string[] Aliases => new[] { "wield", "hold" };
    public override PlayerPosition MinimumPosition => PlayerPosition.Standing;
    public override int MinimumLevel => 1;

    public WearCommand(IWorldDatabase worldDatabase)
    {
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
        var item = ItemSearchService.FindItem(player, itemName, inventory);

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

        // Create equipment manager with current player context
        var equipmentManager = new EquipmentManager(player, _worldDatabase);
        var result = equipmentManager.EquipItem(item, slot.Value);
        
        await SendToPlayerAsync(player, result.Message);
    }

    private EquipmentSlot? DetermineEquipmentSlot(WorldObject item)
    {
        // Light sources first (highest priority)
        if (item.ObjectType == ObjectType.LIGHT)
            return EquipmentSlot.Light;
            
        // Check wear flags in original CircleMUD priority order using modern C# flag checking
        var wearFlags = (WearFlags)item.WearFlags;
        
        if (wearFlags.HasFlag(WearFlags.FINGER))
            return EquipmentSlot.FingerRight;
            
        if (wearFlags.HasFlag(WearFlags.NECK))
            return EquipmentSlot.Neck1;
            
        if (wearFlags.HasFlag(WearFlags.BODY))
            return EquipmentSlot.Body;
            
        if (wearFlags.HasFlag(WearFlags.HEAD))
            return EquipmentSlot.Head;
            
        if (wearFlags.HasFlag(WearFlags.LEGS))
            return EquipmentSlot.Legs;
            
        if (wearFlags.HasFlag(WearFlags.FEET))
            return EquipmentSlot.Feet;
            
        if (wearFlags.HasFlag(WearFlags.HANDS))
            return EquipmentSlot.Hands;
            
        if (wearFlags.HasFlag(WearFlags.ARMS))
            return EquipmentSlot.Arms;
            
        if (wearFlags.HasFlag(WearFlags.SHIELD))
            return EquipmentSlot.Shield;
            
        if (wearFlags.HasFlag(WearFlags.ABOUT))
            return EquipmentSlot.About;
            
        if (wearFlags.HasFlag(WearFlags.WAIST))
            return EquipmentSlot.Waist;
            
        if (wearFlags.HasFlag(WearFlags.WRIST))
            return EquipmentSlot.WristRight;
            
        if (wearFlags.HasFlag(WearFlags.HOLD))
            return EquipmentSlot.Hold;
            
        if (wearFlags.HasFlag(WearFlags.WIELD))
            return EquipmentSlot.Wield;
            
        if (wearFlags.HasFlag(WearFlags.TAIL))
            return EquipmentSlot.Tail;
            
        if (wearFlags.HasFlag(WearFlags.FOURLEGS))
            return EquipmentSlot.FourLegs1;

        return null;
    }
}