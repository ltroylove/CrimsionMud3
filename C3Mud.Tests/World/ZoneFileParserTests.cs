using Xunit;
using C3Mud.Core.World.Parsers;
using C3Mud.Core.World.Models;
using System;
using System.Linq;

namespace C3Mud.Tests.World;

public class ZoneFileParserTests
{
    [Fact]
    public void ZoneFileParser_ParseZoneFile_ReturnsCorrectZoneData()
    {
        // Test basic zone file parsing - this will fail initially (Red Phase)
        var zoneData = @"#0
Aerie~
7849 45 2 128 128 128
*
M 0 7756 7 7759
D 0 7772 1 1
S
*
$~
#999999";

        var parser = new ZoneFileParser();
        var zone = parser.ParseZone(zoneData);
        
        Assert.Equal(0, zone.VirtualNumber);
        Assert.Equal("Aerie", zone.Name);
        Assert.Equal(7849, zone.TopRoom);
        Assert.Equal(45, zone.ResetTime);
        Assert.Equal(ResetMode.Always, zone.ResetMode);
        Assert.Equal(2, zone.ResetCommands.Count);
    }
    
    [Fact]
    public void ZoneFileParser_ParseResetCommand_ParsesMobileSpawn()
    {
        // Test mobile spawn command parsing: M 0 7756 7 7759
        var zoneData = @"#100
Test Zone~
1000 30 1 128 128 128
*
M 0 7756 7 7759
S
*
$~
#999999";

        var parser = new ZoneFileParser();
        var zone = parser.ParseZone(zoneData);
        
        Assert.Single(zone.ResetCommands);
        var command = zone.ResetCommands.First();
        Assert.Equal(ResetCommandType.Mobile, command.CommandType);
        Assert.Equal(0, command.Arg1);
        Assert.Equal(7756, command.Arg2); // mobile vnum
        Assert.Equal(7, command.Arg3);   // max existing
        Assert.Equal(7759, command.Arg4); // room vnum
    }
    
    [Fact]
    public void ZoneFileParser_ParseResetCommand_ParsesObjectLoad()
    {
        // Test object load command parsing: O 0 7801 1 7826
        var zoneData = @"#200
Test Zone~
2000 30 1 128 128 128
*
O 0 7801 1 7826
S
*
$~
#999999";

        var parser = new ZoneFileParser();
        var zone = parser.ParseZone(zoneData);
        
        Assert.Single(zone.ResetCommands);
        var command = zone.ResetCommands.First();
        Assert.Equal(ResetCommandType.Object, command.CommandType);
        Assert.Equal(0, command.Arg1);
        Assert.Equal(7801, command.Arg2); // object vnum
        Assert.Equal(1, command.Arg3);    // max existing
        Assert.Equal(7826, command.Arg4); // room vnum
    }
    
    [Fact]
    public void ZoneFileParser_ParseResetCommand_ParsesDoorState()
    {
        // Test door state command parsing: D 0 7772 1 1
        var zoneData = @"#300
Test Zone~
3000 30 1 128 128 128
*
D 0 7772 1 1
S
*
$~
#999999";

        var parser = new ZoneFileParser();
        var zone = parser.ParseZone(zoneData);
        
        Assert.Single(zone.ResetCommands);
        var command = zone.ResetCommands.First();
        Assert.Equal(ResetCommandType.Door, command.CommandType);
        Assert.Equal(0, command.Arg1);
        Assert.Equal(7772, command.Arg2); // room vnum
        Assert.Equal(1, command.Arg3);    // direction
        Assert.Equal(1, command.Arg4);    // door state (closed)
    }
    
    [Fact]
    public void ZoneFileParser_ParseResetCommand_ParsesEquipCommand()
    {
        // Test equipment command parsing: E 1 7760 0 16
        var zoneData = @"#400
Test Zone~
4000 30 1 128 128 128
*
M 0 7756 7 7759
E 1 7760 0 16
S
*
$~
#999999";

        var parser = new ZoneFileParser();
        var zone = parser.ParseZone(zoneData);
        
        Assert.Equal(2, zone.ResetCommands.Count);
        var equipCommand = zone.ResetCommands.Last();
        Assert.Equal(ResetCommandType.Equip, equipCommand.CommandType);
        Assert.Equal(1, equipCommand.Arg1);
        Assert.Equal(7760, equipCommand.Arg2); // object vnum
        Assert.Equal(0, equipCommand.Arg3);    // max existing
        Assert.Equal(16, equipCommand.Arg4);   // wear position
    }
    
