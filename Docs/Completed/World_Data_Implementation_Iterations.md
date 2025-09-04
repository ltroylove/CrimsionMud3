# World Data Integration - TDD Implementation Plan
## C3Mud Legacy World File Integration - Daily Iteration Breakdown

**Created:** September 1, 2025  
**Project:** C3Mud - Modern C# MUD Implementation  
**Based On:** World_Data_Integration_Plan.md  
**Methodology:** Test-Driven Development with Daily Iterations  
**Total Duration:** 14 days (maximum 1 day per iteration)

---

## Pre-Implementation Analysis

### Current File Inventory
- **World Files (.wld)**: 215 files containing room definitions
- **Mobile Files (.mob)**: 214 files with NPC/monster data  
- **Object Files (.obj)**: 214 files with item definitions
- **Zone Files (.zon)**: 214 files with area metadata and resets
- **Total Data Volume**: ~2,000+ rooms, ~1,500+ mobiles, ~1,000+ objects

### File Format Analysis
- **CircleMUD/DikuMUD Legacy Format**: Tilde-delimited ASCII text
- **Sample Room Format**: VNum, name~, description~, flags, exits with D<dir> syntax
- **Sample Mobile Format**: VNum, keywords~, descriptions~, stats, skills, special attacks
- **Complexity**: Variable record lengths, embedded color codes, special syntax

### Current Codebase State
- ✅ Modern C# networking foundation established (Iteration 1)
- ✅ Command processing system with TDD approach
- ✅ Player management and authentication systems
- ❌ **MISSING**: World data integration (commands show placeholder data)
- ❌ **MISSING**: Room/Mobile/Object data models
- ❌ **MISSING**: World file parsers

## Dependency Mapping

**Critical Dependencies:**
1. **File Parsers** → **Data Models** → **World Database** → **Command Integration**
2. **Room Parsing** must come first (simplest format, needed for movement)
3. **Mobile/Object Parsing** can be parallel after Room foundation
4. **Performance optimization** requires complete data loading capability

**Integration Dependencies:**
- Commands depend on World Database
- Movement commands depend on Room connections
- Player location tracking depends on Room system
- Object interaction depends on Object parsing

## Risk Assessment

### High Risk (Address First)
1. **Data Loss/Corruption**: 20+ years of legacy world content - 100% preservation critical
2. **Performance Degradation**: 215 files, 2000+ rooms could cause memory/loading issues
3. **Format Edge Cases**: Legacy files may have inconsistencies or special syntax

### Medium Risk
1. **Integration Complexity**: Existing commands must seamlessly work with real data
2. **Legacy Compatibility**: Original MUD behavior must be preserved exactly

---

## Detailed TDD Iteration Breakdown

### Phase 1: File Format Foundation (Days 1-3)

#### Iteration 1.1: Room File Parser Core (Day 1 - 6 hours)
**Goal**: Parse basic room data from simplest .wld files

**TDD Red Phase - Tests First**:
```csharp
[TestClass]
public class WorldFileParserTests
{
    [TestMethod]
    public void ParseRoom_BasicFormat_ReturnsCorrectVNum()
    {
        // Test parsing room virtual number from #20385 format
        var roomData = "#20385\nPath through the hills~\nThe path leads...\n~\n112 8 0 1 99 1\nS";
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        Assert.AreEqual(20385, room.VirtualNumber);
    }
    
    [TestMethod]
    public void ParseRoom_BasicFormat_ReturnsCorrectName() 
    {
        // Test parsing room name (short description)
        var roomData = "#20385\nPath through the hills~\n...";
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        Assert.AreEqual("Path through the hills", room.Name);
    }
    
    [TestMethod]
    public void ParseRoom_BasicFormat_ReturnsCorrectDescription()
    {
        // Test parsing room description (long description)
        var roomData = "#20385\nPath through the hills~\nThe path leads north and south...\n~\n...";
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        Assert.IsTrue(room.Description.Contains("path leads north and south"));
    }
    
    [TestMethod]
    public void ParseRoom_InvalidFormat_ThrowsParseException()
    {
        // Test error handling for malformed room data
        var invalidData = "Not a room format";
        var parser = new WorldFileParser();
        Assert.ThrowsException<ParseException>(() => parser.ParseRoom(invalidData));
    }
}
```

