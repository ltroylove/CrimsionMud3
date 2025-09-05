using C3Mud.Core.World.Models;

namespace C3Mud.Core.Characters.Models;

/// <summary>
/// Template for mobile (NPC) creation
/// Based on original mob_index_data structure
/// </summary>
public class MobileTemplate
{
    public int VirtualNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Combat stats
    public int Level { get; set; }
    public int MaxHitPoints { get; set; }
    public int ArmorClass { get; set; } = 10; // Default AC
    public int Strength { get; set; } = 13;
    public int Dexterity { get; set; } = 13;
    public int Constitution { get; set; } = 13;
    public int Intelligence { get; set; } = 13;
    public int Wisdom { get; set; } = 13;
    public int Charisma { get; set; } = 13;
    
    // Damage dice (original: damnodice, damsizedice)
    public int DamageDiceCount { get; set; } = 1;
    public int DamageDiceSides { get; set; } = 4;
    public int DamageBonus { get; set; } = 0;
    
    // Behavioral flags (original PLR1_* flags)
    public bool IsAggressive { get; set; }
    public bool IsWimpy { get; set; }
    public bool IsPoisonous { get; set; }
    public bool IsScavenger { get; set; }
    public bool StaysInZone { get; set; }
    
    // Special attacks (original: attack_type array)
    public List<AttackType> AttackTypes { get; set; } = new();
    
    // Equipment
    public WorldObject? DefaultWeapon { get; set; }
    private readonly Dictionary<EquipmentSlot, WorldObject> _equipment = new();
    
    // Skills (mobs have fixed skill levels)
    private readonly Dictionary<string, int> _skills = new();
    
    // Economic data
    public int Gold { get; set; }
    public int ExperienceReward { get; set; }
    
    // Special procedure function name (for lookup)
    public string? SpecialProcedure { get; set; }
    
    public WorldObject? GetEquippedItem(EquipmentSlot slot)
    {
        _equipment.TryGetValue(slot, out var item);
        return item;
    }
    
    public void SetEquippedItem(EquipmentSlot slot, WorldObject item)
    {
        _equipment[slot] = item;
    }
    
    public int GetSkillLevel(string skillName)
    {
        _skills.TryGetValue(skillName.ToLower(), out var level);
        return level; // Returns 0 if not found
    }
    
    public void SetSkillLevel(string skillName, int level)
    {
        _skills[skillName.ToLower()] = level;
    }
}