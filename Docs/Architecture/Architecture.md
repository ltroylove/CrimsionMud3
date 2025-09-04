# C3Mud Architecture Document

## Executive Summary

C3Mud employs a streamlined Clean Architecture approach specifically tailored for a small-scale Multi-User Dungeon (MUD) supporting fewer than 100 concurrent players. The architecture prioritizes maintainability, testability, and preservation of legacy game mechanics while avoiding over-engineering through careful application of 2024's modern C# patterns and game development best practices.

Built on .NET 8+ with modern async/await networking patterns, the system uses simplified layered architecture with clear separation of concerns, minimal abstractions, and efficient real-time communication. The design embraces modern C# language features (records, pattern matching, source generators) while maintaining the practical needs of classic MUD gameplay.

## Core Architectural Principles

### Clean Architecture Foundation
- **Dependency Inversion**: Dependencies flow inward toward the domain core, keeping game logic independent of external concerns
- **Separation of Concerns**: Each layer has distinct responsibilities with minimal overlap
- **Testability**: Business logic can be tested independently of networking, persistence, and presentation layers
- **Flexibility**: External systems (databases, networking) can be replaced without affecting core game logic

### SOLID Principles Application
- **Single Responsibility**: Each class focuses on a single aspect of MUD functionality
- **Open/Closed**: Game systems are extensible through composition rather than modification
- **Liskov Substitution**: Inheritance hierarchies (players, mobiles, objects) maintain behavioral consistency  
- **Interface Segregation**: Components depend only on interfaces they actually use
- **Dependency Inversion**: High-level game systems don't depend on low-level implementation details

### MUD-Specific Design Considerations
- **Legacy Compatibility**: Architecture preserves original game mechanics and data formats
- **Real-time Responsiveness**: Modern async patterns with ValueTask and Channels for performance
- **State Management**: Clear ownership patterns with deterministic behavior for game consistency
- **Component-Based Design**: Modular game object behavior through composition over inheritance
- **Data-Oriented Performance**: Hot-path optimizations using Span<T> and object pooling

## Layer Architecture

### Domain Layer (Core)
The innermost layer containing pure business logic with no external dependencies.

**Components:**
- **Entities**: Core game objects (Player, Mobile, Room, GameObject)
- **Value Objects**: Immutable record types (Coordinates, Stats, Money, GameTime)
- **Components**: Modular behavior units (HealthComponent, SpellComponent, InventoryComponent)
- **Domain Services**: Core game mechanics (CombatEngine, MovementValidator, SpellCaster)
- **Game Events**: Simple event notifications using System.Threading.Channels
- **Repositories (Interfaces)**: Minimal data access contracts for persistence

**Key Characteristics:**
- No dependencies on external frameworks
- Leverages modern C# records for immutable value objects
- Component-based entity design for flexible game object behavior
- Domain services use static methods where possible for performance
- Rich domain model with behavior encapsulation

### Application Layer (Use Cases)
Orchestrates domain entities to fulfill specific use cases, handling application flow and coordination.

**Components:**
- **Game Services**: Direct service classes for game operations (PlayerService, CombatService, WorldService)
- **Command Processors**: Pattern-matched command handling with source-generated parsers
- **State Managers**: Coordinate complex multi-entity operations (combat rounds, spell effects)
- **Result Types**: Discriminated unions using modern C# pattern matching
- **Validators**: Compile-time validation using source generators where applicable
- **Event Channels**: High-performance communication using System.Threading.Channels

**Communication Patterns:**
- **Direct Service Calls**: Simplified over complex mediator patterns for small scale
- **Channel-Based Events**: High-performance async communication for real-time updates
- **Result<T> Pattern**: Error handling without exceptions using discriminated unions
- **Source Generation**: Compile-time code generation for command parsing and serialization

### Infrastructure Layer (External Concerns)
Handles all external system interactions and technical implementations.

**Networking Module:**
- **TCP Server**: Modern async socket management using System.IO.Pipelines for zero-copy I/O
- **Protocol Handlers**: Efficient Span<T>-based parsing for telnet and ANSI codes
- **Connection Management**: Lightweight session tracking with ValueTask for hot paths
- **Buffer Management**: ArrayPool<T> for memory-efficient temporary allocations
- **Security**: Channel-based rate limiting and connection throttling