**TDD Green Phase - Implementation**:
```csharp
// Create C3Mud.Core/World/Parsers/WorldFileParser.cs
public class WorldFileParser : IWorldFileParser
{
    public Room ParseRoom(string roomData)
    {
        var lines = roomData.Split('\n');
        var room = new Room();
        
        // Parse VNum from #12345 format
        room.VirtualNumber = int.Parse(lines[0].Substring(1));
        
        // Parse name (ends with ~)
        room.Name = lines[1].TrimEnd('~');
        
        // Parse description (multi-line until ~)
        // ... minimal implementation to pass tests
        
        return room;
    }
}

// Create C3Mud.Core/World/Models/Room.cs
public class Room
{
    public int VirtualNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
```

**Success Criteria**: Can parse 15Rooms.wld (3 rooms) with 100% basic field accuracy
**Dependencies**: None
**Risk Level**: High - Foundation for all world data

---

#### Iteration 1.2: Room Exit Parsing (Day 2 - 4 hours)
**Goal**: Parse room exits and connections correctly

**TDD Red Phase - Tests First**:
```csharp
[TestClass]
public class RoomExitParsingTests
{
    [TestMethod]
    public void ParseRoom_SingleExit_CreatesCorrectExit()
    {
        var roomData = @"#20385
Path through the hills~
Description here~
112 8 0 1 99 1
D0
~
~
0 -1 4938
S";
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        
        Assert.IsTrue(room.Exits.ContainsKey(Direction.North));
        Assert.AreEqual(4938, room.Exits[Direction.North].ToRoomVnum);
    }
    
    [TestMethod]
    public void ParseRoom_MultipleExits_CreatesAllExits()
    {
        // Test room with north and south exits
        var roomData = @"#20386
Path through the hills~
Description here~
112 8 0 1 99 1
D0
~
~
0 -1 20385
D2
~
~
0 -1 20387
S";
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        
        Assert.AreEqual(2, room.Exits.Count);
        Assert.IsTrue(room.Exits.ContainsKey(Direction.North));
        Assert.IsTrue(room.Exits.ContainsKey(Direction.South));
    }
    
    [TestMethod]
    public void ParseRoom_ExitWithDescription_ParsesDescription()
    {
        // Test exit with custom description
        var roomData = @"#20386
Path through the hills~
Description here~
112 8 0 1 99 1
D1
A dark winding road
~
~
0 -1 9201
S";
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        
        Assert.AreEqual("A dark winding road", room.Exits[Direction.East].Name);
    }
}
```

**TDD Green Phase - Implementation**:
```csharp
// Extend Room.cs
public class Room
{
    // ... existing properties
    public Dictionary<Direction, Exit> Exits { get; set; } = new();
}

// Create C3Mud.Core/World/Models/Exit.cs  
public class Exit
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ToRoomVnum { get; set; }
    public ExitFlags Flags { get; set; }
    public int KeyVnum { get; set; }
}

// Extend WorldFileParser.cs with exit parsing logic
```

**Success Criteria**: All room connections in 15Rooms.wld parsed correctly
**Dependencies**: Iteration 1.1
**Risk Level**: Medium - Complex syntax but well-defined

---

#### Iteration 1.3: Room Data Model Complete (Day 3 - 4 hours)  
**Goal**: Complete Room model with all legacy fields

**TDD Red Phase - Tests First**:
```csharp
[TestClass]
public class CompleteRoomModelTests
{
    [TestMethod]
    public void Room_AllFields_PreserveOriginalData()
    {
        var roomData = LoadTestRoomData("ComplexRoom.wld");
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        
        // Validate all CircleMUD room fields are preserved
        Assert.IsNotNull(room.Flags);
        Assert.IsNotNull(room.Sector);
        Assert.IsNotNull(room.LightLevel);
        // ... test all fields
    }
    
    [TestMethod]
    public void Room_GetExit_ReturnsCorrectExit()
    {
        var room = CreateTestRoomWithExits();
        var northExit = room.GetExit(Direction.North);
        Assert.IsNotNull(northExit);
        Assert.AreEqual(expectedVnum, northExit.ToRoomVnum);
    }
    
    [TestMethod]
    public void Room_GetAvailableExits_ReturnsAllDirections()
    {
        var room = CreateTestRoomWithMultipleExits();
        var availableExits = room.GetAvailableExits();
        Assert.AreEqual(3, availableExits.Count);
        Assert.Contains(Direction.North, availableExits);
        Assert.Contains(Direction.South, availableExits);
        Assert.Contains(Direction.East, availableExits);
    }
}
```

