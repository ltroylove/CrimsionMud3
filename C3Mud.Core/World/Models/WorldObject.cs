using System.Collections.Generic;

namespace C3Mud.Core.World.Models;

/// <summary>
/// Represents an object (item/equipment) template from CircleMUD/DikuMUD .obj files
/// This is the template/prototype - actual object instances are created from this
/// </summary>
public class WorldObject
{
    /// <summary>
    /// Virtual Number - unique identifier for this object template
    /// </summary>
    public int VirtualNumber { get; set; }
    
    /// <summary>
    /// Keywords used to reference this object (e.g., "obj thunder hammer giant")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Short description shown in inventory/rooms (e.g., "&wa &YThunder &wHammer&n")
    /// </summary>
    public string ShortDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Long description shown when the object is on the ground
    /// </summary>
    public string LongDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Action description shown when object is used/manipulated
    /// </summary>
    public string ActionDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of object (LIGHT, SCROLL, WAND, WEAPON, ARMOR, etc.)
    /// </summary>
    public ObjectType ObjectType { get; set; }
    
    /// <summary>
    /// Extra flags (bitfield) - GLOW, HUM, MAGIC, NODROP, etc.
    /// </summary>
    public long ExtraFlags { get; set; }
    
    /// <summary>
    /// Wear flags (bitfield) - TAKE, FINGER, NECK, BODY, HEAD, etc.
    /// </summary>
    public long WearFlags { get; set; }
    
    /// <summary>
    /// Weight of the object in mud weight units
    /// </summary>
    public int Weight { get; set; }
    
    /// <summary>
    /// Base cost/value of the object in gold coins
    /// </summary>
    public int Cost { get; set; }
    
    /// <summary>
    /// Daily rent cost for this object
    /// </summary>
    public int RentPerDay { get; set; }
    
    /// <summary>
    /// Type-specific values array (damage dice, AC, spell levels, etc.)
    /// Usage depends on ObjectType:
    /// - WEAPON: [0]=damage dice sides, [1]=damage dice count, [2]=damage bonus, [3]=weapon type
    /// - ARMOR: [0]=AC apply, [1-3]=unused
    /// - LIGHT: [0]=unused, [1]=hours of light, [2]=unused, [3]=unused
    /// - SCROLL/POTION/WAND: [0]=spell level, [1-3]=spell numbers
    /// </summary>
    public int[] Values { get; set; } = new int[4];
    
    /// <summary>
    /// Additional effects/applies that modify player stats when worn/used
    /// Dictionary of stat type to modification value
    /// Common applies: STR, DEX, CON, INT, WIS, CHA, HP, MANA, AC, HITROLL, DAMROLL
    /// </summary>
    public Dictionary<int, int> Applies { get; set; } = new Dictionary<int, int>();
    
    /// <summary>
    /// Extra descriptions for detailed examination
    /// Dictionary of keywords to description text
    /// </summary>
    public Dictionary<string, string> ExtraDescriptions { get; set; } = new Dictionary<string, string>();
    
    /// <summary>
    /// Creates a copy of this object template for instantiation in the game world
    /// </summary>
    /// <returns>A new WorldObject instance based on this template</returns>
    public WorldObject CreateInstance()
    {
        return new WorldObject
        {
            VirtualNumber = this.VirtualNumber,
            Name = this.Name,
            ShortDescription = this.ShortDescription,
            LongDescription = this.LongDescription,
            ActionDescription = this.ActionDescription,
            ObjectType = this.ObjectType,
            ExtraFlags = this.ExtraFlags,
            WearFlags = this.WearFlags,
            Weight = this.Weight,
            Cost = this.Cost,
            RentPerDay = this.RentPerDay,
            Values = (int[])this.Values.Clone(),
            Applies = new Dictionary<int, int>(this.Applies),
            ExtraDescriptions = new Dictionary<string, string>(this.ExtraDescriptions)
        };
    }
}