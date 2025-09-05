using C3Mud.Core.Characters.Models;
using C3Mud.Core.World.Models;
using C3Mud.Core.Players.Models;
using C3Mud.Core.Players;

namespace C3Mud.Core.Characters;

/// <summary>
/// Mobile (NPC) character implementation
/// Based on original mob_index_data and char_data structures
/// </summary>
public class Mobile : ICharacter
{
    private readonly MobileTemplate _template;
    
    public Mobile(MobileTemplate template)
    {
        _template = template ?? throw new ArgumentNullException(nameof(template));
        Nr = template.VirtualNumber;
        MaxHitPoints = template.MaxHitPoints;
        HitPoints = MaxHitPoints; // Start at full health
    }
    
    public string Name => _template.Name;
    public int Nr { get; set; }
    public CharacterType Type => CharacterType.NPC;
    public int CurrentRoomVnum { get; set; }
    public PlayerPosition Position { get; set; } = PlayerPosition.Standing;
    
    // Combat stats from template
    public int HitPoints { get; set; }
    public int MaxHitPoints { get; set; }
    public int Level => _template.Level;
    public int ArmorClass => _template.ArmorClass;
    public int Strength => _template.Strength;
    public int Dexterity => _template.Dexterity; 
    public int Constitution => _template.Constitution;
    
    // Combat state
    public ICharacter? Fighting { get; set; }
    public bool IsInCombat => Fighting != null;
    
    // Mobile-specific properties
    public MobileTemplate Template => _template;
    public bool IsAggressive => _template.IsAggressive;
    public bool IsWimpy => _template.IsWimpy;
    public bool IsPoisonous => _template.IsPoisonous;
    
    public WorldObject? GetWieldedWeapon()
    {
        // Mobs can have default weapons from template
        return _template.DefaultWeapon;
    }
    
    public WorldObject? GetEquippedItem(EquipmentSlot slot)
    {
        // Simple implementation - mobs don't change equipment much
        return _template.GetEquippedItem(slot);
    }
    
    public async Task SendMessageAsync(string message)
    {
        // Mobs don't receive messages (no connection)
        await Task.CompletedTask;
    }
    
    public int GetSkillLevel(string skillName)
    {
        // Mobs have fixed skill levels from template
        return _template.GetSkillLevel(skillName);
    }
    
    /// <summary>
    /// Get mob's damage dice from template
    /// Original: ch->specials.damnodice, ch->specials.damsizedice
    /// </summary>
    public (int count, int sides) GetDamageDice()
    {
        return (_template.DamageDiceCount, _template.DamageDiceSides);
    }
    
    /// <summary>
    /// Check if mob has special attack type
    /// Original: ch->specials.attack_type
    /// </summary>
    public bool HasAttackType(AttackType attackType)
    {
        return _template.AttackTypes.Contains(attackType);
    }
}