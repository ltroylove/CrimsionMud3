using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using C3Mud.Core.World.Models;

namespace C3Mud.Core.World.Parsers;

/// <summary>
/// Parser for CircleMUD/DikuMUD .obj files
/// Handles parsing object (item/equipment) definitions from legacy text format
/// </summary>
public class ObjectFileParser
{
    private static readonly Regex VNumRegex = new Regex(@"^#(\d+)$", RegexOptions.Compiled);
    private static readonly Regex ApplyRegex = new Regex(@"^A$", RegexOptions.Compiled);
    private static readonly Regex ExtraDescRegex = new Regex(@"^E$", RegexOptions.Compiled);
    
    /// <summary>
    /// Parses a single object from raw text data
    /// </summary>
    /// <param name="objectData">Raw object data from .obj file</param>
    /// <returns>Parsed WorldObject</returns>
    /// <exception cref="ParseException">Thrown when object data is malformed</exception>
    public WorldObject ParseObject(string objectData)
    {
        if (string.IsNullOrWhiteSpace(objectData))
            throw new ParseException("Object data cannot be null or empty");
            
        var lines = objectData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd())
            .ToArray();
            
        if (lines.Length < 7)
            throw new ParseException("Object data has insufficient lines");
            
        var obj = new WorldObject();
        var lineIndex = 0;
        
        try
        {
            // Parse virtual number (#7750)
            obj.VirtualNumber = ParseVirtualNumber(lines[lineIndex++]);
            
            // Parse keywords (obj thunder hammer giant~)
            obj.Name = ParseTildeTerminatedString(lines[lineIndex++]);
            
            // Parse short description (&wa &YThunder &wHammer&n~)
            obj.ShortDescription = ParseTildeTerminatedString(lines[lineIndex++]);
            
            // Parse long description (A giant hammer surging with elictrical power lies here.~)
            obj.LongDescription = ParseTildeTerminatedString(lines[lineIndex++]);
            
            // Parse action description (~)
            obj.ActionDescription = ParseTildeTerminatedString(lines[lineIndex++]);
            
            // Parse type and flags line (5 33557569 32 8193)
            ParseTypeFlagsLine(lines[lineIndex++], obj);
            
            // Parse values line (262272 7 7 6)
            ParseValuesLine(lines[lineIndex++], obj);
            
            // Parse weight, cost, rent line (15 141000 500)
            ParseWeightCostRentLine(lines[lineIndex++], obj);
            
            // Parse remaining lines for applies and extra descriptions
            ParseSpecialProperties(lines, lineIndex, obj);
            
            return obj;
        }
        catch (Exception ex) when (!(ex is ParseException))
        {
            throw new ParseException($"Error parsing object at line {lineIndex + 1}: {ex.Message}", ex);
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
    /// Parses tilde-terminated string, removing the ~ at the end
    /// </summary>
    private string ParseTildeTerminatedString(string line)
    {
        var trimmed = line.TrimEnd();
        if (trimmed.EndsWith("~"))
            return trimmed.Substring(0, trimmed.Length - 1);
        return trimmed;
    }
    
    /// <summary>
    /// Parses the type and flags line: type extra_flags wear_flags anti_flags
    /// Format: "5 33557569 32 8193"
    /// </summary>
    private void ParseTypeFlagsLine(string line, WorldObject obj)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            throw new ParseException($"Invalid type/flags line format: {line}");
            
        // Object type
        if (int.TryParse(parts[0], out int type))
        {
            obj.ObjectType = (ObjectType)type;
        }
        
        // Extra flags
        if (long.TryParse(parts[1], out long extraFlags))
        {
            obj.ExtraFlags = extraFlags;
        }
        
        // Wear flags
        if (long.TryParse(parts[2], out long wearFlags))
        {
            obj.WearFlags = wearFlags;
        }
        
        // Anti flags (part[3]) are typically merged into extra flags in modern implementations
    }
    
