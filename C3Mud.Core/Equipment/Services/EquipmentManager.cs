using C3Mud.Core.Equipment.Models;
using C3Mud.Core.Players;
using C3Mud.Core.World.Models;
using C3Mud.Core.Characters.Models;
using System.Text;

namespace C3Mud.Core.Equipment.Services;

/// <summary>
/// Equipment management service implementation
/// Based on original CircleMUD equipment functions
/// </summary>
public class EquipmentManager : IEquipmentManager
{
    private readonly IPlayer _player;
    
    /// <summary>
    /// Strength-based carrying capacity table from original CircleMUD
    /// </summary>
    private static readonly Dictionary<int, int> StrengthCapacityTable = new()
    {
        { 3, 10 }, { 4, 20 }, { 5, 30 }, { 6, 40 }, { 7, 50 },
        { 8, 60 }, { 9, 70 }, { 10, 80 }, { 11, 90 }, { 12, 100 },
        { 13, 110 }, { 14, 120 }, { 15, 130 }, { 16, 130 }, { 17, 150 },
        { 18, 200 }, { 19, 300 }, { 20, 400 }, { 21, 500 }, { 22, 600 },
        { 23, 700 }, { 24, 800 }, { 25, 640 }
    };

    public EquipmentManager(IPlayer player)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
    }

    public EquipmentOperationResult EquipItem(WorldObject item, EquipmentSlot slot)
    {
        if (item == null)
            return EquipmentOperationResult.CreateFailure("Invalid item.");

        // Check if item can be worn in this slot
        if (!CanEquipInSlot(item, slot))
        {
            return EquipmentOperationResult.CreateFailure(GetCantWearMessage(slot));
        }
        
        // Check weight capacity
        if (GetCurrentWeight() + item.Weight > GetCarryingCapacity())
        {
            return EquipmentOperationResult.CreateFailure("That item is too heavy for you to wear.");
        }
        
        // Check level restrictions
        if (!CanUseItem(item))
        {
            return GetRestrictionFailureMessage(item);
        }
        
        // Check for two-handed weapon conflicts
        if (IsSlotConflicting(slot, item))
        {
            var conflictMessage = GetConflictMessage(slot, item);
            if (!conflictMessage.StartsWith("You stop using"))
            {
                return EquipmentOperationResult.CreateFailure(conflictMessage);
            }
            
            // Handle automatic unequipping of conflicting items
            ResolveSlotConflicts(slot, item);
            
            var equipMessage = GetEquipMessage(item, slot);
            return EquipmentOperationResult.CreateSuccess($"{conflictMessage}\r\n{equipMessage}");
        }
        
        var messages = new List<string>();
        
        // If slot already has item, unequip it first
        var currentItem = _player.GetEquippedItem(slot);
        if (currentItem != null)
        {
            messages.Add(GetUnequipMessage(currentItem, slot));
            
            // Move old item to inventory
            _player.GetInventory().Add(currentItem);
        }
        
        // Equip the new item
        if (_player is Player playerImpl)
        {
            playerImpl.SetEquippedItem(slot, item);
        }
        
        // Remove from inventory if present
        _player.GetInventory().Remove(item);
        
        // Apply stat bonuses
        ApplyEquipmentBonuses(item);
        
        messages.Add(GetEquipMessage(item, slot));
        
        return EquipmentOperationResult.CreateSuccess(string.Join("\r\n", messages));
    }

    public EquipmentOperationResult UnequipItem(EquipmentSlot slot)
    {
        var item = _player.GetEquippedItem(slot);
        if (item == null)
        {
            return EquipmentOperationResult.CreateFailure(GetNotEquippedMessage(slot));
        }
        
        // Check if item is cursed and can't be removed
        if (IsCursed(item))
        {
            return EquipmentOperationResult.CreateFailure("You can't remove that cursed item.");
        }
        
        // Remove equipment bonuses
        RemoveEquipmentBonuses(item);
        
        // Unequip item
        if (_player is Player playerImpl)
        {
            playerImpl.SetEquippedItem(slot, null);
        }
        
        // Add to inventory
        _player.GetInventory().Add(item);
        
        var message = GetUnequipMessage(item, slot);
        return EquipmentOperationResult.CreateSuccess(message);
    }

    public int GetCarryingCapacity()
    {
        var strength = _player.Strength;
        
        // Handle exceptional strength values
        if (strength > 25) strength = 25;
        if (strength < 3) strength = 3;
        
        return StrengthCapacityTable.GetValueOrDefault(strength, 100);
    }

    public int GetCurrentWeight()
    {
        var totalWeight = 0;
        foreach (EquipmentSlot slot in Enum.GetValues<EquipmentSlot>())
        {
            var item = _player.GetEquippedItem(slot);
            if (item != null)
            {
                totalWeight += item.Weight;
            }
        }
        return totalWeight;
    }

    public bool CanEquipInSlot(WorldObject item, EquipmentSlot slot)
    {
        return EquipmentSlotValidator.CanWearInSlot(item, slot);
    }

    public string GetEquipmentDisplay()
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are using:");
        
        var slotNames = new Dictionary<EquipmentSlot, string>
        {
            { EquipmentSlot.Light, "<used as light>" },
            { EquipmentSlot.FingerRight, "<worn on finger>" },
            { EquipmentSlot.FingerLeft, "<worn on finger>" },
            { EquipmentSlot.Neck1, "<worn around neck>" },
            { EquipmentSlot.Neck2, "<worn around neck>" },
            { EquipmentSlot.Body, "<worn on body>" },
            { EquipmentSlot.Head, "<worn on head>" },
            { EquipmentSlot.Legs, "<worn on legs>" },
            { EquipmentSlot.Feet, "<worn on feet>" },
            { EquipmentSlot.Hands, "<worn on hands>" },
            { EquipmentSlot.Arms, "<worn on arms>" },
            { EquipmentSlot.Shield, "<used as shield>" },
            { EquipmentSlot.About, "<worn about body>" },
            { EquipmentSlot.Waist, "<worn around waist>" },
            { EquipmentSlot.WristRight, "<worn around wrist>" },
            { EquipmentSlot.WristLeft, "<worn around wrist>" },
            { EquipmentSlot.Wield, "<wielded>" },
            { EquipmentSlot.Hold, "<held>" }
        };

        foreach (var slot in Enum.GetValues<EquipmentSlot>())
        {
            var item = _player.GetEquippedItem(slot);
            var slotName = slotNames[slot];
            
            if (item != null)
            {
                sb.AppendLine($"{slotName,-20} {item.ShortDescription}");
            }
            else
            {
                sb.AppendLine($"{slotName,-20} <nothing>");
            }
        }
        
        return sb.ToString();
    }
    
    #region Helper Methods
    
    private bool CanUseItem(WorldObject item)
    {
        // Check level restrictions based on item cost
        var requiredLevel = GetRequiredLevel(item);
        if (_player.Level < requiredLevel)
        {
            return false;
        }
        
        // Check class restrictions
        if (HasClassRestriction(item))
        {
            return false;
        }
        
        // Check alignment restrictions
        if (HasAlignmentRestriction(item))
        {
            return false;
        }
        
        return true;
    }
    
    private int GetRequiredLevel(WorldObject item)
    {
        // Simple level requirement based on item cost
        // High cost items require higher levels
        return Math.Max(1, item.Cost / 2500);
    }
    
    private bool HasClassRestriction(WorldObject item)
    {
        var playerClass = _player.GetCharacterClass();
        
        // Check anti-class flags (original ITEM_ANTI_* flags)
        var antiMage = (item.ExtraFlags & (1 << 7)) != 0; // ITEM_ANTI_MAGIC_USER
        var antiCleric = (item.ExtraFlags & (1 << 8)) != 0; // ITEM_ANTI_CLERIC
        var antiThief = (item.ExtraFlags & (1 << 9)) != 0; // ITEM_ANTI_THIEF
        var antiWarrior = (item.ExtraFlags & (1 << 10)) != 0; // ITEM_ANTI_WARRIOR
        
        return playerClass switch
        {
            CharacterClass.Mage => antiMage,
            CharacterClass.Cleric => antiCleric,
            CharacterClass.Thief => antiThief,
            CharacterClass.Warrior => antiWarrior,
            _ => false
        };
    }
    
    private bool HasAlignmentRestriction(WorldObject item)
    {
        var alignment = _player.GetAlignment();
        
        // Check anti-alignment flags
        var antiGood = (item.ExtraFlags & (1 << 11)) != 0; // ITEM_ANTI_GOOD
        var antiEvil = (item.ExtraFlags & (1 << 12)) != 0; // ITEM_ANTI_EVIL
        var antiNeutral = (item.ExtraFlags & (1 << 13)) != 0; // ITEM_ANTI_NEUTRAL
        
        return alignment switch
        {
            Alignment.Good or Alignment.LawfulGood or Alignment.NeutralGood or Alignment.ChaoticGood => antiGood,
            Alignment.Evil or Alignment.LawfulEvil or Alignment.NeutralEvil or Alignment.ChaoticEvil => antiEvil,
            Alignment.Neutral => antiNeutral,
            _ => false
        };
    }
    
    private EquipmentOperationResult GetRestrictionFailureMessage(WorldObject item)
    {
        var requiredLevel = GetRequiredLevel(item);
        if (_player.Level < requiredLevel)
        {
            return EquipmentOperationResult.CreateFailure("You are not experienced enough to use that item.");
        }
        
        if (HasClassRestriction(item) || HasAlignmentRestriction(item))
        {
            return EquipmentOperationResult.CreateFailure("You are forbidden to use that item.");
        }
        
        return EquipmentOperationResult.CreateFailure("You cannot use that item.");
    }
    
    private bool IsSlotConflicting(EquipmentSlot slot, WorldObject item)
    {
        // Check for two-handed weapon conflicts
        if (slot == EquipmentSlot.Wield && IsTwoHanded(item))
        {
            return _player.GetEquippedItem(EquipmentSlot.Shield) != null;
        }
        
        var wieldedItem = _player.GetEquippedItem(EquipmentSlot.Wield);
        if (slot == EquipmentSlot.Shield && wieldedItem != null)
        {
            return IsTwoHanded(wieldedItem);
        }
        
        return false;
    }
    
    private string GetConflictMessage(EquipmentSlot slot, WorldObject item)
    {
        var wieldedItem = _player.GetEquippedItem(EquipmentSlot.Wield);
        if (slot == EquipmentSlot.Shield && wieldedItem != null && IsTwoHanded(wieldedItem))
        {
            return "You can't use a shield while wielding a two-handed weapon.";
        }
        
        var shield = _player.GetEquippedItem(EquipmentSlot.Shield);
        if (slot == EquipmentSlot.Wield && IsTwoHanded(item) && shield != null)
        {
            return GetUnequipMessage(shield, EquipmentSlot.Shield);
        }
        
        return "";
    }
    
    private void ResolveSlotConflicts(EquipmentSlot slot, WorldObject item)
    {
        var shield = _player.GetEquippedItem(EquipmentSlot.Shield);
        if (slot == EquipmentSlot.Wield && IsTwoHanded(item) && shield != null)
        {
            RemoveEquipmentBonuses(shield);
            if (_player is Player playerImpl)
            {
                playerImpl.SetEquippedItem(EquipmentSlot.Shield, null);
            }
            _player.GetInventory().Add(shield);
        }
    }
    
    private bool IsTwoHanded(WorldObject item)
    {
        // Check for two-handed flag
        return (item.ExtraFlags & (1 << 17)) != 0; // ITEM_TWO_HANDED
    }
    
    private bool IsCursed(WorldObject item)
    {
        // Check for cursed flag
        return (item.ExtraFlags & (1 << 2)) != 0; // ITEM_CURSED
    }
    
    private void ApplyEquipmentBonuses(WorldObject item)
    {
        // TODO: Apply stat bonuses from item.Applies to player
        // This would modify player AC, hitroll, damroll, attributes, etc.
        // For now, this is a placeholder until player stat system is implemented
    }
    
    private void RemoveEquipmentBonuses(WorldObject item)
    {
        // TODO: Remove stat bonuses from item.Applies from player
        // This would revert changes to player AC, hitroll, damroll, attributes, etc.
        // For now, this is a placeholder until player stat system is implemented
    }
    
    private string GetCantWearMessage(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Wield => "You can't wield that.",
            EquipmentSlot.Shield => "You can't use that as a shield.",
            EquipmentSlot.Head => "You can't wear that on your head.",
            EquipmentSlot.Body => "You can't wear that on your body.",
            EquipmentSlot.Legs => "You can't wear that on your legs.",
            EquipmentSlot.Feet => "You can't wear that on your feet.",
            EquipmentSlot.Hands => "You can't wear that on your hands.",
            EquipmentSlot.Arms => "You can't wear that on your arms.",
            EquipmentSlot.About => "You can't wear that about your body.",
            EquipmentSlot.Waist => "You can't wear that around your waist.",
            EquipmentSlot.FingerRight or EquipmentSlot.FingerLeft => "You can't wear that on your finger.",
            EquipmentSlot.Neck1 or EquipmentSlot.Neck2 => "You can't wear that around your neck.",
            EquipmentSlot.WristRight or EquipmentSlot.WristLeft => "You can't wear that around your wrist.",
            EquipmentSlot.Hold => "You can't hold that.",
            EquipmentSlot.Light => "You can't use that as a light source.",
            _ => "You can't wear that there."
        };
    }
    
    private string GetNotEquippedMessage(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Wield => "You aren't wielding anything.",
            EquipmentSlot.Shield => "You aren't using a shield.",
            EquipmentSlot.Head => "You aren't wearing anything on your head.",
            EquipmentSlot.Body => "You aren't wearing anything on your body.",
            EquipmentSlot.Hold => "You aren't holding anything.",
            _ => "You aren't wearing anything there."
        };
    }
    
    private string GetEquipMessage(WorldObject item, EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Wield => $"You wield {item.ShortDescription}.",
            EquipmentSlot.Shield => $"You start using {item.ShortDescription} as a shield.",
            EquipmentSlot.Head => $"You wear {item.ShortDescription} on your head.",
            EquipmentSlot.Body => $"You wear {item.ShortDescription} on your body.",
            EquipmentSlot.Legs => $"You wear {item.ShortDescription} on your legs.",
            EquipmentSlot.Feet => $"You wear {item.ShortDescription} on your feet.",
            EquipmentSlot.Hands => $"You wear {item.ShortDescription} on your hands.",
            EquipmentSlot.Arms => $"You wear {item.ShortDescription} on your arms.",
            EquipmentSlot.About => $"You wear {item.ShortDescription} about your body.",
            EquipmentSlot.Waist => $"You wear {item.ShortDescription} around your waist.",
            EquipmentSlot.FingerRight => $"You slide {item.ShortDescription} on your right finger.",
            EquipmentSlot.FingerLeft => $"You slide {item.ShortDescription} on your left finger.",
            EquipmentSlot.Neck1 or EquipmentSlot.Neck2 => $"You wear {item.ShortDescription} around your neck.",
            EquipmentSlot.WristRight => $"You wear {item.ShortDescription} around your right wrist.",
            EquipmentSlot.WristLeft => $"You wear {item.ShortDescription} around your left wrist.",
            EquipmentSlot.Hold => $"You hold {item.ShortDescription}.",
            EquipmentSlot.Light => $"You light {item.ShortDescription}.",
            _ => $"You wear {item.ShortDescription}."
        };
    }
    
    private string GetUnequipMessage(WorldObject item, EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Wield => $"You stop using {item.ShortDescription}.",
            EquipmentSlot.Shield => $"You stop using {item.ShortDescription}.",
            EquipmentSlot.Hold => $"You stop holding {item.ShortDescription}.",
            EquipmentSlot.Light => $"You extinguish {item.ShortDescription}.",
            _ => $"You stop wearing {item.ShortDescription}."
        };
    }
    
    #endregion
}