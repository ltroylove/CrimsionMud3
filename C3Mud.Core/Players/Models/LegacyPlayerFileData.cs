using System.Runtime.InteropServices;

namespace C3Mud.Core.Players.Models;

/// <summary>
/// Legacy player file data structure matching original char_file_u from structs.h
/// This struct maintains exact binary compatibility with the original MUD player files
/// Based on Original-Code/src/structs.h lines 1372-1463
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LegacyPlayerFileData
{
    // Core character attributes (matching original struct order)
    public sbyte Sex;                   // sbyte sex
    public sbyte Class;                 // sbyte class  
    public sbyte Race;                  // sbyte race
    public sbyte Level;                 // sbyte level
    public long Birth;                  // time_t birth (64-bit on modern systems)
    public int Played;                  // int played
    public int LastPkill;               // int lastpkill
    public long LastLogon;              // time_t last_logon
    public long LogoffTime;             // time_t logoff_time
    public byte HowPlayerLeftGame;      // ubyte how_player_left_game
    
    // Physical attributes
    public byte Weight;                 // ubyte weight
    public byte Height;                 // ubyte height
    
    // String fields with fixed sizes matching original
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
    public string Title;                // char title[80]
    
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)] // MAX_DESC
    public string Description;          // char description[MAX_DESC]
    
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
    public string ImmortalEnter;        // char immortal_enter[80]
    
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
    public string ImmortalExit;         // char immortal_exit[80]
    
    // Room and visibility
    public short StartRoom;             // sh_int start_room
    public short PrivateRoom;           // sh_int private_room
    public sbyte VisibleLevel;          // sbyte visible_level
    
    // Core game data structures (simplified for now - will expand)
    public LegacyCharAbilityData Abilities;     // struct char_ability_data abilities
    public LegacyCharPointData Points;          // struct char_point_data points
    
    // Skills array (MAX_SKILLS from original - typically around 200)
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
    public LegacyCharSkillData[] Skills;        // struct char_skill_data skills[MAX_SKILLS]
    
    // Affected array (MAX_AFFECT from original - typically 25-50)
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
    public LegacyAffectedType[] Affected;       // struct affected_type affected[MAX_AFFECT]
    
    // Clan data
    public LegacyClanData Clan;                 // struct clan_data clan
    
    // Saving throws
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    public short[] ApplySavingThrow;            // sh_int apply_saving_throw[5]
    
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public short[] Conditions;                  // signed short int conditions[3]
    
    public byte SpellsToLearn;                  // ubyte spells_to_learn
    public int Alignment;                       // int alignment
    
    // ACT flags (behavior flags)
    public long Act;                            // long act
    public long Act2;                           // long act2
    public long Act3;                           // long act3
    public long Act4;                           // long act4
    public long Act5;                           // long act5
    public long Act6;                           // long act6
    public long JailTime;                       // long jailtime
    
    // Time-based restrictions
    public long DeathShadowTime;                // long deathshadowtime
    public long GodMuzzleTime;                  // long godmuzzletime
    
    // Level-based features
    public int GhostLevel;                      // int ghost_lvl
    public int IncognitoLevel;                  // int incognito_lvl
    
    // Quest system
    public int QuestPoints;                     // int questpoints
    public int NextQuest;                       // int nextquest
    
    public short ScreenLines;                   // sh_int screen_lines
    
    // Authentication data
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
    public string Name;                         // char name[20]
    
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
    public string Password;                     // char pwd[11]
    
    // Padding fields
    public byte FillerOne;                      // ubyte filler_one
    public byte FillerTwo;                      // ubyte filler_two  
    public byte FillerThree;                    // ubyte filler_three
    
    public long TimeOfMuzzle;                   // time_t time_of_muzzle
    public long DescriptorFlag1;                // long descriptor_flag1
    
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 36)]
    public string EmailName;                    // char email_name[36]
    
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)]
    public string Filler;                       // char filler[200]
    
    public int NextCast;                        // int nextcast
    public int Jailed;                          // int jailed
    
    // Spare fields for future expansion (matching original)
    public int Spare1;                          // int spare1
    public int Spare2;                          // int spare2
    public int Spare3;                          // int spare3
    public int Spare4;                          // int spare4
    public int Spare5;                          // int spare5
    public long WizPerms;                       // long wiz_perms (was spare6)
    public long Spare7;                         // long spare7
    public long Spare8;                         // long spare8
    public long Spare9;                         // long spare9
    public long Spare10;                        // long spare10
    public byte Spare11;                        // ubyte spare11
    public byte Spare12;                        // ubyte spare12
    public byte Spare13;                        // ubyte spare13
    public byte Spare14;                        // ubyte spare14
    public byte Spare15;                        // ubyte spare15
    
    // Spare strings for future expansion
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)]
    public string SpareString1;                 // char sparestring1[200]
    
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)]
    public string SpareString2;                 // char sparestring2[200]
    
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)]
    public string SpareString3;                 // char sparestring3[200]
    
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)]
    public string SpareString4;                 // char sparestring4[200]
    
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)]
    public string SpareString5;                 // char sparestring5[200]
    
    /// <summary>
    /// Sets the player name with validation matching original MUD rules
    /// </summary>
    public void SetName(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            throw new ArgumentException("Player name cannot be null or empty");
            
        if (playerName.Length < 2)
            throw new ArgumentException("Player name must be at least 2 characters");
            
        if (playerName.Length > 19)
            throw new ArgumentException("Player name cannot exceed 19 characters");
            
        if (!playerName.All(char.IsLetter))
            throw new ArgumentException("Player name can only contain letters");
            
        if (!char.IsUpper(playerName[0]))
            throw new ArgumentException("Player name must start with uppercase letter");
            
        if (playerName.Skip(1).Any(char.IsUpper))
            throw new ArgumentException("Only first letter can be uppercase");
            
        Name = playerName;
    }
    
    /// <summary>
    /// Sets the password with validation matching original limits
    /// </summary>
    public void SetPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty");
            
        if (password.Length > 10)
            throw new ArgumentException("Password cannot exceed 10 characters");
            
        Password = password;
    }
}

