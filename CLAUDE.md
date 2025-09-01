# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a C# rewrite of Crimson-2-MUD, originally a DikuMUD/CircleMUD-based MUD (Multi-User Dungeon) written in C. The goal is to modernize the legacy C codebase while preserving the game mechanics and world data.

## Original Codebase Reference

The legacy C source code is preserved in `Original-Code/` for reference during the rewrite:
- `Original-Code/src/` - Original C source (30+ .c files, 20+ .h files)  
- `Original-Code/*/lib/areas/` - Game world data files (.wld, .mob, .obj, .zon)
- `Original-Code/controlscripts/` - Server management scripts

## C# Project Structure

The C# rewrite should follow modern .NET practices:
- Use .NET 8+ with modern C# features
- Implement async/await patterns for network I/O
- Use dependency injection for modular architecture
- Apply SOLID principles and clean architecture patterns

## Core Systems to Rewrite

Based on the original C architecture, implement these key systems:

### Communication & Networking
- **Original**: `comm.c` - Basic socket handling, blocking I/O
- **C# Target**: Modern async networking with TCP listeners, connection management

### Game Database & Persistence  
- **Original**: `db.c` - File-based world loading, custom player file format
- **C# Target**: Modern data persistence (consider Entity Framework, JSON, or database)

### Command Processing
- **Original**: `parser.c` - Basic command parsing and dispatch
- **C# Target**: Command pattern with reflection-based command discovery

### Game Systems
- **Combat**: `fight.c` → Modern combat engine with events
- **Magic**: `magic*.c`, `spells*.c` → Spell system with effects framework  
- **Movement**: `move.c` → Room-based movement with validation
- **Objects**: `obj*.c` → Item system with inheritance
- **Quests**: `quest.c` → Quest engine with trigger system

### World Data Migration
- Parse original .wld, .mob, .obj, .zon files into modern data structures
- Preserve all room descriptions, mobile stats, object properties, and zone resets
- Maintain compatibility with existing world content

## Development Approach

1. **Start with Core Infrastructure**: Networking, basic player connection, command processing
2. **Implement World Loading**: Parse and load legacy world files into C# objects  
3. **Add Game Systems Incrementally**: Combat, magic, movement, etc.
4. **Preserve Game Balance**: Maintain original formulas and mechanics
5. **Modern Features**: Add logging, configuration management, testing framework

## Legacy Data Preservation

- All original game world content must be preserved
- Character progression mechanics should remain consistent
- Combat formulas and spell effects should match original behavior
- Quest triggers and zone resets should function identically