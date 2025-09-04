# Original C MUD Data Models Reference

This document provides a comprehensive reference to all data structures, types, and constants from the original C codebase for the C3Mud C# rewrite project.

## Table of Contents
1. [Core Type Definitions](#core-type-definitions)
2. [Character System](#character-system)
3. [World & Room System](#world--room-system)
4. [Object System](#object-system)
5. [Combat & Magic System](#combat--magic-system)
6. [Communication & Networking](#communication--networking)
7. [Quest System](#quest-system)
8. [Clan System](#clan-system)
9. [Game Constants](#game-constants)

---

## Core Type Definitions

### Basic Types
```c
// Basic data types used throughout the MUD
typedef signed char sbyte;
typedef unsigned char ubyte;
typedef signed short int sh_int;
typedef unsigned short int ush_int;
typedef char bool;
typedef char byte;

#define INT8  unsigned char
#define INT16 unsigned short int
#define INT32 unsigned long int
```

### Time & Calendar
```c
struct time_info_data {
    sbyte hours, day, month;
    sh_int year;
};

struct time_data {
    signed long birth;          // Character age
    time_t logon;              // Last logon time
    int lastpkill;             // Time since last player kill
    int played;                // Total time played (seconds)
};

struct weather_data {
    int pressure;              // Atmospheric pressure (Mb)
    int change;                // Weather change rate/direction
    int sky;                   // Sky condition (SKY_CLOUDLESS, etc.)
    int sunlight;              // Sun condition (SUN_DARK, etc.)
    int lunar_phase;           // Moon phase (PHASE_NEW, etc.)
    int current_hour_in_phase; // Current hour in lunar phase
};
```

---

## Character System

### Core Character Structure
```c
struct char_data {
    sh_int beg_rec_id;                      // Sanity check
    sh_int nr;                             // Monster number in file
    sh_int in_room;                        // Current location
    sh_int riding;                         // Vehicle being ridden
    sh_int extra_mana;                     // Extra mana for spells
    sbyte language;                        // Language faults
    
    struct char_player_data player;        // Player-specific data
    struct char_ability_data abilities;    // Base abilities
    struct char_ability_data tmpabilities; // Current modified abilities
    struct char_ability_data bonus;        // Bonus abilities
    struct char_point_data points;         // HP/Mana/Movement points
    struct char_special_data specials;     // Special flags and data
    struct char_skill_data skills[MAX_SKILLS]; // Skill percentages
    struct affected_type *affected;        // Active spell effects
    
    struct obj_data *equipment[MAX_WEAR];  // Worn/wielded equipment
    struct obj_data *carrying;             // Inventory list
    struct descriptor_data *desc;          // Network connection (NULL for NPCs)
    
    struct char_data *next_in_room;        // Room occupant list
    struct char_data *next;                // Global character list
    struct char_data *next_fighting;       // Combat list
    struct follow_type *followers;         // Follower list
    struct char_data *master;              // Who character follows
    
    // Quest system
    int questpoints;                       // Quest points earned
    int nextquest;                         // Time until next quest
    int countdown;                         // Time to complete current quest
    int reward;                            // Quest reward amount
    struct obj_data *questobj;             // Required quest object
    struct char_data *questmob;            // Required quest target
    struct quest_data *questdata;          // Quest conversation data
    struct questsys_data *questsys;        // Advanced quest system
    
    // Other attributes
    int incognito_lvl;                     // Incognito level
    int ghost_lvl;                         // Ghost level
    int blessing;                          // Blessing status
    int nextcast;                          // Spell casting timer
    int castcount;                         // Cast time remaining
    int jailed;                            // Jail status
    sh_int screen_lines;                   // Terminal height
    sh_int end_rec_id;                     // Sanity check
};
```

### Player Data
```c
struct char_player_data {
    char *name;                    // Character name
    char *short_descr;             // Short description (for actions)
    char *long_descr;              // Long description (for look)
    char *description;             // Extra descriptions
    char *title;                   // Character title
    sbyte sex;                     // SEX_MALE, SEX_FEMALE, SEX_NEUTRAL
    sbyte class;                   // Character class
    sbyte race;                    // Character race
    sbyte level;                   // Character level
    struct time_data time;         // Time-related data
    ubyte weight;                  // Character weight
    ubyte height;                  // Character height
    char *immortal_enter;          // Poofin message
    char *immortal_exit;           // Poofout message
    struct alias_data *aliases;    // Command aliases
    sh_int start_room;             // Starting room
    sh_int private_room;           // Private room
    sbyte visible_level;           // Invisibility level
    time_t logoff_time;            // Last logoff time
    ubyte how_player_left_game;    // How player left (quit, crash, etc.)
    ubyte how_player_left_save;    // Save method used
};
```

### Abilities & Stats
```c
struct char_ability_data {
    sbyte str;      // Strength
    sbyte intel;    // Intelligence  
    sbyte wis;      // Wisdom
    sbyte dex;      // Dexterity
    sbyte con;      // Constitution
    sbyte cha;      // Charisma
};

struct char_point_data {
    int mana;           // Current mana
    int max_mana;       // Maximum mana
    int hit;            // Current hit points
    int max_hit;        // Maximum hit points
    int move;           // Current movement
    int max_move;       // Maximum movement
    sh_int armor;       // Armor class (-100 to 100)
    int gold;           // Gold carried
    int inn_gold;       // Gold in inn
    int bank_gold;      // Gold in bank
    int exp;            // Experience points
    sbyte hitroll;      // Hit roll bonus
    sbyte damroll;      // Damage roll bonus
    int fate;           // Fate points
    int stat_count;     // Stats purchased
    int stat_gold;      // Gold from stats
    int score;          // Player score
    int used_exp;       // Experience used for stats
    int fspells;        // Foreign spells learned
    int class1;         // Original class
    int class2;         // First remort class
    int class3;         // Second remort class
    int prompt;         // Prompt settings
    sbyte extra_hits;   // Extra hit points
    char filler[31];    // Padding
};
```

### Special Character Data
```c
struct char_special_data {
    struct char_data *fighting;     // Combat opponent
    struct char_data *hunting;      // Hunt target
    struct char_data *ridden_by;    // Who is riding this character
    struct char_data *mount;        // What character is riding
    struct clan_data clan;          // Clan information
    
    // Affect bitvectors
    long affected_by;               // Primary affects (AFF_*)
    long affected_02;               // Secondary affects
    long affected_03;               // Tertiary affects
    long affected_04;               // Quaternary affects
    
    sbyte position;                 // Current position (standing, sitting, etc.)
    sbyte default_pos;              // Default position (for NPCs)
    
    // Player flags
    long act;                       // Primary flags (PLR1_*)
    long act2;                      // Secondary flags (PLR2_*)
    long act3;                      // Tertiary flags (PLR3_*)
    long act4;                      // Quaternary flags (PLR4_*)
    long act5;                      // Additional flags
    long act6;                      // Additional flags
    long wiz_perm;                  // Wizard permissions
    
    // Timers
    long jailtime;                  // Jail time remaining
    long deathshadowtime;           // Death shadow time
    long godmuzzletime;             // God muzzle time
    time_t time_of_muzzle;          // When muzzle was applied
    
    ubyte spells_to_learn;          // Spells available to learn
    int carry_weight;               // Current carry weight
    sbyte carry_items;              // Number of items carried
    int timer;                      // General timer
    sh_int was_in_room;             // Previous room (for linkdead)
    sh_int apply_saving_throw[5];   // Saving throw bonuses
    signed short int conditions[3]; // Hunger, thirst, drunk
    
    // Combat data
    sbyte damnodice;                // Number of damage dice
    sbyte damsizedice;              // Size of damage dice
    sbyte last_direction;           // Last movement direction
    int attack_type;                // Attack type bitvector
    int alignment;                  // Alignment (-1000 to +1000)
};
```

### Skills & Spells
```c
struct char_skill_data {
    ubyte learned;      // Skill percentage (0-100)
};

struct affected_type {
    ubyte type;                    // Spell/skill type
    sh_int duration;               // Effect duration
    sbyte modifier;                // Ability modifier
    sbyte location;                // What to modify (APPLY_*)
    long bitvector;                // Primary affect bits (AFF_*)
    long bitvector2;               // Secondary affect bits
    long bitvector3;               // Tertiary affect bits
    long bitvector4;               // Quaternary affect bits
    struct affected_type *next;    // Next effect in list
};
```

### Races & Classes
```c
struct race_data {
    char name[20];                     // Race name
    char desc[6];                      // Short description
    char text[4][80];                  // Race description text
    int flag;                          // Race flags (RFLAG_*)
    
    // Attribute maximums
    sbyte max_str, max_int, max_wis;
    sbyte max_dex, max_con, max_cha;
    int max_hit, max_mana, max_move;
    ubyte max_food, max_thirst, max_drunk;
    
    // Base values
    int base_hit, base_mana, base_move;
    
    // Adjustments
    sbyte adj_str, adj_int, adj_wis;
    sbyte adj_dex, adj_con, adj_cha;
    int adj_hit, adj_mana, adj_move;
    sbyte adj_hitroll, adj_dmgroll;
    int adj_ac, adj_food, adj_thirst, adj_drunk;
    
    // Regeneration rates
    int regen_hit, regen_mana, regen_move;
    
    // Racial abilities
    ubyte perm_spell[MAX_RACE_ATTRIBUTES];  // Permanent spells
    sbyte skill_min_level[MAX_SKILLS];      // Min levels for skills
    sbyte skill_max[MAX_SKILLS];            // Max skill percentages
} races[MAX_RACES];

struct class_data {
    char name[20];                     // Class name
    char desc[5];                      // Short description
    char text[4][80];                  // Class description text
    ubyte thaco_numerator;             // THAC0 calculation
    ubyte thaco_denominator;           // THAC0 calculation
    int flag;                          // Class flags (CFLAG_*)
    sbyte skill_min_level[MAX_SKILLS]; // Min levels for skills
    sbyte skill_max[MAX_SKILLS];       // Max skill percentages
    ubyte adj_hit;                     // Hit point adjustment
    ubyte adj_mana;                    // Mana adjustment
    ubyte adj_move;                    // Movement adjustment
} classes[MAX_CLASSES];
```

---

## World & Room System

### Room Structure
```c
struct room_data {
    sh_int number;                          // Room vnum
    sh_int zone;                           // Zone number
    int sector_type;                       // Terrain type (SECT_*)
    char *name;                            // Room name
    char *description;                     // Room description
    struct extra_descr_data *ex_description; // Extra descriptions
    struct room_direction_data *dir_option[6]; // Exits (N,E,S,W,U,D)
    unsigned long room_flags;              // Room flags (RM1_*)
    unsigned long room_flags2;             // Additional room flags (RM2_*)
    sbyte light;                           // Light level
    sbyte min_level;                       // Minimum level to enter
    sbyte max_level;                       // Maximum level to enter
    int (*funct)();                        // Special procedure function
    ush_int next;                          // Used for zone dirty list
    struct obj_data *contents;             // Objects in room
    struct char_data *people;              // Characters in room
    struct questsys_data *questsys;        // Quest system data
};

struct room_direction_data {
    char *general_description;  // Exit description
    char *keyword;             // Door keyword
    sh_int exit_info;          // Exit flags (EX_*)
    sh_int key;                // Key vnum (-1 for no key)
    sh_int to_room;            // Destination room
};

struct extra_descr_data {
    char *keyword;                     // Keywords for examination
    char *description;                 // Description text
    struct extra_descr_data *next;     // Next description
};
```

### Zone System
```c
struct zone_data {
    char *name;                    // Zone name
    int lifespan;                  // Zone reset time
    int age;                       // Current age
    int top;                       // Highest room vnum
    int reset_mode;                // Reset mode (0=never, 1=empty, 2=always)
    struct reset_com *cmd;         // Reset command list
    int number;                    // Zone number
    int builders;                  // Builder permissions
    int locked;                    // Zone lock status
    long flags;                    // Zone flags (ZONE_*)
};

struct reset_com {
    char command;          // Reset command type (M,O,P,E,D,R)
    bool if_flag;          // Conditional execution
    int arg1, arg2, arg3;  // Command arguments
    int line;              // Line number in zone file
    
    // Command types:
    // M = Load mobile
    // O = Load object  
    // P = Put object in object
    // E = Equip object on mobile
    // D = Set door state
    // R = Remove object
};
```

---

## Object System

### Core Object Structure
```c
struct obj_data {
    sh_int item_number;                    // Object vnum
    sh_int in_room;                        // Room location (-1 if carried)
    struct obj_flag_data obj_flags;        // Object properties
    struct obj_affected_type affected[MAX_OBJ_AFFECT]; // Stat modifiers
    
    char *name;                            // Object keywords
    char *description;                     // Room description
    char *short_description;               // Inventory description
    char *action_description;              // Use message
    struct extra_descr_data *ex_description; // Extra descriptions
    
    struct char_data *carried_by;          // Carrier (NULL if not carried)
    struct obj_data *in_obj;               // Container (NULL if not contained)
    struct obj_data *contains;             // Contents list
    struct obj_data *next_content;         // Next in contents list
    struct obj_data *next;                 // Next in global object list
    
    struct questsys_data *questsys;        // Quest system data
};

struct obj_flag_data {
    int value[4];           // Object values (type-specific)
    sbyte type_flag;        // Object type (ITEM_*)
    int wear_flags;         // Where it can be worn (ITEM_WEAR_*)
    int flags1;             // Object flags (OBJ1_*)
    int flags2;             // Additional flags (OBJ2_*)
    int weight;             // Object weight
    int cost;               // Base cost in gold
    int cost_per_day;       // Rent cost per day
    int timer;              // Timer (for decay, etc.)
    long bitvector;         // Character affects when worn
};

struct obj_affected_type {
    sbyte location;         // What to modify (APPLY_*)
    sbyte modifier;         // Modification amount
};
```

### Object Index
```c
struct obj_index_data {
    sh_int vnum;                           // Virtual number
    int number;                            // Number in world
    int (*func)();                         // Special procedure
    char *name, *short_description;        // Basic strings
    char *description, *action_description; // Descriptions
    struct extra_descr_data *ex_description; // Extra descriptions
    struct obj_flag_data obj_flags;        // Object flags and values
    struct obj_affected_type affected[MAX_OBJ_AFFECT]; // Affects
    struct questsys_data *questsys;        // Quest data
};
```

### Vehicles
```c
struct vehicle_data {
    struct obj_data *obj;       // Vehicle object
    sh_int room;                // Room used as vehicle interior
    struct vehicle_data *next;  // Next in vehicle list
};
```

---

## Combat & Magic System

### Combat Messages
```c
struct msg_type {
    char *attacker_msg;     // Message to attacker
    char *victim_msg;       // Message to victim  
    char *room_msg;         // Message to room
};

struct message_type {
    struct msg_type die_msg;        // Death messages
    struct msg_type miss_msg;       // Miss messages
    struct msg_type hit_msg;        // Hit messages
    struct msg_type sanctuary_msg;  // Sanctuary hit messages
    struct msg_type god_msg;        // God mode messages
    struct message_type *next;      // Next message set
};

struct message_list {
    int a_type;                     // Attack type
    int number_of_attacks;          // Number of message sets
    struct message_type *msg;       // Message list
};
```

### Spell Information
```c
struct spell_info_type {
    byte min_position;      // Minimum position to cast
    int mana_min;          // Minimum mana cost
    int mana_max;          // Maximum mana cost  
    int mana_change;       // Mana cost change per level
    int min_level[MAX_CLASSES]; // Minimum level per class
    int routines;          // Spell routines (TARGET_*, etc.)
    int targets;           // Valid targets
    int violent;           // Is spell violent?
    char *name;            // Spell name
};

struct attack_hit_type {
    char *singular;        // Singular attack message
    char *plural;          // Plural attack message  
};
```

---

## Communication & Networking

### Network Connection
```c
struct descriptor_data {
    int descriptor;                    // Socket file descriptor
    char host[50];                     // Hostname
    char pwd[12];                      // Password
    int pos;                           // Position in player file
    int connected;                     // Connection state (CON_*)
    int connected_at;                  // Connection time
    int admin_state;                   // Admin/edit state
    int old_state;                     // Previous state
    int wait;                          // Wait loops
    
    // Text display system
    char *showstr_head;                // Text paging head
    char **showstr_vector;             // Text paging vector
    int showstr_count;                 // Text count
    int showstr_page;                  // Current page
    char **str;                        // String editing
    int max_str;                       // Max string length
    
    // Input/output
    int has_prompt;                    // Prompt flag
    int prompt_mode;                   // Prompt mode
    int prompt_cr;                     // Carriage return flag
    char curr_input[MAX_INPUT_LENGTH]; // Current input
    char last_input[MAX_INPUT_LENGTH]; // Previous input
    char **history;                    // Command history
    int history_pos;                   // History position
    struct txt_q input;                // Input queue
    
    // Character association
    struct char_data *character;       // Associated character
    struct char_data *original;        // Original character (for switch)
    char *reply_to;                    // Tell reply target
    char *ignore[20];                  // Ignore list
    struct snoop_data snoop;           // Snooping data
    
    // Admin/building
    struct char_data *admin;           // Admin target
    struct char_data *last_target;     // Last target
    long last_hit_time;                // Last hit time
    struct obj_data *admin_obj;        // Admin object
    int admin_zone;                    // Admin zone
    
    // Mail system
    int mailtoclan;                    // Clan mail flag
    int mailedit;                      // Mail edit flag
    int mailto;                        // Mail target
    
    // Output buffer system
    char small_outbuf[SMALL_BUFSIZE];  // Small output buffer
    int bufptr;                        // Buffer pointer
    int bufspace;                      // Buffer space
    struct txt_block *large_outbuf;    // Large output buffer
    char *output;                      // Output pointer
    
    struct descriptor_data *next;      // Next descriptor
    int tmp_count;                     // Temporary counter
    int autologin;                     // Auto-login flag
};

struct txt_block {
    unsigned char beg_rec_id;   // Sanity check
    char *text;                 // Text content
    struct txt_block *next;     // Next block
    int aliased;                // Alias flag
    unsigned char end_rec_id;   // Sanity check
};

struct txt_q {
    struct txt_block *head;     // Queue head
    struct txt_block *tail;     // Queue tail
};
```

### Command System
```c
struct command_info {
    char *command;              // Command name
    byte minimum_position;      // Minimum position required
    void (*command_pointer)();  // Function pointer
    sh_int minimum_level;       // Minimum level required
    int subcmd;                 // Sub-command identifier
};
```

---

## Quest System

### Quest Data Structures
```c
struct questsys_data {
    struct char_data *ch;       // Quest owning mobile
    struct obj_data *obj;       // Quest owning object
    struct room_data *room;     // Quest owning room
    int sysregs[MAX_QUEST_SYSREGS];     // System registers
    int sysregs_types[MAX_QUEST_SYSREGS]; // System register types
    int regs[MAX_QUEST_REGS];   // User registers
    int regs_types[MAX_QUEST_REGS]; // User register types
    int userregs;               // Number of user registers
    struct questsys_reg *userreg; // User register data
    struct questsys_script *script; // Attached scripts
};

struct questsys_script {
    int mob_vnum;               // Mobile vnum
    char *description;          // Script description
    char *triggers[MAX_QUEST_TRIGGERS]; // Trigger scripts
};

struct questsys_reg {
    int value;                  // Register value
    int rettype;                // Return type
    char *name;                 // Register name
};

struct quest_data {
    int vmob;                   // Quest mobile vnum
    struct conversation_data *convo; // Conversation data
};

struct conversation_data {
    int number;                 // Conversation number
    sbyte low;                  // Low level range
    sbyte high;                 // High level range
    struct keyword_data *keywords; // Keywords
    char *reply;                // Reply text
    char *command;              // Command to execute
    struct conversation_data *next; // Next conversation
};

struct keyword_data {
    char *keyword;              // Keyword text
    struct keyword_data *next;  // Next keyword
};
```

---

## Clan System

### Clan Data Structures
```c
struct clans_data {
    int number;                             // Clan number
    sbyte enabled;                          // Enabled flag
    char name[MAX_CLAN_STR_LENGTH];         // Clan name
    char leader[20];                        // Leader name
    int room;                               // Clan hall room
    int don_room;                           // Donation room
    int board_num;                          // Board vnum
    char color[20];                         // Clan color
    char rank[MAX_CLAN_RANKS + 1][MAX_CLAN_STR_LENGTH]; // Rank names
    char desc[MAX_CLAN_DESC][MAX_CLAN_STR_LENGTH]; // Clan description
    struct clan_war_data *war;              // War data
};

struct clan_data {
    ubyte number;               // Clan number
    ubyte rank;                 // Member rank
    ubyte flags1;               // Clan flags
};

struct clan_war_data {
    long enemy_clan;            // Enemy clan number
    long start_time;            // War start time
    long attack_time;           // Last attack time
    long kill_time;             // Last kill time
    long attacks;               // Attack count
    long kills;                 // Kill count
    long deaths;                // Death count
    long escapes;               // Escape count
    struct clan_war_data *next; // Next war
};
```

---

## Game Constants

### Character Classes
```c
#define CLASS_MAGIC_USER    1
#define CLASS_CLERIC        2
#define CLASS_THIEF         3
#define CLASS_WARRIOR       4
#define CLASS_BARD          5
#define CLASS_PRIEST        6
#define CLASS_PALADIN       7
#define CLASS_DRUID         8
#define CLASS_ELDRITCH      9
#define CLASS_MONK          10
#define CLASS_RANGER        13
#define CLASS_NECROMANCER   15
#define CLASS_CHAOSKNIGHT   20
```

### Character Races
```c
#define RACE_HUMAN          1
#define RACE_DWARF          2
#define RACE_GNOME          3
#define RACE_HALFLING       4
#define RACE_HALF_GIANT     5
#define RACE_PIXIE          6
#define RACE_DEMON          7   // Must stay evil
#define RACE_SNOTLING       8
#define RACE_FELINE         9
#define RACE_TROLL          10
// ... (continues through RACE_WEMIC = 36)
```

### Object Types
```c
#define ITEM_LIGHT          1
#define ITEM_SCROLL         2
#define ITEM_WAND           3
#define ITEM_STAFF          4
#define ITEM_WEAPON         5
#define ITEM_FURNITURE      6
#define ITEM_MISSILE        7
#define ITEM_TREASURE       8
#define ITEM_ARMOR          9
#define ITEM_POTION         10
// ... (continues through ITEM_CORPSE = 31)
```

### Wear Locations
```c
#define WEAR_LIGHT          0
#define WEAR_FINGER_R       1
#define WEAR_FINGER_L       2
#define WEAR_NECK_1         3
#define WEAR_NECK_2         4
#define WEAR_BODY           5
#define WEAR_HEAD           6
#define WEAR_LEGS           7
#define WEAR_FEET           8
#define WEAR_HANDS          9
#define WEAR_ARMS           10
#define WEAR_SHIELD         11
#define WEAR_ABOUT          12
#define WEAR_WAISTE         13
#define WEAR_WRIST_R        14
#define WEAR_WRIST_L        15
#define WIELD               16
#define HOLD                17
#define WEAR_TAIL           18
#define WEAR_4LEGS_1        19
#define WEAR_4LEGS_2        20
```

### Positions
```c
#define POSITION_DEAD       0
#define POSITION_MORTALLYW  1
#define POSITION_INCAP      2
#define POSITION_STUNNED    3
#define POSITION_SLEEPING   4
#define POSITION_RESTING    5
#define POSITION_SITTING    6
#define POSITION_FIGHTING   7
#define POSITION_STANDING   8
```

### Directions
```c
#define NORTH               0
#define EAST                1
#define SOUTH               2
#define WEST                3
#define UP                  4
#define DOWN                5
```

### Sector Types
```c
#define SECT_INSIDE         0
#define SECT_CITY           1
#define SECT_FIELD          2
#define SECT_FOREST         3
#define SECT_HILLS          4
#define SECT_MOUNTAIN       5
#define SECT_WATER_SWIM     6
#define SECT_WATER_NOSWIM   7
#define SECT_UNDERWATER     8
```

### Time & Weather Constants
```c
// Sun states
#define SUN_DARK            0
#define SUN_RISE            1
#define SUN_LIGHT           2
#define SUN_SET             3

// Sky conditions
#define SKY_CLOUDLESS       0
#define SKY_CLOUDY          1
#define SKY_RAINING         2
#define SKY_LIGHTNING       3

// Lunar phases
#define PHASE_NEW           0
#define PHASE_CRESCENT_1    1
#define PHASE_FIRST_QUARTER 2
#define PHASE_WAXING_GIBBOUS 3
#define PHASE_FULL_MOON     4
#define PHASE_WANING_GIBBOUS 5
#define PHASE_LAST_QUARTER  6
#define PHASE_CRESCENT_2    7
```

### Pulse & Timer Constants
```c
#define PULSE_ZONE          960
#define PULSE_OBJECTS       48
#define PULSE_MOBILE        160
#define PULSE_HUNT          48
#define PULSE_VIOLENCE      48
#define PULSE_ESCAPE        32
#define PULSE_VEHICLE       48
#define WAIT_SEC            16
#define WAIT_ROUND          16
#define AUTOSAVE_DELAY      1500    // seconds
```

---

This reference document provides the foundation for implementing equivalent C# data structures while preserving the original game mechanics and data relationships. Each structure should be carefully translated to use modern C# patterns while maintaining the same logical relationships and data storage requirements.