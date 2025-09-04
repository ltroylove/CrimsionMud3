using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using C3Mud.Core.World.Models;

namespace C3Mud.Core.World.Parsers;

/// <summary>
/// Parser for CircleMUD/DikuMUD .zon files
/// Handles parsing zone definitions and reset commands from legacy text format
/// </summary>
public class ZoneFileParser
{
    private static readonly Regex VNumRegex = new Regex(@"^#(\d+)$", RegexOptions.Compiled);
    private static readonly Regex ResetCommandRegex = new Regex(@"^([MOEGDRP])\s+(\d+)\s+(\d+)(?:\s+(\d+))?(?:\s+(\d+))?(?:\s+(\d+))?", RegexOptions.Compiled);
    
    /// <summary>
    /// Parses a single zone from raw text data
    /// </summary>
    /// <param name="zoneData">Raw zone data from .zon file</param>
    /// <returns>Parsed Zone object</returns>
    /// <exception cref="ParseException">Thrown when zone data is malformed</exception>
    public Zone ParseZone(string zoneData)
    {
        if (string.IsNullOrWhiteSpace(zoneData))
            throw new ParseException("Zone data cannot be null or empty");
            
        var lines = zoneData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd())
            .ToArray();
            
        if (lines.Length < 4)
            throw new ParseException("Zone data has insufficient lines");
            
        var zone = new Zone();
        var lineIndex = 0;
        
        try
        {
            // Parse virtual number (#0, #100, etc.)
            zone.VirtualNumber = ParseVirtualNumber(lines[lineIndex++]);
            
            // Parse zone name (ends with ~)
            zone.Name = ParseTildeTerminatedString(lines[lineIndex++]);
            
            // Parse zone parameters line (top_room reset_time reset_mode minlevel maxlevel maxplayers)
            ParseZoneParameters(lines[lineIndex++], zone);
            
            // Skip comment line (*)
            if (lineIndex < lines.Length && lines[lineIndex].Trim() == "*")
                lineIndex++;
                
            // Parse reset commands until we hit 'S' or end of data
            ParseResetCommands(lines, ref lineIndex, zone);
            
            return zone;
        }
        catch (Exception ex) when (!(ex is ParseException))
        {
            throw new ParseException($"Error parsing zone at line {lineIndex + 1}: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Parses virtual number from format #12345
    /// </summary>
    private int ParseVirtualNumber(string line)
    {
        var match = VNumRegex.Match(line.Trim());
        if (!match.Success)
            throw new ParseException($"Invalid virtual number format: {line}");
            
        return int.Parse(match.Groups[1].Value);
    }
    
    /// <summary>
    /// Parses tilde-terminated strings (removes trailing ~)
    /// </summary>
    private string ParseTildeTerminatedString(string line)
    {
        return line.TrimEnd('~');
    }
    
    /// <summary>
    /// Parses zone parameters line: top_room reset_time reset_mode minlevel maxlevel maxplayers
    /// Example: "7849 45 2 128 128 128"
    /// </summary>
    private void ParseZoneParameters(string line, Zone zone)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 6)
            throw new ParseException($"Invalid zone parameters format: {line}");
            
        zone.TopRoom = int.Parse(parts[0]);
        zone.ResetTime = int.Parse(parts[1]);
        zone.ResetMode = (ResetMode)int.Parse(parts[2]);
        zone.MinLevel = int.Parse(parts[3]);
        zone.MaxPlayers = int.Parse(parts[4]);
        // parts[5] appears to be unused in most zone files
    }
    
    /// <summary>
    /// Parses reset commands from the zone data
    /// </summary>
    private void ParseResetCommands(string[] lines, ref int lineIndex, Zone zone)
    {
        while (lineIndex < lines.Length)
        {
            var line = lines[lineIndex].Trim();
            
            // Check for end of reset commands
            if (line == "S" || line.StartsWith("$~") || line.StartsWith("#999999"))
                break;
                
            // Skip comment lines
            if (line == "*" || line.StartsWith("*"))
            {
                lineIndex++;
                continue;
            }
            
            // Parse reset command
            var command = ParseResetCommand(line);
            if (command != null)
            {
                zone.ResetCommands.Add(command);
            }
            
            lineIndex++;
        }
    }
    
    /// <summary>
    /// Parses a single reset command from a line
    /// </summary>
    private ResetCommand? ParseResetCommand(string line)
    {
        var match = ResetCommandRegex.Match(line.Trim());
        if (!match.Success)
            return null; // Skip invalid lines
            
        var command = new ResetCommand();
        
        // Parse command type
        var commandChar = match.Groups[1].Value[0];
        command.CommandType = (ResetCommandType)commandChar;
        
        // Parse arguments
        command.Arg1 = int.Parse(match.Groups[2].Value);
        command.Arg2 = int.Parse(match.Groups[3].Value);
        
        if (match.Groups[4].Success)
            command.Arg3 = int.Parse(match.Groups[4].Value);
            
        if (match.Groups[5].Success)
            command.Arg4 = int.Parse(match.Groups[5].Value);
            
        if (match.Groups[6].Success)
            command.Arg5 = int.Parse(match.Groups[6].Value);
        
        return command;
    }
}