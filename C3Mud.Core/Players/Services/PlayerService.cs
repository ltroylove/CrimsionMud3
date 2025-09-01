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