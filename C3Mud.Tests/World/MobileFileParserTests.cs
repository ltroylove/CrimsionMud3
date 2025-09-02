using Xunit;
using C3Mud.Core.World.Parsers;
using C3Mud.Core.World.Models;
using System;

namespace C3Mud.Tests.World;
public class MobileFileParserTests
{
    [Fact]
    public void ParseMobile_BasicFormat_ReturnsCorrectVNum()
    {
        // Test parsing mobile virtual number from #7750 format
        var mobileData = @"#7750
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
        var mobile = parser.ParseMobile(mobileData);
        
        Assert.Equal(7750, mobile.VirtualNumber);
    }
    
    [Fact]
    public void ParseMobile_BasicFormat_ReturnsCorrectKeywords()
    {
        // Test parsing mobile keywords
        var mobileData = @"#7750
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
        var mobile = parser.ParseMobile(mobileData);
        
        Assert.Equal("mob jotun storm giant", mobile.Keywords);
    }
    
    [Fact]
    public void ParseMobile_BasicFormat_ReturnsCorrectDescriptions()
    {
        // Test parsing mobile descriptions
        var mobileData = @"#7750
mob jotun storm giant~
&BJotun&w, ruler of the &cStorm Giants&n~
&wA large and very powerful looking &cstorm giant&w stands here.&n
~
Standing a massive twenty six feet tall, Jotun is an intimidating figure
to behold. Rippling muscles show through his cloak, and numerous scars of
battles fought long ago cover his face.
~
1082400782 1048577 136249393 536871080 950 S
42 2 2 20d30+8000 20d3+15
1000000 400000
8 8 1
24 4 26 17 14 15 25 15";

        var parser = new MobileFileParser();
        var mobile = parser.ParseMobile(mobileData);
        
        Assert.Equal("&BJotun&w, ruler of the &cStorm Giants&n", mobile.ShortDescription);
        Assert.Equal("&wA large and very powerful looking &cstorm giant&w stands here.&n", mobile.LongDescription);
        Assert.True(mobile.DetailedDescription.Contains("Standing a massive twenty six feet tall"));
    }
    
    [Fact]
    public void ParseMobile_CombatStats_ParsedCorrectly()
    {
        // Test parsing combat statistics
        var mobileData = @"#7750
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
        var mobile = parser.ParseMobile(mobileData);
        
        Assert.Equal(42, mobile.Level);
        Assert.Equal(2, mobile.ArmorClass);
        Assert.Equal("20d30+8000", mobile.DamageRoll);
        Assert.Equal("20d3+15", mobile.BonusDamageRoll);
        Assert.Equal(1000000, mobile.Experience);
        Assert.Equal(400000, mobile.Gold);
    }
    
    [Fact]
    public void ParseMobile_Stats_ParsedCorrectly()
    {
        // Test parsing ability scores
        var mobileData = @"#7750
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
        var mobile = parser.ParseMobile(mobileData);
        
        Assert.Equal(24, mobile.Strength);
        Assert.Equal(4, mobile.StrengthAdd);
        Assert.Equal(26, mobile.Intelligence);
        Assert.Equal(17, mobile.Wisdom);
        Assert.Equal(14, mobile.Dexterity);
        Assert.Equal(15, mobile.Constitution);
        Assert.Equal(25, mobile.Charisma);
        Assert.Equal(15, mobile.Size);
    }
    
    [Fact]
    public void ParseMobile_WithSkills_ParsesSkillsCorrectly()
    {
        // Test parsing mobile with skills
        var mobileData = @"#7751
mob jerial elven representative elf~
&GJerial&n~
&GJerial&w, representative of the &pElven Kingdom&w, stands here.&n
~
Description here...
~
270346 1 136249393 262312 950 S
36 7 -35 32d12+2500 6d8+30
80000 220000
8 8 2
14 6 19 23 15 14 12 12
SKILL=SPELL_FIREBALL 99
SKILL=SPELL_LIGHTNING_BOLT 99";

        var parser = new MobileFileParser();
        var mobile = parser.ParseMobile(mobileData);
        
        Assert.Equal(7751, mobile.VirtualNumber);
        Assert.Equal(2, mobile.Skills.Count);
        Assert.Equal(99, mobile.Skills["SPELL_FIREBALL"]);
        Assert.Equal(99, mobile.Skills["SPELL_LIGHTNING_BOLT"]);
    }
    
