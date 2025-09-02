using C3Mud.Core.World.Models;

namespace C3Mud.Core.World.Parsers;

/// &lt;summary&gt;
/// Parses CircleMUD/DikuMUD world files (.wld format)
/// &lt;/summary&gt;
public class WorldFileParser
{
    /// &lt;summary&gt;
    /// Parses a single room from raw room data text
    /// &lt;/summary&gt;
    /// &lt;param name="roomData"&gt;Raw room data from .wld file&lt;/param&gt;
    /// &lt;returns&gt;Parsed Room object&lt;/returns&gt;
    /// &lt;exception cref="ParseException"&gt;Thrown when room data format is invalid&lt;/exception&gt;
    public Room ParseRoom(string roomData)
    {
        if (string.IsNullOrWhiteSpace(roomData))
        {
            throw new ParseException("Room data cannot be null or empty");
        }

        var lines = roomData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 3)
        {
            throw new ParseException("Invalid room format - insufficient data");
        }

        var room = new Room();

        // Parse Virtual Number from #12345 format
        var vnumLine = lines[0].Trim();
        if (!vnumLine.StartsWith("#") || !int.TryParse(vnumLine.Substring(1), out var vnum))
        {
            throw new ParseException($"Invalid virtual number format: {vnumLine}");
        }
        room.VirtualNumber = vnum;

        // Parse room name (second line, ends with ~)
        var nameLine = lines[1].Trim();
        if (!nameLine.EndsWith("~"))
        {
            throw new ParseException($"Room name must end with ~: {nameLine}");
        }
        room.Name = nameLine.TrimEnd('~');

        // Parse room description (lines until we find one ending with ~)
        var descriptionLines = new List<string>();
        int currentLine = 2;
        
        while (currentLine < lines.Length)
        {
            var line = lines[currentLine];
            if (line.Trim().EndsWith("~"))
            {
                // This is the last line of the description
                if (line.Trim() != "~")
                {
                    // If the line has content before the ~, include it
                    descriptionLines.Add(line.TrimEnd('~'));
                }
                break;
            }
            descriptionLines.Add(line);
            currentLine++;
        }

        room.Description = string.Join("\n", descriptionLines);

        // Move to next line after description ends
        currentLine++;
        
        // Parse the room flags line (format: "zone roomFlags sectorType lightLevel manaRegen hpRegen")
        if (currentLine < lines.Length)
        {
            ParseRoomFlags(lines[currentLine], room);
            currentLine++; // Move past room flags line
        }
        
        // Parse exits (D0, D1, D2, etc.) until we hit 'S' or end of data
        while (currentLine < lines.Length)
        {
            var line = lines[currentLine].Trim();
            
            // Check if this marks the end of the room (S line)
            if (line == "S")
            {
                break;
            }
            
            // Check if this is a direction line (D0, D1, D2, etc.)
            if (line.StartsWith("D") && line.Length >= 2 && int.TryParse(line.Substring(1), out var directionNum))
            {
                var exit = ParseExit(lines, ref currentLine, directionNum);
                if (exit != null)
                {
                    room.Exits[exit.Direction] = exit;
                }
            }
            else
            {
                currentLine++;
            }
        }

        return room;
    }
    
    /// <summary>
    /// Parses a single exit from the room data
    /// </summary>
    /// <param name="lines">All lines of the room data</param>
    /// <param name="currentLine">Current line index (will be modified)</param>
    /// <param name="directionNum">Direction number (0-5)</param>
    /// <returns>Parsed Exit object or null if parsing fails</returns>
    private Exit? ParseExit(string[] lines, ref int currentLine, int directionNum)
    {
        if (directionNum < 0 || directionNum > 5)
        {
            return null; // Invalid direction
        }
        
        var exit = new Exit
        {
            Direction = (Direction)directionNum
        };
        
        currentLine++; // Move past the D# line
        
        // Parse exit name (can be multiple lines until ~)
        var nameLines = new List<string>();
        while (currentLine < lines.Length)
        {
            var line = lines[currentLine].Trim();
            if (line == "~")
            {
                // End of name section
                currentLine++;
                break;
            }
            nameLines.Add(line);
            currentLine++;
        }
        exit.Name = string.Join(" ", nameLines);
        
        // Parse exit description (can be multiple lines until ~)
        var descLines = new List<string>();
        while (currentLine < lines.Length)
        {
            var line = lines[currentLine].Trim();
            if (line == "~")
            {
                // End of description section
                currentLine++;
                break;
            }
            descLines.Add(line);
            currentLine++;
        }
        exit.Description = string.Join(" ", descLines);
        
        // Parse exit flags, key, and target room (format: "0 -1 4938")
        if (currentLine < lines.Length)
        {
            var flagsLine = lines[currentLine].Trim();
            var parts = flagsLine.Split(' ');
            if (parts.Length >= 3)
            {
                if (int.TryParse(parts[0], out var flags))
                {
                    exit.DoorFlags = flags;
                }
                
                if (int.TryParse(parts[1], out var key))
                {
                    exit.KeyVnum = key;
                }
                
                if (int.TryParse(parts[2], out var targetRoom))
                {
                    exit.TargetRoomVnum = targetRoom;
                }
            }
            currentLine++;
        }
        
        return exit;
    }
    
    /// <summary>
    /// Parses the room flags line containing zone, room flags, sector type, light level, and regen rates
    /// Format: "zone roomFlags sectorType lightLevel manaRegen hpRegen"
    /// Example: "112 8 0 1 99 1"
    /// </summary>
    /// <param name="flagsLine">The room flags line to parse</param>
    /// <param name="room">The room object to populate</param>
    private void ParseRoomFlags(string flagsLine, Room room)
    {
        var parts = flagsLine.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length >= 6)
        {
            // Parse zone number
            if (int.TryParse(parts[0], out var zone))
            {
                room.Zone = zone;
            }
            
            // Parse room flags (bitfield)
            if (int.TryParse(parts[1], out var roomFlags))
            {
                room.RoomFlags = (RoomFlags)roomFlags;
            }
            
            // Parse sector type
            if (int.TryParse(parts[2], out var sectorType))
            {
                room.SectorType = (SectorType)sectorType;
            }
            
            // Parse light level
            if (int.TryParse(parts[3], out var lightLevel))
            {
                room.LightLevel = lightLevel;
            }
            
            // Parse mana regeneration rate
            if (int.TryParse(parts[4], out var manaRegen))
            {
                room.ManaRegen = manaRegen;
            }
            
            // Parse hit point regeneration rate
            if (int.TryParse(parts[5], out var hpRegen))
            {
                room.HpRegen = hpRegen;
            }
        }
    }
}