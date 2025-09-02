using System.Collections.Generic;

namespace C3Mud.Core.World.Models;

/// <summary>
/// Represents a mobile (NPC/Monster) template from CircleMUD/DikuMUD .mob files
/// This is the template/prototype - actual mobile instances are created from this
/// </summary>
public class Mobile
{
    /// <summary>
    /// Virtual Number - unique identifier for this mobile template
    /// </summary>
    public int VirtualNumber { get; set; }
    
    /// <summary>
    /// Keywords used to reference this mobile (e.g., "mob jotun storm giant")
    /// </summary>
    public string Keywords { get; set; } = string.Empty;
    
    /// <summary>
    /// Short description shown in room (e.g., "Jotun, ruler of the Storm Giants")
    /// </summary>
    public string ShortDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Long description shown when the mobile is looked at
    /// </summary>
    public string LongDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description shown when the mobile is examined
    /// </summary>
    public string DetailedDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Mobile's level (determines stats and abilities)
    /// </summary>
    public int Level { get; set; }
    
    /// <summary>
    /// Maximum hit points this mobile can have
    /// </summary>
    public int MaxHitPoints { get; set; }
    
    /// <summary>
    /// Maximum mana points this mobile can have
    /// </summary>
    public int MaxMana { get; set; }
    
    /// <summary>
    /// Armor class (lower is better defense)
    /// </summary>
    public int ArmorClass { get; set; }
    
    /// <summary>
    /// Base damage dice (e.g., "20d30+8000")
    /// </summary>
    public string DamageRoll { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional damage dice for special attacks
    /// </summary>
    public string BonusDamageRoll { get; set; } = string.Empty;
    
    /// <summary>
    /// Experience points awarded when this mobile is killed
    /// </summary>
    public int Experience { get; set; }
    
    /// <summary>
    /// Gold coins this mobile carries
    /// </summary>
    public int Gold { get; set; }
    
    /// <summary>
    /// Mobile's alignment (-1000 to 1000)
    /// </summary>
    public int Alignment { get; set; }
    
    /// <summary>
    /// Mobile flags (bitfield for various behaviors)
    /// </summary>
    public long MobileFlags { get; set; }
    
    /// <summary>
    /// Affection flags (bitfield for magical effects)
    /// </summary>
    public long AffectionFlags { get; set; }
    
    /// <summary>
    /// Default position when spawned
    /// </summary>
    public int DefaultPosition { get; set; }
    
    /// <summary>
    /// Current position (standing, sitting, sleeping, etc.)
    /// </summary>
    public int Position { get; set; }
    
    /// <summary>
    /// Mobile's sex/gender (0=neutral, 1=male, 2=female)
    /// </summary>
    public int Sex { get; set; }
    
    /// <summary>
    /// Statistics - Strength, Intelligence, Wisdom, Dexterity, Constitution, Charisma
    /// </summary>
    public int Strength { get; set; }
    public int Intelligence { get; set; }
    public int Wisdom { get; set; }
    public int Dexterity { get; set; }
    public int Constitution { get; set; }
    public int Charisma { get; set; }
    
    /// <summary>
    /// Additional strength (for values > 18)
    /// </summary>
    public int StrengthAdd { get; set; }
    
    /// <summary>
    /// Size class of the mobile
    /// </summary>
    public int Size { get; set; }
    
    /// <summary>
    /// Skills and spells this mobile knows (skill name -> skill level)
    /// </summary>
    public Dictionary<string, int> Skills { get; set; } = new Dictionary<string, int>();
    
    /// <summary>
    /// Special attack skills and their proficiency levels
    /// </summary>
    public List<int> AttackSkills { get; set; } = new List<int>();
    
    /// <summary>
    /// Special attack types this mobile can use
    /// </summary>
    public List<int> AttackTypes { get; set; } = new List<int>();
    
    /// <summary>
    /// Creates a copy of this mobile template for spawning
    /// </summary>
    /// <returns>A new Mobile instance based on this template</returns>
    public Mobile CreateInstance()
    {
        return new Mobile
        {
            VirtualNumber = this.VirtualNumber,
            Keywords = this.Keywords,
            ShortDescription = this.ShortDescription,
            LongDescription = this.LongDescription,
            DetailedDescription = this.DetailedDescription,
            Level = this.Level,
            MaxHitPoints = this.MaxHitPoints,
            MaxMana = this.MaxMana,
            ArmorClass = this.ArmorClass,
            DamageRoll = this.DamageRoll,
            BonusDamageRoll = this.BonusDamageRoll,
            Experience = this.Experience,
            Gold = this.Gold,
            Alignment = this.Alignment,
            MobileFlags = this.MobileFlags,
            AffectionFlags = this.AffectionFlags,
            DefaultPosition = this.DefaultPosition,
            Position = this.Position,
            Sex = this.Sex,
            Strength = this.Strength,
            Intelligence = this.Intelligence,
            Wisdom = this.Wisdom,
            Dexterity = this.Dexterity,
            Constitution = this.Constitution,
            Charisma = this.Charisma,
            StrengthAdd = this.StrengthAdd,
            Size = this.Size,
            Skills = new Dictionary<string, int>(this.Skills),
            AttackSkills = new List<int>(this.AttackSkills),
            AttackTypes = new List<int>(this.AttackTypes)
        };
    }
}