**TDD Green Phase - Implementation**:
```csharp
// Complete Room.cs with all CircleMUD fields
public class Room
{
    public int VirtualNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Room Properties (from original CircleMUD)
    public RoomFlags Flags { get; set; }
    public SectorType Sector { get; set; }
    public int LightLevel { get; set; }
    
    // Navigation
    public Dictionary<Direction, Exit> Exits { get; set; } = new();
    
    // Contents (will be populated later)
    public List<IPlayer> Players { get; set; } = new();
    public List<Mobile> Mobiles { get; set; } = new();
    public List<WorldObject> Objects { get; set; } = new();
    
    // Zone Information
    public int ZoneNumber { get; set; }
    
    // Helper Methods
    public Exit? GetExit(Direction direction) 
    {
        return Exits.TryGetValue(direction, out var exit) ? exit : null;
    }
    
    public List<Direction> GetAvailableExits() 
    {
        return Exits.Keys.ToList();
    }
    
    public bool CanEnter(IPlayer player) 
    {
        // Implement room entry restrictions
        return true; // Placeholder
    }
}
```

**Success Criteria**: Room model contains 100% of original .wld data fields
**Dependencies**: Iteration 1.1, 1.2
**Risk Level**: Low - Data modeling

---

### Phase 2: World Database Foundation (Days 4-6)

#### Iteration 2.1: Basic World Database (Day 4 - 6 hours)
**Goal**: In-memory storage and lookup for rooms

**TDD Red Phase - Tests First**:
```csharp
[TestClass]
public class WorldDatabaseTests
{
    [TestMethod]
    public void WorldDatabase_LoadRooms_AllRoomsAccessible()
    {
        var worldDb = new WorldDatabase();
        var rooms = LoadTestRooms(); // Load from 15Rooms.wld
        
        foreach (var room in rooms)
        {
            worldDb.AddRoom(room);
        }
        
        Assert.AreEqual(3, worldDb.GetRoomCount());
        foreach (var room in rooms)
        {
            var retrieved = worldDb.GetRoom(room.VirtualNumber);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(room.Name, retrieved.Name);
        }
    }
    
    [TestMethod]
    public void WorldDatabase_GetRoom_ReturnsCorrectRoom()
    {
        var worldDb = new WorldDatabase();
        var testRoom = CreateTestRoom(12345, "Test Room");
        worldDb.AddRoom(testRoom);
        
        var retrieved = worldDb.GetRoom(12345);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("Test Room", retrieved.Name);
        Assert.AreEqual(12345, retrieved.VirtualNumber);
    }
    
    [TestMethod]
    public void WorldDatabase_GetRoom_PerformanceUnder1ms()
    {
        var worldDb = new WorldDatabase();
        // Add 1000 test rooms
        for (int i = 1; i <= 1000; i++)
        {
            worldDb.AddRoom(CreateTestRoom(i, $"Room {i}"));
        }
        
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            var room = worldDb.GetRoom(Random.Next(1, 1001));
            Assert.IsNotNull(room);
        }
        stopwatch.Stop();
        
        var avgTime = stopwatch.ElapsedMilliseconds / 100.0;
        Assert.IsTrue(avgTime < 1.0, $"Average lookup time {avgTime}ms exceeds 1ms target");
    }
}
```

**TDD Green Phase - Implementation**:
```csharp
// Create C3Mud.Core/World/Services/WorldDatabase.cs
public class WorldDatabase : IWorldDatabase
{
    private readonly ConcurrentDictionary<int, Room> _rooms = new();
    private readonly ConcurrentDictionary<int, Mobile> _mobilePrototypes = new();
    private readonly ConcurrentDictionary<int, WorldObject> _objectPrototypes = new();
    private readonly ConcurrentDictionary<int, Zone> _zones = new();
    
    public void AddRoom(Room room)
    {
        _rooms.TryAdd(room.VirtualNumber, room);
    }
    
    public Room? GetRoom(int vnum)
    {
        return _rooms.TryGetValue(vnum, out var room) ? room : null;
    }
    
    public int GetRoomCount()
    {
        return _rooms.Count;
    }
    
    // Additional methods as needed...
}
```

