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
        if (string.IsNullOrEmpty(legacyData.Name))
            throw new ArgumentException("Legacy data must have a valid name", nameof(legacyData));
            
        var playerId = Guid.NewGuid().ToString();
        var player = new Player(playerId)
        {
            Name = legacyData.Name,
            Level = legacyData.Level,
            
            // Map hit points from legacy points structure
            HitPoints = legacyData.Points.Hit,
            MaxHitPoints = legacyData.Points.MaxHit,
            
            // Map experience and gold
            ExperiencePoints = legacyData.Points.Experience,
            Gold = legacyData.Points.Gold,
            
            // Map position from legacy data (if applicable)
            Position = PlayerPosition.Standing, // Default to standing
            
            // Map current room from legacy start room
            CurrentRoomVnum = legacyData.StartRoom > 0 ? legacyData.StartRoom : 20385, // Default to starting room
            
            // Store the full legacy data for complete preservation
            LegacyPlayerFileData = legacyData
        };
        
        return player;
    }
    
    /// <summary>
    /// Converts modern Player object to legacy file format
    /// </summary>
    public LegacyPlayerFileData ConvertToLegacyFormat(Player player)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));
        if (string.IsNullOrEmpty(player.Name))
            throw new ArgumentException("Player must have a valid name", nameof(player));
            
        // Start with existing legacy data if available, otherwise create new
        var legacyData = player.LegacyPlayerFileData ?? new LegacyPlayerFileData();
        
        // Core character data
        legacyData.SetName(player.Name);
        legacyData.Level = (sbyte)Math.Max(1, Math.Min(127, player.Level)); // Ensure valid sbyte range
        
        // Character class and race - get from existing legacy data or use defaults
        if (!player.LegacyPlayerFileData.HasValue)
        {
            legacyData.Class = (sbyte)player.GetCharacterClass();
            legacyData.Race = 0; // Default to human (race 0)
            legacyData.Sex = 1; // Default to male (1)
        }
        
        // Hit points, mana, movement
        legacyData.Points.Hit = (short)Math.Max(0, Math.Min(short.MaxValue, player.HitPoints));
        legacyData.Points.MaxHit = (short)Math.Max(0, Math.Min(short.MaxValue, player.MaxHitPoints));
        
        // Experience and gold
        legacyData.Points.Experience = Math.Max(0, player.ExperiencePoints);
        legacyData.Points.Gold = Math.Max(0, player.Gold);
        
        // Room location
        legacyData.StartRoom = (short)Math.Max(0, Math.Min(short.MaxValue, player.CurrentRoomVnum));
        
        // Time data - if not already set from legacy data
        if (!player.LegacyPlayerFileData.HasValue)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            legacyData.Birth = now;
            legacyData.LastLogon = now;
            legacyData.LogoffTime = now;
            legacyData.Played = 0; // Start with 0 play time
        }
        
        // Abilities - use reasonable defaults if not in legacy data
        if (!player.LegacyPlayerFileData.HasValue)
        {
            legacyData.Abilities.Strength = (byte)Math.Max(3, Math.Min(25, player.Strength));
            legacyData.Abilities.Dexterity = (byte)Math.Max(3, Math.Min(25, player.Dexterity));
            legacyData.Abilities.Constitution = (byte)Math.Max(3, Math.Min(25, player.Constitution));
            legacyData.Abilities.Intelligence = 13; // Default
            legacyData.Abilities.Wisdom = 13; // Default
            legacyData.Abilities.Charisma = 13; // Default
            legacyData.Abilities.StrengthAdd = 0; // No 18/xx strength by default
        }
        
        // Combat stats
        legacyData.Points.Armor = (byte)Math.Max(0, Math.Min(255, player.ArmorClass));
        
        // Alignment - convert from player alignment if available
        var alignment = player.GetAlignment();
        legacyData.Alignment = (int)alignment;
        
        // Initialize arrays if they don't exist
        legacyData.Skills ??= new LegacyCharSkillData[200];
        legacyData.Affected ??= new LegacyAffectedType[50];
        legacyData.ApplySavingThrow ??= new short[5];
        legacyData.Conditions ??= new short[3];
        
        // Set default saving throws if not already set
        if (!player.LegacyPlayerFileData.HasValue)
        {
            // Initialize saving throws to reasonable defaults based on class and level
            var baseThrow = Math.Max(1, 20 - player.Level / 2);
            for (int i = 0; i < 5; i++)
            {
                legacyData.ApplySavingThrow[i] = (short)baseThrow;
            }
            
            // Initialize conditions (hunger, thirst, drunk)
            legacyData.Conditions[0] = 24; // Full
            legacyData.Conditions[1] = 24; // Thirst satisfied  
            legacyData.Conditions[2] = 0;  // Sober
        }
        
        // Physical attributes defaults
        if (!player.LegacyPlayerFileData.HasValue)
        {
            legacyData.Weight = 150; // Default weight
            legacyData.Height = 70;  // Default height
            legacyData.VisibleLevel = (sbyte)player.Level; // Visible at own level
        }
        
        // Quest system defaults
        if (!player.LegacyPlayerFileData.HasValue)
        {
            legacyData.QuestPoints = 0;
            legacyData.NextQuest = 0;
        }
        
        // Screen settings
        if (!player.LegacyPlayerFileData.HasValue)
        {
            legacyData.ScreenLines = 24; // Default screen height
        }
        
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
        var playerService = new PlayerService();
        return playerService.ConvertFromLegacyFormat(legacyData);
    }
}