    /// <summary>
    /// Parses the values line with type-specific object values
    /// Format: "262272 7 7 6"
    /// </summary>
    private void ParseValuesLine(string line, WorldObject obj)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Initialize values array
        obj.Values = new int[4];
        
        for (int i = 0; i < Math.Min(parts.Length, 4); i++)
        {
            if (int.TryParse(parts[i], out int value))
            {
                obj.Values[i] = value;
            }
        }
    }
    
    /// <summary>
    /// Parses the weight, cost, and rent line
    /// Format: "15 141000 500"
    /// </summary>
    private void ParseWeightCostRentLine(string line, WorldObject obj)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            throw new ParseException($"Invalid weight/cost/rent line format: {line}");
            
        if (int.TryParse(parts[0], out int weight))
            obj.Weight = weight;
            
        if (int.TryParse(parts[1], out int cost))
            obj.Cost = cost;
            
        if (int.TryParse(parts[2], out int rent))
            obj.RentPerDay = rent;
    }
    
    /// <summary>
    /// Parses applies and extra descriptions
    /// Apply format: A, apply_type, apply_value
    /// Extra desc format: E, keywords~, description~
    /// </summary>
    private void ParseSpecialProperties(string[] lines, int startIndex, WorldObject obj)
    {
        for (int i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            if (line == "A" && i + 2 < lines.Length)
            {
                // Parse apply: A, apply_type, apply_value
                if (int.TryParse(lines[i + 1].Trim(), out int applyType) && 
                    int.TryParse(lines[i + 2].Trim(), out int applyValue))
                {
                    obj.Applies[applyType] = applyValue;
                }
                i += 2; // Skip the next two lines we just processed
            }
            else if (line == "E" && i + 2 < lines.Length)
            {
                // Parse extra description: E, keywords~, description~
                var keywords = ParseTildeTerminatedString(lines[i + 1]);
                var description = ParseTildeTerminatedString(lines[i + 2]);
                if (!string.IsNullOrEmpty(keywords))
                {
                    obj.ExtraDescriptions[keywords.ToLowerInvariant()] = description;
                }
                i += 2; // Skip the next two lines we just processed
            }
        }
    }
    
    /// <summary>
    /// Parses objects from a complete .obj file
    /// </summary>
    /// <param name="fileContent">Complete content of .obj file</param>
    /// <returns>Collection of parsed objects</returns>
    public IEnumerable<WorldObject> ParseFile(string fileContent)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
            return new List<WorldObject>();
            
        var objects = new List<WorldObject>();
        var lines = fileContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var currentObjectLines = new List<string>();
        var inObject = false;
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Check for object start (#12345)
            if (VNumRegex.IsMatch(trimmedLine))
            {
                // If we were processing an object, parse it
                if (inObject && currentObjectLines.Count > 0)
                {
                    var obj = TryParseObject(currentObjectLines);
                    if (obj != null)
                        objects.Add(obj);
                }
                
                // Start new object
                currentObjectLines.Clear();
                currentObjectLines.Add(line);
                inObject = true;
            }
            else if (inObject)
            {
                currentObjectLines.Add(line);
            }
            
            // Check for end marker
            if (trimmedLine.StartsWith("#99999") || trimmedLine.StartsWith("$~"))
                break;
        }
        
        // Parse final object if exists
        if (inObject && currentObjectLines.Count > 0)
        {
            var obj = TryParseObject(currentObjectLines);
            if (obj != null)
                objects.Add(obj);
        }
        
        return objects;
    }
    
    /// <summary>
    /// Attempts to parse an object from lines, returning null on failure
    /// </summary>
    /// <param name="objectLines">Lines containing object data</param>
    /// <returns>Parsed object or null if parsing failed</returns>
    private WorldObject? TryParseObject(List<string> objectLines)
    {
        try
        {
            var objectData = string.Join("\n", objectLines);
            return ParseObject(objectData);
        }
        catch (ParseException ex)
        {
            Console.WriteLine($"Warning: Failed to parse object: {ex.Message}");
            return null;
        }
    }
}