**Success Criteria**: Load and lookup rooms from 15Rooms.wld under 1ms avg
**Dependencies**: Phase 1 complete
**Risk Level**: Medium - Performance critical

---

#### Iteration 2.2: World Loader Service (Day 5 - 4 hours)
**Goal**: Service to load all world files from directory

**TDD Red Phase - Tests First**:
```csharp
[TestClass] 
public class WorldLoaderTests
{
    [TestMethod]
    public async Task WorldLoader_LoadAllAreas_NoErrors()
    {
        var loader = new WorldLoader();
        var worldDb = await loader.LoadAllAreasAsync("Original-Code/dev/lib/areas/");
        
        Assert.IsNotNull(worldDb);
        Assert.IsTrue(worldDb.GetRoomCount() > 0, "Should load at least some rooms");
    }
    
    [TestMethod]
    public async Task WorldLoader_LoadAllAreas_CorrectRoomCount()
    {
        var loader = new WorldLoader();
        var worldDb = await loader.LoadAllAreasAsync("Original-Code/dev/lib/areas/");
        
        // Should load 2000+ rooms from all area files
        Assert.IsTrue(worldDb.GetRoomCount() > 2000, $"Expected >2000 rooms, got {worldDb.GetRoomCount()}");
    }
    
    [TestMethod]
    public async Task WorldLoader_LoadTime_Under30Seconds()
    {
        var loader = new WorldLoader();
        var stopwatch = Stopwatch.StartNew();
        
        var worldDb = await loader.LoadAllAreasAsync("Original-Code/dev/lib/areas/");
        
        stopwatch.Stop();
        Assert.IsTrue(stopwatch.ElapsedSeconds < 30, $"Load time {stopwatch.ElapsedSeconds}s exceeds 30s target");
        Assert.IsTrue(worldDb.GetRoomCount() > 1000, "Should have loaded substantial number of rooms");
    }
}
```

**TDD Green Phase - Implementation**:
```csharp
// Create C3Mud.Core/World/Services/WorldLoader.cs
public class WorldLoader : IWorldLoader
{
    private readonly WorldFileParser _worldParser = new();
    
    public async Task<WorldDatabase> LoadAllAreasAsync(string areaPath)
    {
        var worldDb = new WorldDatabase();
        var wldFiles = Directory.GetFiles(areaPath, "*.wld");
        
        var loadTasks = wldFiles.Select(async file =>
        {
            var rooms = await _worldParser.ParseFileAsync(file);
            return rooms;
        });
        
        var allRoomSets = await Task.WhenAll(loadTasks);
        
        foreach (var roomSet in allRoomSets)
        {
            foreach (var room in roomSet)
            {
                worldDb.AddRoom(room);
            }
        }
        
        return worldDb;
    }
}
```

**Success Criteria**: Load all 215 .wld files under 30 seconds
**Dependencies**: Iteration 2.1
**Risk Level**: High - Performance at scale

---

#### Iteration 2.3: Room Manager Service (Day 6 - 4 hours)
**Goal**: Room operations and player tracking

**TDD Red Phase - Tests First**:
```csharp
[TestClass]
public class RoomManagerTests
{
    [TestMethod]
    public void RoomManager_AddPlayer_PlayerInRoom()
    {
        var roomManager = new RoomManager();
        var room = CreateTestRoom(12345);
        var player = CreateTestPlayer("TestPlayer");
        
        roomManager.AddPlayerToRoom(player, room);
        
        Assert.IsTrue(room.Players.Contains(player));
        Assert.AreEqual(room.VirtualNumber, player.CurrentRoomVnum);
    }
    
    [TestMethod]
    public void RoomManager_RemovePlayer_PlayerNotInRoom() 
    {
        var roomManager = new RoomManager();
        var room = CreateTestRoom(12345);
        var player = CreateTestPlayer("TestPlayer");
        
        roomManager.AddPlayerToRoom(player, room);
        roomManager.RemovePlayerFromRoom(player, room);
        
        Assert.IsFalse(room.Players.Contains(player));
    }
    
    [TestMethod]
    public void RoomManager_ThreadSafety_ConcurrentOperations()
    {
        var roomManager = new RoomManager();
        var room = CreateTestRoom(12345);
        var players = CreateTestPlayers(100);
        
        // Test concurrent add/remove operations
        var tasks = players.Select(async player =>
        {
            await Task.Run(() =>
            {
                roomManager.AddPlayerToRoom(player, room);
                Thread.Sleep(1); // Simulate work
                roomManager.RemovePlayerFromRoom(player, room);
            });
        });
        
        Task.WaitAll(tasks.ToArray());
        Assert.AreEqual(0, room.Players.Count);
    }
}
```

