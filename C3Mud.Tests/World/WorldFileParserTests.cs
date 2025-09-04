using Xunit;
using C3Mud.Core.World.Parsers;
using C3Mud.Core.World.Models;

namespace C3Mud.Tests.World;

public class WorldFileParserTests
{
    [Fact]
    public void ParseRoom_BasicFormat_ReturnsCorrectVNum()
    {
        // Test parsing room virtual number from #20385 format
        var roomData = "#20385\nPath through the hills~\nThe path leads...\n~\n112 8 0 1 99 1\nS";
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        Assert.Equal(20385, room.VirtualNumber);
    }
    
    [Fact]
    public void ParseRoom_BasicFormat_ReturnsCorrectName() 
    {
        // Test parsing room name (short description)
        var roomData = "#20385\nPath through the hills~\n...";
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        Assert.Equal("Path through the hills", room.Name);
    }
    
    [Fact]
    public void ParseRoom_BasicFormat_ReturnsCorrectDescription()
    {
        // Test parsing room description (long description)
        var roomData = "#20385\nPath through the hills~\nThe path leads north and south...\n~\n...";
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        Assert.Contains("path leads north and south", room.Description);
    }
    
    [Fact]
    public void ParseRoom_InvalidFormat_ThrowsParseException()
    {
        // Test error handling for malformed room data
        var invalidData = "Not a room format";
        var parser = new WorldFileParser();
        Assert.Throws<ParseException>(() => parser.ParseRoom(invalidData));
    }

    [Fact]
    public void ParseRoom_RealWorldData_ParsesRoom20385Correctly()
    {
        // Test with actual data from 15Rooms.wld - room 20385
        var roomData = @"#20385
Path through the hills~
The path leads north and south. South leads towards a temple while north leads to a larger road.
~
112 8 0 1 99 1
D0
~
~
0 -1 4938
D2
~
~
0 -1 20386
S";
        
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        
        Assert.Equal(20385, room.VirtualNumber);
        Assert.Equal("Path through the hills", room.Name);
        Assert.Equal("The path leads north and south. South leads towards a temple while north leads to a larger road.", room.Description.Trim());
    }

    [Fact]
    public void ParseRoom_WithExits_ReturnsCorrectExitCount()
    {
        // Test room 20385 which has 2 exits: north to 4938, south to 20386
        var roomData = @"#20385
Path through the hills~
The path leads north and south. South leads towards a temple while north leads to a larger road.
~
112 8 0 1 99 1
D0
~
~
0 -1 4938
D2
~
~
0 -1 20386
S";
        
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        
        Assert.Equal(2, room.Exits.Count);
    }

    [Fact]
    public void ParseRoom_WithExits_ReturnsCorrectExitDirections()
    {
        // Test room 20385 which has north and south exits
        var roomData = @"#20385
Path through the hills~
The path leads north and south. South leads towards a temple while north leads to a larger road.
~
112 8 0 1 99 1
D0
~
~
0 -1 4938
D2
~
~
0 -1 20386
S";
        
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        
        Assert.True(room.Exits.ContainsKey(Direction.North));
        Assert.True(room.Exits.ContainsKey(Direction.South));
        Assert.False(room.Exits.ContainsKey(Direction.East));
        Assert.False(room.Exits.ContainsKey(Direction.West));
    }

    [Fact]
    public void ParseRoom_WithExits_ReturnsCorrectTargetRooms()
    {
        // Test room 20385 exits lead to correct target rooms
        var roomData = @"#20385
Path through the hills~
The path leads north and south. South leads towards a temple while north leads to a larger road.
~
112 8 0 1 99 1
D0
~
~
0 -1 4938
D2
~
~
0 -1 20386
S";
        
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        
        Assert.Equal(4938, room.Exits[Direction.North].TargetRoomVnum);
        Assert.Equal(20386, room.Exits[Direction.South].TargetRoomVnum);
    }

    [Fact]
    public void ParseRoom_WithExits_HandlesExitDescriptions()
    {
        // Test room 20386 which has an exit with a name/description
        var roomData = @"#20386
Path through the hills~
The path leads north and south. South leads towards a temple while north leads to a larger road.
There is a small trail that leads to the east.
~
112 8 0 1 99 1
D0
~
~
0 -1 20385
D1
A dark winding road
~
~
0 -1 9201
D2
~
~
0 -1 20387
S";
        
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        
        Assert.Equal(3, room.Exits.Count);
        Assert.Equal("", room.Exits[Direction.North].Name); // No name specified
        Assert.Equal("A dark winding road", room.Exits[Direction.East].Name); // Named exit
        Assert.Equal("", room.Exits[Direction.South].Name); // No name specified
    }

