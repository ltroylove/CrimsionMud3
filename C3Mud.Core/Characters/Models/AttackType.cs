namespace C3Mud.Core.Characters.Models;

/// <summary>
/// Special attack types for mobiles
/// Based on original attack_type array from mob_index_data
/// </summary>
public enum AttackType
{
    None = 0,
    Bite = 1,
    Claw = 2,
    Sting = 3,
    Kick = 4,
    Bash = 5,
    Crush = 6,
    Poison = 7,
    Breathe_Fire = 8,
    Breathe_Frost = 9,
    Breathe_Acid = 10,
    Breathe_Gas = 11,
    Breathe_Lightning = 12
}