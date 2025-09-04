using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using C3Mud.Core.World.Models;

namespace C3Mud.Core.World.Parsers;

/// <summary>
/// Parser for CircleMUD/DikuMUD .mob files
/// Handles parsing mobile (NPC/Monster) definitions from legacy text format
/// </summary>
public class MobileFileParser
{
    private static readonly Regex VNumRegex = new Regex(@"^#(\d+)$", RegexOptions.Compiled);
    private static readonly Regex SkillRegex = new Regex(@"^SKILL=(.+?)\s+(\d+)$", RegexOptions.Compiled);
    private static readonly Regex AttackSkillRegex = new Regex(@"^ATTACK_SKILL=(\d+)$", RegexOptions.Compiled);
    private static readonly Regex AttackTypeRegex = new Regex(@"^ATTACK_TYPE=(\d+)$", RegexOptions.Compiled);
    
    /// <summary>
    /// Parses a single mobile from raw text data
    /// </summary>
    /// <param name="mobileData">Raw mobile data from .mob file</param>
    /// <returns>Parsed Mobile object</returns>
    /// <exception cref="ParseException">Thrown when mobile data is malformed</exception>
    public Mobile ParseMobile(string mobileData)
    {
        if (string.IsNullOrWhiteSpace(mobileData))
            throw new ParseException("Mobile data cannot be null or empty");
            
        var lines = mobileData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd())
            .ToArray();
            
        if (lines.Length < 8)
            throw new ParseException("Mobile data has insufficient lines");
            
        var mobile = new Mobile();
        var lineIndex = 0;
        
        try
        {
            // Parse virtual number (#7750)
            mobile.VirtualNumber = ParseVirtualNumber(lines[lineIndex++]);
            
            // Parse keywords (mob jotun storm giant~)
            mobile.Keywords = ParseTildeTerminatedString(lines[lineIndex++]);
            
            // Parse short description (&BJotun&w, ruler of the &cStorm Giants&n~)
            mobile.ShortDescription = ParseTildeTerminatedString(lines[lineIndex++]);
            
            // Parse long description (&wA large and very powerful looking &cstorm giant&w stands here.&n~)
            mobile.LongDescription = ParseTildeTerminatedString(lines[lineIndex++]);
            
            // Skip the ~ separator line
            if (lineIndex < lines.Length && lines[lineIndex].Trim() == "~")
                lineIndex++;
                
            // Parse detailed description (multi-line until ~)
            mobile.DetailedDescription = ParseMultilineDescription(lines, ref lineIndex);
            
            // Skip the ~ separator line
            if (lineIndex < lines.Length && lines[lineIndex].Trim() == "~")
                lineIndex++;
                
            // Parse flags line (1082400782 1048577 136249393 536871080 950 S)
            ParseFlagsLine(lines[lineIndex++], mobile);
            
            // Parse stats line (42 2 2 20d30+8000 20d3+15)
            ParseStatsLine(lines[lineIndex++], mobile);
            
            // Parse experience and gold line (1000000 400000)
            ParseExperienceGoldLine(lines[lineIndex++], mobile);
            
            // Parse position and default position line (8 8 1)
            ParsePositionLine(lines[lineIndex++], mobile);
            
            // Parse ability scores line (24 4 26 17 14 15 25 15)
            ParseAbilityScores(lines[lineIndex++], mobile);
            
            // Parse remaining lines for skills and special attacks
            ParseSpecialAbilities(lines, lineIndex, mobile);
            
            return mobile;
        }
        catch (Exception ex) when (!(ex is ParseException))
        {
            throw new ParseException($"Error parsing mobile at line {lineIndex + 1}: {ex.Message}", ex);
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
    /// Parses multi-line descriptions until encountering a ~ on its own line
    /// </summary>
    private string ParseMultilineDescription(string[] lines, ref int lineIndex)
    {
        var description = new List<string>();
        
        while (lineIndex < lines.Length)
        {
            var line = lines[lineIndex];
            if (line.Trim() == "~")
                break;
                
            description.Add(line);
            lineIndex++;
        }
        
        return string.Join("\n", description);
    }
    
    /// <summary>
    /// Parses the mobile flags line: actionBits affectionBits alignment hitrollFlags damrollFlags position
    /// </summary>
    private void ParseFlagsLine(string line, Mobile mobile)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 6)
            throw new ParseException($"Invalid flags line format: {line}");
            