**Success Criteria**: Handle 100+ concurrent player operations
**Dependencies**: Iteration 2.1
**Risk Level**: Medium - Concurrency complexity

---

### Phase 3: Command Integration (Days 7-9)

#### Iteration 3.1: Look Command Integration (Day 7 - 4 hours)
**Goal**: Replace placeholder room data with real world data

**TDD Red Phase - Tests First**:
```csharp
[TestClass]
public class LookCommandIntegrationTests
{
    [TestMethod]
    public async Task LookCommand_RealRoom_ShowsCorrectName()
    {
        // Setup real room data
        var worldDb = await LoadTestWorldDatabase();
        var room = worldDb.GetRoom(20385); // From 15Rooms.wld
        var player = CreateTestPlayer();
        player.CurrentRoomVnum = 20385;
        
        var lookCommand = new LookCommand(worldDb);
        await lookCommand.ExecuteAsync(player, "", 15);
        
        var output = GetPlayerOutput(player);
        Assert.IsTrue(output.Contains("Path through the hills"), "Should show real room name");
        Assert.IsFalse(output.Contains("placeholder"), "Should not show placeholder content");
    }
    
    [TestMethod]
    public async Task LookCommand_RealRoom_ShowsCorrectExits()
    {
        var worldDb = await LoadTestWorldDatabase();
        var player = CreateTestPlayerInRoom(20386); // Room with north/south/east exits
        
        var lookCommand = new LookCommand(worldDb);
        await lookCommand.ExecuteAsync(player, "", 15);
        
        var output = GetPlayerOutput(player);
        Assert.IsTrue(output.Contains("north"), "Should show north exit");
        Assert.IsTrue(output.Contains("south"), "Should show south exit");
        Assert.IsTrue(output.Contains("east"), "Should show east exit");
    }
}
```

**TDD Green Phase - Implementation**:
```csharp
// Update C3Mud.Core/Commands/Basic/LookCommand.cs
public class LookCommand : BaseCommand
{
    private readonly IWorldDatabase _worldDatabase;
    
    public LookCommand(IWorldDatabase worldDatabase)
    {
        _worldDatabase = worldDatabase;
    }
    
    private async Task LookAtRoom(IPlayer player)
    {
        var room = _worldDatabase.GetRoom(player.CurrentRoomVnum);
        if (room == null)
        {
            await SendToPlayerAsync(player, "You are floating in the void!");
            return;
        }
        
        // Show real room name
        await SendToPlayerAsync(player, $"&W{room.Name}&N", formatted: true);
        
        // Show real room description  
        await SendToPlayerAsync(player, room.Description, formatted: true);
        
        // Show real exits
        var exits = room.GetAvailableExits();
        if (exits.Any())
        {
            var exitText = "&YObvious exits:&N " + string.Join(", ", exits.Select(FormatDirection));
            await SendToPlayerAsync(player, exitText, formatted: true);
        }
        else
        {
            await SendToPlayerAsync(player, "&YObvious exits:&N None.", formatted: true);
        }
        
        // Show other players in room
        var otherPlayers = room.Players.Where(p => p != player);
        foreach (var otherPlayer in otherPlayers)
        {
            await SendToPlayerAsync(player, $"{otherPlayer.Name} is standing here.", formatted: true);
        }
    }
}
```

**Success Criteria**: Look command shows real room data instead of placeholder
**Dependencies**: Phase 2 complete  
**Risk Level**: Low - Direct integration

---

#### Iteration 3.2: Movement Commands (Day 8 - 6 hours)
**Goal**: Implement directional movement between real rooms