    [Fact]
    public void ParseRoom_IntegrationTest_ParsesActual15RoomsData()
    {
        // Test parsing the entire 15Rooms.wld content to ensure compatibility
        var roomData = @"#20385
Path through the hills~
The path leads north and south. South leads towards a temple while north leads to a larger road.
~
112 8 0 1 99 1
D0
~
~
0 -1 4938
D2
~
~
0 -1 20386
S";
        
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        
        // Verify basic room data
        Assert.Equal(20385, room.VirtualNumber);
        Assert.Equal("Path through the hills", room.Name);
        Assert.Contains("path leads north and south", room.Description);
        
        // Verify exits
        Assert.Equal(2, room.Exits.Count);
        Assert.True(room.Exits.ContainsKey(Direction.North));
        Assert.True(room.Exits.ContainsKey(Direction.South));
        
        // Verify exit targets match the 15Rooms.wld file
        Assert.Equal(4938, room.Exits[Direction.North].TargetRoomVnum);
        Assert.Equal(20386, room.Exits[Direction.South].TargetRoomVnum);
        
        // Verify door flags and keys are parsed
        Assert.Equal(0, room.Exits[Direction.North].DoorFlags);
        Assert.Equal(-1, room.Exits[Direction.North].KeyVnum);
        Assert.Equal(0, room.Exits[Direction.South].DoorFlags);
        Assert.Equal(-1, room.Exits[Direction.South].KeyVnum);
    }

    [Fact]
    public void ParseRoom_CompleteData_ReturnsCorrectZoneNumber()
    {
        // Test parsing room zone number from "112 8 0 1 99 1" line
        var roomData = @"#20385
Path through the hills~
The path leads north and south. South leads towards a temple while north leads to a larger road.
~
112 8 0 1 99 1
D0
~
~
0 -1 4938
D2
~
~
0 -1 20386
S";
        
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        
        Assert.Equal(112, room.Zone);
    }

    [Fact]
    public void ParseRoom_CompleteData_ReturnsCorrectRoomFlags()
    {
        // Test parsing room flags from "112 8 0 1 99 1" line - flag value 8 = INDOORS
        var roomData = @"#20385
Path through the hills~
The path leads north and south. South leads towards a temple while north leads to a larger road.
~
112 8 0 1 99 1
D0
~
~
0 -1 4938
D2
~
~
0 -1 20386
S";
        
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        
        Assert.Equal(RoomFlags.Indoors, room.RoomFlags);
    }

    [Fact]
    public void ParseRoom_CompleteData_ReturnsCorrectSectorType()
    {
        // Test parsing sector type from "112 8 0 1 99 1" line - sector 0 = INSIDE
        var roomData = @"#20385
Path through the hills~
The path leads north and south. South leads towards a temple while north leads to a larger road.
~
112 8 0 1 99 1
D0
~
~
0 -1 4938
D2
~
~
0 -1 20386
S";
        
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        
        Assert.Equal(SectorType.Inside, room.SectorType);
    }

    [Fact]
    public void ParseRoom_CompleteData_ReturnsCorrectLightLevel()
    {
        // Test parsing light level from "112 8 0 1 99 1" line - light level 1
        var roomData = @"#20385
Path through the hills~
The path leads north and south. South leads towards a temple while north leads to a larger road.
~
112 8 0 1 99 1
D0
~
~
0 -1 4938
D2
~
~
0 -1 20386
S";
        
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        
        Assert.Equal(1, room.LightLevel);
    }

    [Fact]
    public void ParseRoom_CompleteData_ReturnsCorrectRegenRates()
    {
        // Test parsing regen rates from "112 8 0 1 99 1" line - mana regen 99, hp regen 1
        var roomData = @"#20385
Path through the hills~
The path leads north and south. South leads towards a temple while north leads to a larger road.
~
112 8 0 1 99 1
D0
~
~
0 -1 4938
D2
~
~
0 -1 20386
S";
        
        var parser = new WorldFileParser();
        var room = parser.ParseRoom(roomData);
        
        Assert.Equal(99, room.ManaRegen);
        Assert.Equal(1, room.HpRegen);
    }
}