/// <summary>
/// Character ability data matching original struct char_ability_data
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LegacyCharAbilityData
{
    public byte Strength;
    public byte Intelligence;
    public byte Wisdom;
    public byte Dexterity;
    public byte Constitution;
    public byte Charisma;
    public byte StrengthAdd;  // For 18/xx strength
}

/// <summary>
/// Character point data matching original struct char_point_data  
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LegacyCharPointData
{
    public short Mana;
    public short MaxMana;
    public short Hit;
    public short MaxHit;
    public short Move;
    public short MaxMove;
    public byte Armor;
    public int Gold;
    public int Bank;
    public int Experience;
    public sbyte Hitroll;
    public sbyte Damroll;
}

/// <summary>
/// Character skill data matching original struct char_skill_data
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LegacyCharSkillData
{
    public byte Learned;      // Skill percentage (0-100)
    public byte Recognizep;   // Recognition percentage
}

/// <summary>
/// Affected type data matching original struct affected_type
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LegacyAffectedType
{
    public short Type;        // Spell/skill type
    public short Duration;    // Duration in MUD time
    public sbyte Modifier;    // Modifier value
    public byte Location;     // Where the affect applies
    public long Bitvector;    // Bitvector for flags
    public long Bitvector2;   // Additional bitvector
    public long Bitvector3;   // Additional bitvector
    public long Bitvector4;   // Additional bitvector
}

/// <summary>
/// Clan data matching original struct clan_data
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LegacyClanData
{
    public int Number;        // Clan number
    public byte Rank;         // Player's rank in clan
    public byte Flags1;       // Clan flags
}