**Persistence Module:**
- **World Data Parsers**: Source-generated parsers for legacy file formats (.wld, .mob, .obj, .zon)
- **Player Repository**: System.Text.Json with modern serialization and nullable reference types
- **Caching Strategy**: Memory<T>-based world data with efficient zone loading
- **Configuration**: IOptions<T> pattern with hot-reload and nullable annotations

**Logging & Monitoring:**
- **Structured Logging**: Microsoft.Extensions.Logging with high-performance LoggerMessage.Define
- **Telemetry**: System.Diagnostics.Metrics for modern observability
- **Error Handling**: Global exception handling with structured context preservation

### Presentation Layer (Interface)
Manages all player interactions and external communications.

**Components:**
- **Command Parser**: Source-generated parsers with pattern matching for efficient command recognition
- **Output Formatter**: Span<T>-based ANSI color processing and StringBuilder pooling
- **Session Manager**: Lightweight player state with record-based preferences
- **Administrative Interface**: Minimal API endpoints for modern web-based administration
- **Hot-Reload Support**: Development-time live content updates for rapid iteration

## System Components Architecture

### Networking System
**Design Pattern**: Modern Async with System.IO.Pipelines
```
TcpListener → PipeReader/Writer → ConnectionHandler → CommandChannel
```

**Key Features:**
- High-performance I/O using System.IO.Pipelines for zero-copy operations
- Channel-based communication between networking and game layers
- ValueTask for hot paths with potential synchronous completion
- ArrayPool<T> for efficient temporary buffer management

**Implementation Approach:**
- Direct async/await without complex abstraction layers
- Span<T> and Memory<T> for efficient string processing
- Channel<T> for high-throughput command queuing
- Minimal allocations through object pooling patterns

### Command Processing System
**Design Pattern**: Source-Generated Command Processing
```
Input → Pattern Matching → Source-Generated Parser → Direct Service Call → Channel Response
```

**Architecture Benefits:**
- Compile-time command registration using source generators
- Pattern matching for efficient command recognition and parameter extraction
- Direct service method calls instead of complex mediator patterns
- Channel-based response queuing for non-blocking output

**Modern C# Features:**
- Source generators for zero-runtime-cost command discovery
- Pattern matching with when clauses for context-aware parsing
- Record types for immutable command parameters
- Nullable reference types for safer parameter handling

**Legacy Integration:**
- Command alias mapping preserves original MUD syntax
- Pattern matching supports flexible command variations
- Gradual migration with both legacy and modern command patterns supported

### Game State Management
**Design Pattern**: Component-Based Entity System
```
GameWorld → EntityManager → ComponentStore → SystemUpdater
```

**State Architecture:**
- Entity-Component-System (ECS) for flexible game object composition
- Component stores using efficient data structures (Dictionary<int, T> keyed by entity ID)
- Direct state mutation with channel-based change notifications
- Deterministic systems for consistent game behavior

**Performance Optimizations:**
- Component data stored in contiguous arrays for cache efficiency
- Lazy loading of inactive world regions with weak references
- Object pooling for frequently created/destroyed entities (projectiles, effects)
- Span<T> operations for batch component updates

### Combat & Game Systems
**Design Pattern**: Component-Based Systems with Data-Oriented Design
```
CombatSystem → ComponentQuery → DamageCalculation → EffectApplication → ChannelNotification
```

**System Characteristics:**
- Component-based combat using health, weapon, armor, and skill components
- Data-oriented calculations using Span<T> for batch processing multiple combatants
- Deterministic random number generation using System.Security.Cryptography
- ValueTask-based async processing for non-blocking combat rounds

**Modern Implementation:**
- System.Threading.Channels for high-performance combat event distribution  
- Record types for immutable combat results and damage calculations
- Source-generated serialization for combat log persistence
- Pattern matching for complex combat rules and special cases

**Legacy Preservation:**
- Identical damage formulas implemented as pure static methods
- Original skill/spell progression curves maintained in configuration
- Equipment stat bonuses preserved through component composition

## Data Flow & Communication Patterns