**TDD Red Phase - Tests First**:
```csharp
[TestClass]
public class MovementCommandTests
{
    [TestMethod]
    public async Task MovementCommand_ValidExit_MovesPlayer()
    {
        var worldDb = await LoadTestWorldDatabase();
        var player = CreateTestPlayerInRoom(20385); // Has north exit to 4938
        var northCommand = new NorthCommand(worldDb);
        
        var success = await northCommand.ExecuteAsync(player, "", 0);
        
        Assert.IsTrue(success);
        Assert.AreEqual(4938, player.CurrentRoomVnum);
    }
    
    [TestMethod]
    public async Task MovementCommand_InvalidExit_ShowsError()
    {
        var worldDb = await LoadTestWorldDatabase();
        var player = CreateTestPlayerInRoom(20385); // No west exit
        var westCommand = new WestCommand(worldDb);
        
        var success = await westCommand.ExecuteAsync(player, "", 0);
        
        Assert.IsFalse(success);
        Assert.AreEqual(20385, player.CurrentRoomVnum); // Should not move
        var output = GetPlayerOutput(player);
        Assert.IsTrue(output.Contains("cannot go that way"));
    }
    
    [TestMethod]
    public async Task MovementCommand_PlayerTracking_UpdatesRooms()
    {
        var worldDb = await LoadTestWorldDatabase();
        var startRoom = worldDb.GetRoom(20385);
        var targetRoom = worldDb.GetRoom(4938);
        var player = CreateTestPlayerInRoom(20385);
        
        // Verify player starts in correct room
        Assert.IsTrue(startRoom.Players.Contains(player));
        Assert.IsFalse(targetRoom.Players.Contains(player));
        
        var northCommand = new NorthCommand(worldDb);
        await northCommand.ExecuteAsync(player, "", 0);
        
        // Verify player moved to target room
        Assert.IsFalse(startRoom.Players.Contains(player));
        Assert.IsTrue(targetRoom.Players.Contains(player));
    }
}
```

**TDD Green Phase - Implementation**:
```csharp
// Create movement command base class
public abstract class MovementCommand : BaseCommand
{
    protected readonly IWorldDatabase _worldDatabase;
    protected abstract Direction Direction { get; }
    
    public MovementCommand(IWorldDatabase worldDatabase)
    {
        _worldDatabase = worldDatabase;
    }
    
    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        var moved = await MovePlayer(player, Direction);
        if (moved)
        {
            // Show new room
            var lookCommand = new LookCommand(_worldDatabase);
            await lookCommand.ExecuteAsync(player, "", 15);
        }
    }
    
    protected async Task<bool> MovePlayer(IPlayer player, Direction direction)
    {
        var currentRoom = _worldDatabase.GetRoom(player.CurrentRoomVnum);
        var exit = currentRoom?.GetExit(direction);
        
        if (exit == null)
        {
            await SendToPlayerAsync(player, "You cannot go that way.");
            return false;
        }
        
        var targetRoom = _worldDatabase.GetRoom(exit.ToRoomVnum);
        if (targetRoom == null)
        {
            await SendToPlayerAsync(player, "That way leads nowhere.");
            return false;
        }
        
        // Remove from current room
        currentRoom.Players.Remove(player);
        
        // Add to target room
        targetRoom.Players.Add(player);
        player.CurrentRoomVnum = targetRoom.VirtualNumber;
        
        await SendToPlayerAsync(player, $"You go {direction.ToString().ToLower()}.");
        
        return true;
    }
}

// Create specific direction commands
public class NorthCommand : MovementCommand
{
    protected override Direction Direction => Direction.North;
    public override string Name => "north";
    public override string[] Aliases => new[] { "n" };
    
    public NorthCommand(IWorldDatabase worldDatabase) : base(worldDatabase) { }
}

// ... Similar for South, East, West, Up, Down
```

**Success Criteria**: Players can navigate between real rooms
**Dependencies**: Iteration 3.1
**Risk Level**: Medium - Player state management

---

#### Iteration 3.3: Player Location Integration (Day 9 - 4 hours)
**Goal**: Integrate player location tracking with world system

