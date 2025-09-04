# C3Mud World Data

This directory contains the legacy MUD world data files migrated from the original Crimson-2-MUD implementation.

## Directory Structure

### `World/Areas/` - Room Definition Files (.wld)
- **214 files** containing room descriptions, exits, and properties
- **Format**: CircleMUD/DikuMUD legacy format with tilde delimiters
- **Content**: ~2,000+ rooms across all game areas
- **Example**: `15Rooms.wld` contains 3 interconnected rooms for testing

### `World/Mobiles/` - NPC/Monster Definition Files (.mob)  
- **213 files** containing NPC and monster data
- **Format**: Mobile stats, combat properties, AI behaviors, special attacks
- **Content**: ~1,500+ unique creatures and NPCs
- **Example**: `Aerie.mob` contains complex high-level monsters

### `World/Objects/` - Item Definition Files (.obj)
- **213 files** containing weapons, armor, and miscellaneous items  
- **Format**: Item properties, stat bonuses, special effects
- **Content**: ~1,000+ unique objects and equipment pieces
- **Example**: `Aerie.obj` contains magical weapons and artifacts

### `World/Zones/` - Zone Metadata Files (.zon)
- **213 files** containing area configuration and reset commands
- **Format**: Zone properties, repop timers, mobile/object spawn commands
- **Content**: ~100+ distinct game zones/areas
- **Purpose**: Controls how areas reset and repopulate with creatures/items

## Data Integrity

These files are **exact copies** of the original legacy MUD data, preserving:
- All room descriptions and connections
- Complete NPC stats and behaviors  
- Full item properties and bonuses
- Zone reset mechanisms and timing

**Total Data**: ~853 files, ~9.3MB of game content representing 20+ years of MUD development.

## Usage

The C# WorldLoader service reads these files at startup:
```csharp
var result = await worldLoader.LoadWorldFromDirectoryAsync("Data/World/");
```

Files are parsed into modern C# objects while preserving 100% behavioral compatibility with the original MUD.