### Inbound Request Flow
```
PipeReader → Pattern Matching → Service Method → Component Update → Channel Broadcast → PipeWriter
```

### Channel-Based Communication
**Game Event Channels:**
- `Channel<PlayerEvent>` for connection/disconnection notifications
- `Channel<MovementEvent>` for room changes and proximity updates
- `Channel<CombatEvent>` for damage, healing, and combat state changes
- `Channel<SystemEvent>` for spells, skills, and environmental effects

**Channel Benefits:**
- High-performance async communication with backpressure handling
- Type-safe event distribution without reflection overhead
- Natural flow control preventing system overload
- Error isolation with channel-specific error handling

### State Change Propagation
```
Component Update → Change Detection → Channel Writer → Async Handlers → Client Notifications
```

**Modern Implementation Examples:**
- Player movement updates room occupancy using efficient set operations
- Combat damage uses batch component updates with Span<T> for multiple targets
- Spell effects apply through component composition with deterministic ordering
- Experience awards calculated using pure functions with immutable result records

## Technology Stack

### Core Framework
- **.NET 8+**: Latest LTS with modern C# 12 features (primary constructors, collection expressions)
- **Minimal APIs**: Lightweight administrative endpoints without full MVC overhead
- **System.Text.Json**: Source-generated serializers for zero-reflection performance
- **Nullable Reference Types**: Full nullable context enabled for safer code

### Modern C# Features
- **Source Generators**: Compile-time code generation for commands, serialization, and validation
- **Pattern Matching**: Advanced pattern matching for command parsing and game logic
- **Record Types**: Immutable data structures for value objects and DTOs
- **Span<T> & Memory<T>**: Zero-copy string and buffer operations
- **ValueTask**: Optimized async returns for hot paths with fast synchronous completion

### Networking & I/O
- **System.IO.Pipelines**: Zero-copy I/O with PipeReader/PipeWriter for maximum throughput
- **System.Threading.Channels**: High-performance async communication between systems
- **ArrayPool<T>**: Efficient memory management for temporary allocations
- **Microsoft.Extensions.Hosting**: Background service hosting with graceful shutdown

### Data & Persistence  
- **System.Text.Json**: Source-generated serialization with modern nullable support
- **Memory<T>**: Efficient world data caching with minimal allocations
- **Source-Generated Parsers**: Compile-time parsers for legacy world file formats
- **IMemoryCache**: Built-in high-performance in-memory caching

### Observability & Configuration
- **Microsoft.Extensions.Logging**: High-performance LoggerMessage.Define for structured logging
- **System.Diagnostics.Metrics**: Modern telemetry and observability
- **IOptions<T>**: Strongly-typed configuration with hot-reload support
- **Global Exception Handling**: Centralized error handling with context preservation

### Testing & Quality
- **xUnit**: Modern async test support with source-generated test data
- **NSubstitute**: Minimal mocking framework preferred over Moq
- **FluentAssertions**: Expressive assertions with async support
- **Microsoft.Extensions.Time.Testing**: Time abstraction for deterministic testing

### Development Experience
- **Hot Reload**: Live content updates during development
- **Source Link**: Enhanced debugging with decompiled source support
- **EditorConfig**: Consistent code formatting and style enforcement
- **Central Package Management**: Directory.Packages.props for unified dependency management

## Scalability Considerations

### Right-Sized Architecture
**Player Capacity Planning:**
- Target: <100 concurrent players with headroom for 200+ during peak events
- Architecture: Single-process with async I/O and efficient memory patterns
- Persistence: File-based JSON with Memory<T> caching (no database complexity)
- Monitoring: System.Diagnostics.Metrics with minimal overhead

### Performance Optimization
**Memory Efficiency:**
- Component-based ECS with efficient data layout for cache performance
- ArrayPool<T> and ObjectPool<T> for frequently allocated temporary objects
- Weak references for inactive world regions with automatic cleanup
- Zero-allocation hot paths using Span<T> and stackalloc

**Network Performance:**
- System.IO.Pipelines for zero-copy I/O operations
- Channel-based message queuing with backpressure handling
- Span<T>-based ANSI processing without string allocations
- Source-generated serialization eliminating reflection costs