**TDD Red Phase - Tests First**:
```csharp
[TestClass]
public class PlayerLocationIntegrationTests
{
    [TestMethod]
    public void Player_SetCurrentRoom_UpdatesCorrectly()
    {
        var worldDb = CreateTestWorldDatabase();
        var room = worldDb.GetRoom(12345);
        var player = CreateTestPlayer();
        
        player.SetCurrentRoom(room);
        
        Assert.AreEqual(12345, player.CurrentRoomVnum);
        Assert.AreEqual(room, player.CurrentRoom);
        Assert.IsTrue(room.Players.Contains(player));
    }
    
    [TestMethod]
    public async Task Player_MoveToRoom_UpdatesAllTracking()
    {
        var worldDb = CreateTestWorldDatabase();
        var startRoom = worldDb.GetRoom(12345);
        var targetRoom = worldDb.GetRoom(12346);
        var player = CreateTestPlayer();
        player.SetCurrentRoom(startRoom);
        
        await player.MoveToRoom(12346);
        
        Assert.AreEqual(12346, player.CurrentRoomVnum);
        Assert.AreEqual(targetRoom, player.CurrentRoom);
        Assert.IsFalse(startRoom.Players.Contains(player));
        Assert.IsTrue(targetRoom.Players.Contains(player));
    }
}
```

**TDD Green Phase - Implementation**:
```csharp
// Extend IPlayer interface
public interface IPlayer
{
    // Existing properties...
    int CurrentRoomVnum { get; set; }
    Room? CurrentRoom { get; set; }
    
    // New methods
    Task<bool> MoveToRoom(int targetRoomVnum);
    Task<bool> MoveDirection(Direction direction);
    void SetCurrentRoom(Room room);
}

// Update Player implementation
public class Player : IPlayer
{
    // ... existing properties
    public int CurrentRoomVnum { get; set; }
    public Room? CurrentRoom { get; set; }
    
    public void SetCurrentRoom(Room room)
    {
        // Remove from previous room
        CurrentRoom?.Players.Remove(this);
        
        // Set new room
        CurrentRoom = room;
        CurrentRoomVnum = room.VirtualNumber;
        
        // Add to new room
        room.Players.Add(this);
    }
    
    public async Task<bool> MoveToRoom(int targetRoomVnum)
    {
        var worldDb = ServiceLocator.GetService<IWorldDatabase>();
        var targetRoom = worldDb.GetRoom(targetRoomVnum);
        
        if (targetRoom == null)
            return false;
            
        SetCurrentRoom(targetRoom);
        return true;
    }
}
```

**Success Criteria**: All players tracked in correct rooms consistently
**Dependencies**: Iteration 3.2
**Risk Level**: Medium - State consistency

---

### Phase 4: Extended World Data (Days 10-12)

#### Iteration 4.1: Mobile File Parser (Day 10 - 6 hours)
**Goal**: Parse .mob files into Mobile data structures

**TDD Red Phase - Tests First**:
```csharp
[TestClass]
public class MobileParserTests
{
    [TestMethod]
    public void MobileParser_BasicMobile_AllFieldsCorrect()
    {
        var mobData = @"#7750
mob jotun storm giant~
&BJotun&w, ruler of the &cStorm Giants&n~
&wA large and very powerful looking &cstorm giant&w stands here.&n
~
Standing a massive twenty six feet tall...
~
1082400782 1048577 136249393 536871080 950 S
42 2 2 20d30+8000 20d3+15
1000000 400000
8 8 1
24 4 26 17 14 15 25 15";
        
        var parser = new MobileFileParser();
        var mobile = parser.ParseMobile(mobData);
        
        Assert.AreEqual(7750, mobile.VirtualNumber);
        Assert.AreEqual("mob jotun storm giant", mobile.Keywords);
        Assert.AreEqual(42, mobile.Level);
        Assert.AreEqual(1000000, mobile.Gold);
    }
    
    [TestMethod]
    public void MobileParser_CombatStats_ParsedCorrectly()
    {
        var mobData = LoadTestMobileData();
        var parser = new MobileFileParser();
        var mobile = parser.ParseMobile(mobData);
        
        Assert.IsTrue(mobile.Level > 0);
        Assert.IsTrue(mobile.MaxHitPoints > 0);
        Assert.IsTrue(mobile.ArmorClass != 0);
        Assert.IsTrue(mobile.Experience >= 0);
    }
}
```

**Success Criteria**: Parse Aerie.mob (18KB, complex mobiles) correctly
**Dependencies**: Phase 3 complete (allows parallel work)
**Risk Level**: Medium - Complex format with variable fields

---

#### Iteration 4.2: Object File Parser (Day 11 - 6 hours)
**Goal**: Parse .obj files into WorldObject data structures  

**Similar TDD pattern as 4.1 for object parsing**

**Success Criteria**: Parse complex object files with all value types
**Dependencies**: None (parallel with 4.1)
**Risk Level**: Medium - Multiple object type variations

---

