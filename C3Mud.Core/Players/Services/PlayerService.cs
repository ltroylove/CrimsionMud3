using C3Mud.Core.Players.Models;

namespace C3Mud.Core.Players.Services;

/// <summary>
/// Service for player data operations and conversions
/// </summary>
public class PlayerService : IPlayerService
{
    private readonly LegacyPlayerFileRepository _playerRepository;
    
    public PlayerService(LegacyPlayerFileRepository? playerRepository = null)
    {
        _playerRepository = playerRepository ?? new LegacyPlayerFileRepository("playerfiles");
    }
    
    /// <summary>
    /// Converts legacy player file data to modern Player object
    /// </summary>
    public Player ConvertFromLegacyFormat(LegacyPlayerFileData legacyData)
    {
        // TODO: INCOMPLETE CONVERSION - Only mapping 2 fields out of 50+ available
        // FAILING TESTS: LegacyPlayerFileFormatTests.PlayerService_ConvertToLegacyFormat_ShouldMapAllFields
        // MISSING CONVERSIONS:
        // 1. All ability scores (Str, Int, Wis, Dex, Con, Cha)
        // 2. Hit points, mana, movement points
        // 3. Experience, gold, bank balance
        // 4. Skills array (200 skills)
        // 5. Affected spells/conditions
        // 6. Equipment and inventory
        // 7. Player description, title
        // 8. Last logon time, total played time
        // 9. All the other 40+ fields from LegacyPlayerFileData
        
        var playerId = Guid.NewGuid().ToString();
        var player = new Player(playerId)
        {
            Name = legacyData.Name,
            Level = legacyData.Level
        };
        
        return player;
    }
    
    /// <summary>
    /// Converts modern Player object to legacy file format
    /// </summary>
    public LegacyPlayerFileData ConvertToLegacyFormat(Player player)
    {
        // TODO: INCOMPLETE CONVERSION - Only setting name and level, missing all other data
        // FAILING TESTS: LegacyPlayerFileFormatTests.PlayerService_ConvertToLegacyFormat_ShouldMapAllFields
        // This method needs to populate ALL fields in LegacyPlayerFileData:
        // MISSING MAPPINGS:
        // 1. All ability scores and modifiers
        // 2. Hit points, mana, movement (current and max)
        // 3. Experience points and alignment
        // 4. All 200 skills with learned percentages
        // 5. Equipment slots and inventory items
        // 6. Player description and title strings
        // 7. Time-based data (birth, last logon, played time)
        // 8. Combat data (AC, hitroll, damroll)
        // 9. Preferences and configurations
        // 10. All remaining 40+ fields from the original structure
        
        var legacyData = new LegacyPlayerFileData();
        legacyData.SetName(player.Name);
        legacyData.Level = (sbyte)player.Level;
        
        // Initialize required arrays
        legacyData.Skills = new LegacyCharSkillData[200];
        legacyData.Affected = new LegacyAffectedType[50];
        legacyData.ApplySavingThrow = new short[5];
        legacyData.Conditions = new short[3];
        
        return legacyData;
    }
    
    /// <summary>
    /// Saves player data to persistent storage
    /// </summary>
    public async Task SavePlayerAsync(IPlayer player)
    {
        var modernPlayer = player as Player ?? throw new ArgumentException("Invalid player type");
        var legacyData = ConvertToLegacyFormat(modernPlayer);
        await _playerRepository.SavePlayerAsync(legacyData);
    }
    
    /// <summary>
    /// Loads player data from persistent storage
    /// </summary>
    public async Task<Player?> LoadPlayerAsync(string playerName)
    {
        var legacyData = await _playerRepository.LoadPlayerAsync(playerName);
        return legacyData?.ConvertFromLegacyFormat();
    }
}

/// <summary>
/// Interface for player service
/// </summary>
public interface IPlayerService
{
    Player ConvertFromLegacyFormat(LegacyPlayerFileData legacyData);
    LegacyPlayerFileData ConvertToLegacyFormat(Player player);
    Task SavePlayerAsync(IPlayer player);
    Task<Player?> LoadPlayerAsync(string playerName);
}

/// <summary>
/// Extension method for legacy data conversion
/// </summary>
public static class LegacyPlayerFileDataExtensions
{
    public static Player ConvertFromLegacyFormat(this LegacyPlayerFileData legacyData)
    {
        var playerId = Guid.NewGuid().ToString();
        var player = new Player(playerId)
        {
            Name = legacyData.Name,
            Level = legacyData.Level
        };
        
        return player;
    }
}