**CPU Optimization:**
- Data-oriented design with batch processing using Span<T>
- Pattern matching compiled to efficient jump tables
- Source-generated command parsing eliminating runtime reflection
- Deterministic algorithms with minimal branching for combat calculations

### Growth Path
**Modern Scaling Options:**
- Component-based world partitioning enables natural zone-based distribution
- Channel-based communication facilitates inter-process coordination if needed
- Source-generated serialization supports efficient network protocols for clustering
- Stateless service methods enable easy horizontal replication

**Performance Characteristics:**
- Current architecture efficient to ~500+ concurrent players with modern patterns
- Memory usage optimized through component pooling and weak reference cleanup
- CPU usage dominated by efficient batch operations rather than per-player overhead
- Network throughput maximized through pipeline-based zero-copy operations

## Integration with Legacy Systems

### Data Migration Strategy
**World Data Preservation:**
```
Legacy Files (.wld, .mob, .obj, .zon) → Source-Generated Parsers → Record Types → Memory<T> Cache → Component Entities
```

**Migration Phases:**
1. **Source Generator Development**: Compile-time parsers with full validation and error reporting
2. **Record-Based Validation**: Immutable record types enable comprehensive data integrity checking  
3. **Performance Validation**: Memory<T>-based caching ensures sub-second world loading
4. **Component Migration**: Legacy data mapped to modern component-based entities

### Compatibility Guarantees
**Mechanical Preservation:**
- Combat formulas implemented as pure static methods with identical results
- Experience curves and skill progression maintained through immutable configuration records
- Spell effects preserved using component-based composition with deterministic ordering
- Equipment bonuses applied through component systems maintaining exact statistical effects

**Content Preservation:**
- Room descriptions and connections stored as immutable record types
- Mobile behavior patterns converted to component-based AI systems
- Object properties maintained through flexible component composition
- Zone reset logic implemented using modern C# pattern matching and async workflows

### Player Data Migration
**Character Transfer:**
- Source-generated player file parsers with comprehensive validation
- Gradual migration using discriminated union types for old/new format support
- Data integrity verification through record-based immutable snapshots
- Character history preserved using append-only event patterns

## Security & Administrative Considerations

### Modern Security Measures
- **Input Validation**: Span<T>-based parsing with source-generated validators for zero-allocation validation
- **Rate Limiting**: Channel-based throttling with configurable per-player command queues
- **Connection Security**: IP-based limits with efficient HashSet lookups and automatic cleanup
- **Data Integrity**: Source-generated validation ensuring world data consistency at compile-time

### Administrative Features
- **Live Configuration**: IOptionsMonitor<T> with hot-reload using modern options pattern
- **Web Admin Interface**: Minimal API endpoints for lightweight browser-based administration
- **Real-time Monitoring**: System.Diagnostics.Metrics with low-overhead telemetry collection
- **Backup Strategy**: Incremental JSON snapshots with immutable record-based player data

### Modern Deployment Strategy
- **Self-Contained Binary**: Single-file deployment with native AOT compilation for optimal startup
- **Configuration**: Strongly-typed IOptions<T> with nullable reference types and validation
- **Graceful Shutdown**: IHostApplicationLifetime integration with player notification channels
- **Observability**: OpenTelemetry-compatible metrics and structured logging for modern monitoring

## Development Experience Enhancements

### Hot-Reload Capabilities
- **Content Updates**: Live world data changes during development without server restart
- **Command Development**: Source generator refresh enables new commands without compilation delays
- **Configuration Changes**: Hot-reload of game parameters for rapid balance iteration

### Modern Debugging
- **Source Link**: Direct-to-source debugging even for NuGet dependencies
- **Nullable Analysis**: Compile-time null safety reducing runtime errors
- **Pattern Matching**: Enhanced debugger support for complex game state inspection
- **Component Inspection**: ECS debugging tools for runtime component state visualization

This architecture represents a thoroughly modern approach to MUD development, leveraging 2024's best practices in C# development while maintaining the essential character of classic MUD gameplay. The design prioritizes developer productivity, runtime performance, and long-term maintainability through careful application of modern language features and design patterns appropriate for small-scale real-time systems.