#### Iteration 4.3: Zone File Parser (Day 12 - 4 hours)
**Goal**: Parse .zon files for area metadata and resets

**Similar TDD pattern for zone parsing**

**Success Criteria**: Parse zone metadata and reset commands
**Dependencies**: None (parallel with 4.1, 4.2)
**Risk Level**: Low - Simpler format

---

### Phase 5: Performance & Validation (Days 13-14)

#### Iteration 5.1: Memory Optimization (Day 13 - 4 hours)
**Goal**: Optimize memory usage for full world loading

**TDD Red Phase - Tests First**:
```csharp
[TestClass]
public class MemoryOptimizationTests
{
    [TestMethod]
    public async Task WorldDatabase_FullLoad_MemoryUnder1GB()
    {
        var initialMemory = GC.GetTotalMemory(true);
        var worldLoader = new WorldLoader();
        var worldDb = await worldLoader.LoadAllAreasAsync("Original-Code/dev/lib/areas/");
        
        // Add 100 test players
        for (int i = 0; i < 100; i++)
        {
            var player = CreateTestPlayer($"Player{i}");
            var room = worldDb.GetRandomRoom();
            room?.Players.Add(player);
        }
        
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = (finalMemory - initialMemory) / 1024 / 1024; // MB
        
        Assert.IsTrue(memoryUsed < 1024, $"Memory usage {memoryUsed}MB exceeds 1GB limit");
    }
}
```

**Success Criteria**: Full world + 100 players under 1GB memory
**Dependencies**: Phase 4 complete
**Risk Level**: Medium - Performance optimization

---

#### Iteration 5.2: Legacy Compatibility Validation (Day 14 - 6 hours)
**Goal**: Validate 100% compatibility with original world data

**TDD Red Phase - Tests First**:
```csharp
[TestClass]
public class LegacyCompatibilityTests
{
    [TestMethod]
    public async Task LegacyValidator_AllRooms_ExactMatch()
    {
        var validator = new LegacyCompatibilityValidator();
        var worldDb = await LoadCompleteWorld();
        var report = await validator.ValidateWorldData(worldDb);
        
        Assert.AreEqual(0, report.DataLossErrors.Count, 
            $"Data loss detected: {string.Join(", ", report.DataLossErrors)}");
        Assert.AreEqual(0, report.FormatErrors.Count,
            $"Format errors: {string.Join(", ", report.FormatErrors)}");
        Assert.IsTrue(report.OverallCompatibility >= 100.0,
            $"Compatibility {report.OverallCompatibility}% below 100% requirement");
    }
}
```

**Success Criteria**: 100% field-by-field match with original files
**Dependencies**: All previous phases
**Risk Level**: Critical - Data integrity validation

---

## Success Criteria Summary

### Technical Milestones
- **Day 3**: Parse all 215 area files without errors
- **Day 6**: Load complete world under 30 seconds  
- **Day 9**: All commands use real data instead of placeholders
- **Day 12**: Extended world data (mobs/objects) fully parsed
- **Day 14**: 100% field preservation validation complete

### User-Visible Impact
- **Before**: Commands show "placeholder room description"
- **After**: Commands show real MUD world with 2000+ actual rooms
- **Before**: Movement commands don't work
- **After**: Players can navigate the complete legacy world

### Integration Validation
- LookCommand shows real room names, descriptions, exits
- Movement commands (north, south, etc.) work between actual rooms
- WhoCommand shows real player locations by room name
- Player location tracking accurate across all systems
- No regression in existing functionality

## Risk Mitigation Strategy

### Data Preservation (Critical)
- Parse ALL area files during each iteration
- Automated comparison with original file data
- Backup/rollback capability at each iteration
- Manual spot-checking of complex areas

### Performance Management (High)
- Memory profiling at each iteration
- Performance regression testing
- Load testing with target player counts
- Fallback to smaller test worlds if needed

### Integration Safety (Medium)
- Feature flags for gradual rollout
- Comprehensive regression testing
- Parallel development where possible
- Incremental command system integration

---

**This plan provides a methodical, TDD-driven approach to integrate the substantial legacy world data while maintaining the high code quality standards established in the existing codebase. Each iteration is independently deliverable and testable, with clear success criteria and risk mitigation strategies.**

**Total Timeline: 14 days maximum, with each iteration being 6 hours or less of focused development work.**