    [Fact]
    public void ParseMobile_WithAttackSkills_ParsesAttackSkillsCorrectly()
    {
        // Test parsing mobile with attack skills
        var mobileData = @"#7750
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
24 4 26 17 14 15 25 15
SKILL=SKILL_PARRY 40
ATTACK_SKILL=70
ATTACK_SKILL=50
ATTACK_SKILL=20
ATTACK_TYPE=108
ATTACK_SKILL=50
ATTACK_SKILL=0
ATTACK_SKILL=0
ATTACK_TYPE=100";

        var parser = new MobileFileParser();
        var mobile = parser.ParseMobile(mobileData);
        
        Assert.Equal(1, mobile.Skills.Count);
        Assert.Equal(40, mobile.Skills["SKILL_PARRY"]);
        Assert.Equal(6, mobile.AttackSkills.Count);
        Assert.Equal(70, mobile.AttackSkills[0]);
        Assert.Equal(50, mobile.AttackSkills[1]);
        Assert.Equal(2, mobile.AttackTypes.Count);
        Assert.Equal(108, mobile.AttackTypes[0]);
        Assert.Equal(100, mobile.AttackTypes[1]);
    }
    
    [Fact]
    public void ParseMobile_InvalidFormat_ThrowsParseException()
    {
        // Test error handling for malformed mobile data
        var invalidData = "Not a mobile format";
        var parser = new MobileFileParser();
        
        Assert.Throws<ParseException>(() => parser.ParseMobile(invalidData));
    }
    
    [Fact]
    public void ParseMobile_MissingVNum_ThrowsParseException()
    {
        // Test error handling for missing virtual number
        var invalidData = @"mob test~
Test Mobile~
A test mobile stands here.~
~
Description...
~";
        var parser = new MobileFileParser();
        
        Assert.Throws<ParseException>(() => parser.ParseMobile(invalidData));
    }
    
    [Fact]
    public void ParseMobile_ComplexMobile_AllFieldsParsedCorrectly()
    {
        // Test parsing the full complex mobile from Aerie.mob
        var mobileData = @"#7753
mob viana divine light~
&BViana&w, the &CDivine Light&n~
&wThe &Cdivine &BViana&w is here, searching for &revil&w.&n
~
    The beauty that is Viana transcends mortal words. She wears no armor, but
instead chooses to don a gown of the purest white so that those creatures of
evil will see her coming when she runs them through with her sword.
~
1342439436 135299073 136249393 51380910 1000 S
48 2 -30 100d10+35000 15d5+70
3000000 2000000
8 8 2
1 7 22 22 22 22 22 22
SKILL=SPELL_ELSEWHERE 99
SKILL=SPELL_DISPEL_EVIL 99
SKILL=SPELL_SUMMON 99
SKILL=SKILL_SNEAK 99
ATTACK_SKILL=90
ATTACK_SKILL=80
ATTACK_TYPE=103";

        var parser = new MobileFileParser();
        var mobile = parser.ParseMobile(mobileData);
        
        Assert.Equal(7753, mobile.VirtualNumber);
        Assert.Equal("mob viana divine light", mobile.Keywords);
        Assert.Equal(48, mobile.Level);
        Assert.Equal(-30, mobile.ArmorClass);
        Assert.Equal("100d10+35000", mobile.DamageRoll);
        Assert.Equal("15d5+70", mobile.BonusDamageRoll);
        Assert.Equal(3000000, mobile.Experience);
        Assert.Equal(2000000, mobile.Gold);
        Assert.Equal(1, mobile.Strength);
        Assert.Equal(4, mobile.Skills.Count);
        Assert.True(mobile.Skills.ContainsKey("SPELL_ELSEWHERE"));
        Assert.True(mobile.Skills.ContainsKey("SKILL_SNEAK"));
        Assert.Equal(2, mobile.AttackSkills.Count);
        Assert.Equal(1, mobile.AttackTypes.Count);
    }
}