using Xunit;
using C3Mud.Core.World.Parsers;
using System.IO;

namespace C3Mud.Tests.World;

public class WorldFileIntegrationTests
{
    [Fact]
    public void ParseCompleteFile_15RoomsWld_ParsesAllRoomsCorrectly()
    {
        // Load the actual 15Rooms.wld file
        var filePath = @"C:\Projects\C3Mud\Original-Code\test\lib\areas\15Rooms.wld";
        
        if (!File.Exists(filePath))
        {
            // Skip test if file doesn't exist
            return;
        }

        var fileContent = File.ReadAllText(filePath);
        var parser = new WorldFileParser();
        
        // The file contains 3 rooms: 20385, 20386, 20387
        // Each room starts with # and ends before the next # or $~
        var roomSections = ExtractRoomSections(fileContent);
        
        Assert.Equal(3, roomSections.Count);
        
        // Test parsing of each room
        var room1 = parser.ParseRoom(roomSections[0]);
        Assert.Equal(20385, room1.VirtualNumber);
        Assert.Equal("Path through the hills", room1.Name);
        Assert.Contains("path leads north and south", room1.Description);
        
        var room2 = parser.ParseRoom(roomSections[1]);
        Assert.Equal(20386, room2.VirtualNumber);
        Assert.Equal("Path through the hills", room2.Name);
        Assert.Contains("small trail that leads to the east", room2.Description);
        
        var room3 = parser.ParseRoom(roomSections[2]);
        Assert.Equal(20387, room3.VirtualNumber);
        Assert.Equal("Path through the hills", room3.Name);
        Assert.Contains("path leads north and south", room3.Description);
    }

    private List<string> ExtractRoomSections(string fileContent)
    {
        var sections = new List<string>();
        var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        var currentSection = new List<string>();
        foreach (var line in lines)
        {
            if (line.StartsWith("#") && currentSection.Any())
            {
                // Start of new room, finish current section
                sections.Add(string.Join("\n", currentSection));
                currentSection.Clear();
            }
            
            if (line.StartsWith("$~"))
            {
                // End of file marker
                break;
            }
            
            currentSection.Add(line);
        }
        
        // Add the last section if it exists
        if (currentSection.Any())
        {
            sections.Add(string.Join("\n", currentSection));
        }
        
        return sections;
    }
}