        mobile.MobileFlags = long.Parse(parts[0]);
        mobile.AffectionFlags = long.Parse(parts[1]);
        mobile.Alignment = int.Parse(parts[2]);
        // Skip hitrollFlags and damrollFlags (parts[3] and parts[4])
        mobile.DefaultPosition = parts[5] == "S" ? 8 : int.Parse(parts[5]); // S = standing = 8
    }
    
    /// <summary>
    /// Parses the stats line: level thac0 ac hpdice damdice
    /// </summary>
    private void ParseStatsLine(string line, Mobile mobile)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5)
            throw new ParseException($"Invalid stats line format: {line}");
            
        mobile.Level = int.Parse(parts[0]);
        // Skip thac0 (parts[1])
        mobile.ArmorClass = int.Parse(parts[2]);
        mobile.DamageRoll = parts[3];
        
        if (parts.Length > 4)
            mobile.BonusDamageRoll = parts[4];
    }
    
    /// <summary>
    /// Parses experience and gold line: experience gold
    /// </summary>
    private void ParseExperienceGoldLine(string line, Mobile mobile)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            throw new ParseException($"Invalid experience/gold line format: {line}");
            
        mobile.Experience = int.Parse(parts[0]);
        mobile.Gold = int.Parse(parts[1]);
    }
    
    /// <summary>
    /// Parses position line: position defaultPosition sex
    /// </summary>
    private void ParsePositionLine(string line, Mobile mobile)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            throw new ParseException($"Invalid position line format: {line}");
            
        mobile.Position = int.Parse(parts[0]);
        mobile.DefaultPosition = int.Parse(parts[1]);
        mobile.Sex = int.Parse(parts[2]);
    }
    
    /// <summary>
    /// Parses ability scores: str strAdd int wis dex con cha size
    /// </summary>
    private void ParseAbilityScores(string line, Mobile mobile)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 8)
            throw new ParseException($"Invalid ability scores line format: {line}");
            
        mobile.Strength = int.Parse(parts[0]);
        mobile.StrengthAdd = int.Parse(parts[1]);
        mobile.Intelligence = int.Parse(parts[2]);
        mobile.Wisdom = int.Parse(parts[3]);
        mobile.Dexterity = int.Parse(parts[4]);
        mobile.Constitution = int.Parse(parts[5]);
        mobile.Charisma = int.Parse(parts[6]);
        mobile.Size = int.Parse(parts[7]);
    }
    
    /// <summary>
    /// Parses special abilities (skills, attack skills, attack types)
    /// </summary>
    private void ParseSpecialAbilities(string[] lines, int startIndex, Mobile mobile)
    {
        for (int i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            // Parse skills (SKILL=SPELL_FIREBALL 99)
            var skillMatch = SkillRegex.Match(line);
            if (skillMatch.Success)
            {
                var skillName = skillMatch.Groups[1].Value;
                var skillLevel = int.Parse(skillMatch.Groups[2].Value);
                mobile.Skills[skillName] = skillLevel;
                continue;
            }
            
            // Parse attack skills (ATTACK_SKILL=70)
            var attackSkillMatch = AttackSkillRegex.Match(line);
            if (attackSkillMatch.Success)
            {
                var attackSkill = int.Parse(attackSkillMatch.Groups[1].Value);
                mobile.AttackSkills.Add(attackSkill);
                continue;
            }
            
            // Parse attack types (ATTACK_TYPE=108)
            var attackTypeMatch = AttackTypeRegex.Match(line);
            if (attackTypeMatch.Success)
            {
                var attackType = int.Parse(attackTypeMatch.Groups[1].Value);
                mobile.AttackTypes.Add(attackType);
                continue;
            }
        }
    }
}