    [Fact]
    public void ZoneFileParser_ParseResetCommand_ParsesGiveCommand()
    {
        // Test give object command parsing: G 1 7774 1
        var zoneData = @"#500
Test Zone~
5000 30 1 128 128 128
*
M 0 7766 1 7833
G 1 7774 1
S
*
$~
#999999";

        var parser = new ZoneFileParser();
        var zone = parser.ParseZone(zoneData);
        
        Assert.Equal(2, zone.ResetCommands.Count);
        var giveCommand = zone.ResetCommands.Last();
        Assert.Equal(ResetCommandType.Give, giveCommand.CommandType);
        Assert.Equal(1, giveCommand.Arg1);
        Assert.Equal(7774, giveCommand.Arg2); // object vnum
        Assert.Equal(1, giveCommand.Arg3);    // max existing
    }
    
    [Fact]
    public void ZoneFileParser_ParseResetCommand_ParsesPutCommand()
    {
        // Test put object in container command parsing: P 1 7803 2 7801
        var zoneData = @"#600
Test Zone~
6000 30 1 128 128 128
*
O 0 7801 1 7826
P 1 7803 2 7801
S
*
$~
#999999";

        var parser = new ZoneFileParser();
        var zone = parser.ParseZone(zoneData);
        
        Assert.Equal(2, zone.ResetCommands.Count);
        var putCommand = zone.ResetCommands.Last();
        Assert.Equal(ResetCommandType.Put, putCommand.CommandType);
        Assert.Equal(1, putCommand.Arg1);
        Assert.Equal(7803, putCommand.Arg2); // object vnum
        Assert.Equal(2, putCommand.Arg3);    // max existing
        Assert.Equal(7801, putCommand.Arg4); // container vnum
    }
    
    [Fact]
    public void ZoneFileParser_ParseZone_HandlesEmptyZone()
    {
        // Test parsing empty zone with no reset commands
        var zoneData = @"#0
Blank Zone~
3 1 0 128 128 128
*
*
S
*
$~
#999999";

        var parser = new ZoneFileParser();
        var zone = parser.ParseZone(zoneData);
        
        Assert.Equal(0, zone.VirtualNumber);
        Assert.Equal("Blank Zone", zone.Name);
        Assert.Equal(3, zone.TopRoom);
        Assert.Equal(1, zone.ResetTime);
        Assert.Equal(ResetMode.Never, zone.ResetMode);
        Assert.Empty(zone.ResetCommands);
    }
    
    [Fact]
    public void ZoneFileParser_ParseZone_ThrowsOnInvalidVNum()
    {
        // Test that invalid virtual numbers throw exceptions
        var zoneData = @"InvalidVNum
Blank Zone~
3 1 0 128 128 128
*
S
*
$~
#999999";

        var parser = new ZoneFileParser();
        
        Assert.Throws<ParseException>(() => parser.ParseZone(zoneData));
    }
    
    [Fact]
    public void ZoneFileParser_ParseZone_ThrowsOnNullOrEmptyData()
    {
        // Test that null or empty zone data throws exceptions
        var parser = new ZoneFileParser();
        
        Assert.Throws<ParseException>(() => parser.ParseZone(null));
        Assert.Throws<ParseException>(() => parser.ParseZone(""));
        Assert.Throws<ParseException>(() => parser.ParseZone("   "));
    }
    
    [Fact]
    public void ZoneFileParser_ParseComplexZone_HandlesNestedCommands()
    {
        // Test parsing complex zone with nested commands (mobile + equipment)
        var complexZoneData = @"#700
Complex Zone~
7000 45 2 128 128 128
*
M 0 7750 1 7826
E 1 7759 0 5
E 1 7750 0 16
G 1 7774 1
O 0 7801 1 7826
P 1 7803 2 7801
D 0 7832 1 1
S
*
$~
#999999";

        var parser = new ZoneFileParser();
        var zone = parser.ParseZone(complexZoneData);
        
        Assert.Equal(700, zone.VirtualNumber);
        Assert.Equal("Complex Zone", zone.Name);
        Assert.Equal(7, zone.ResetCommands.Count);
        
        // Verify command types
        Assert.Equal(ResetCommandType.Mobile, zone.ResetCommands[0].CommandType);
        Assert.Equal(ResetCommandType.Equip, zone.ResetCommands[1].CommandType);
        Assert.Equal(ResetCommandType.Equip, zone.ResetCommands[2].CommandType);
        Assert.Equal(ResetCommandType.Give, zone.ResetCommands[3].CommandType);
        Assert.Equal(ResetCommandType.Object, zone.ResetCommands[4].CommandType);
        Assert.Equal(ResetCommandType.Put, zone.ResetCommands[5].CommandType);
        Assert.Equal(ResetCommandType.Door, zone.ResetCommands[6].CommandType);
    }
}