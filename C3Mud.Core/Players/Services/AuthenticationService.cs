using C3Mud.Core.Players.Models;
using Microsoft.Extensions.Logging;

namespace C3Mud.Core.Players.Services;

/// <summary>
/// Authentication service providing legacy MUD authentication compatibility
/// Handles player login, account creation, and password management
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly LegacyPlayerFileRepository _playerRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly ILogger<AuthenticationService> _logger;
    
    public AuthenticationService(
        LegacyPlayerFileRepository? playerRepository = null,
        PasswordHasher? passwordHasher = null,
        ILogger<AuthenticationService>? logger = null)
    {
        _playerRepository = playerRepository ?? new LegacyPlayerFileRepository("playerfiles");
        _passwordHasher = passwordHasher ?? new PasswordHasher();
        _logger = logger ?? CreateDefaultLogger();
    }
    
    /// <summary>
    /// Authenticates a player with username and password
    /// </summary>
    public async Task<AuthenticationResult> AuthenticatePlayerAsync(string username, string password)
    {
        try
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return AuthenticationResult.Failure("Username and password are required");
            }
            
            // Load player data from legacy file
            var legacyData = await _playerRepository.LoadPlayerAsync(username);
            
            if (legacyData == null)
            {
                _logger.LogWarning("Authentication attempt for non-existent player: {Username}", username);
                return AuthenticationResult.Failure("Invalid username or password");
            }
            
            // Verify password using legacy hash
            if (!_passwordHasher.VerifyPassword(password, legacyData.Value.Password))
            {
                _logger.LogWarning("Failed password attempt for player: {Username}", username);
                return AuthenticationResult.Failure("Invalid username or password");
            }
            
            // Create modern Player object from legacy data
            var player = CreatePlayerFromLegacyData(legacyData.Value);
            
            _logger.LogInformation("Successful authentication for player: {Username}", username);
            return AuthenticationResult.Success(player);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for player: {Username}", username);
            return AuthenticationResult.Failure("Authentication service error");
        }
    }
    
    /// <summary>
    /// Creates a new player account
    /// </summary>
    public async Task<AuthenticationResult> CreatePlayerAccountAsync(string username, string password)
    {
        try
        {
            // Validate username according to legacy rules
            var nameValidationResult = ValidatePlayerName(username);
            if (!nameValidationResult.IsValid)
            {
                return AuthenticationResult.Failure(nameValidationResult.ErrorMessage);
            }
            
            // Validate password according to legacy rules
            var passwordValidationResult = ValidatePassword(password);
            if (!passwordValidationResult.IsValid)
            {
                return AuthenticationResult.Failure(passwordValidationResult.ErrorMessage);
            }
            
            // Check if player already exists
            if (_playerRepository.PlayerExists(username))
            {
                return AuthenticationResult.Failure("A player with that name already exists");
            }
            
            // TODO: MISSING INTEGRATION - File system operations may fail
            // The _playerRepository.PlayerExists() and SavePlayerAsync() calls may fail if:
            // 1. Player file directory doesn't exist
            // 2. File permissions are incorrect
            // 3. Disk space issues
            // 4. Path resolution problems
            // Need proper error handling and directory creation
            
            // Create legacy player data structure
            var legacyData = CreateDefaultLegacyPlayerData(username, password);
            
            // Save to legacy file format
            await _playerRepository.SavePlayerAsync(legacyData);
            
            // Create modern Player object
            var player = CreatePlayerFromLegacyData(legacyData);
            
            _logger.LogInformation("Created new player account: {Username}", username);
            return AuthenticationResult.Success(player);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating player account: {Username}", username);
            return AuthenticationResult.Failure("Account creation failed");
        }
    }
    
    /// <summary>
    /// Changes a player's password
    /// </summary>
    public async Task<AuthenticationResult> ChangePlayerPasswordAsync(string username, string oldPassword, string newPassword)
    {
        try
        {
            // First authenticate with old password
            var authResult = await AuthenticatePlayerAsync(username, oldPassword);
            if (!authResult.IsSuccess)
            {
                return AuthenticationResult.Failure("Current password is incorrect");
            }
            
            // Validate new password
            var passwordValidationResult = ValidatePassword(newPassword);
            if (!passwordValidationResult.IsValid)
            {
                return AuthenticationResult.Failure(passwordValidationResult.ErrorMessage);
            }
            
            // Load current player data
            var legacyData = await _playerRepository.LoadPlayerAsync(username);
            if (legacyData == null)
            {
                return AuthenticationResult.Failure("Player not found");
            }
            
            // Update password
            var updatedData = legacyData.Value;
            updatedData.SetPassword(_passwordHasher.HashPassword(newPassword));
            
            // Save updated data
            await _playerRepository.SavePlayerAsync(updatedData);
            
            _logger.LogInformation("Password changed for player: {Username}", username);
            return AuthenticationResult.Success(authResult.Player!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for player: {Username}", username);
            return AuthenticationResult.Failure("Password change failed");
        }
    }
    
    /// <summary>
    /// Validates if a player name exists
    /// </summary>
    public async Task<bool> ValidatePlayerNameAsync(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            return false;
            
        return await Task.FromResult(_playerRepository.PlayerExists(playerName));
    }
    
    /// <summary>
    /// Validates player name according to legacy MUD rules
    /// </summary>
    private (bool IsValid, string ErrorMessage) ValidatePlayerName(string playerName)
    {
        // TODO: VALIDATION MESSAGE MISMATCH - Error messages don't match test expectations
        // FAILING TESTS: AuthenticationServiceTests.CreatePlayerAccountAsync_InvalidUsername_ShouldRejectCreation
        // Tests expect error messages containing "invalid" but getting specific validation messages
        // REQUIRED FIXES:
        // 1. Change error messages to match original MUD validation output
        // 2. Possibly return generic "invalid" message instead of specific validation details
        // 3. Check if tests expect exact legacy validation message format
        // 4. Consider whether validation should be more/less strict than original
        
        if (string.IsNullOrEmpty(playerName))
            return (false, "Player name cannot be empty");
            
        if (playerName.Length < 2)
            return (false, "Player name must be at least 2 characters");
            
        if (playerName.Length > 19)
            return (false, "Player name cannot exceed 19 characters");
            
        if (!playerName.All(char.IsLetter))
            return (false, "Player name can only contain letters");
            
        if (!char.IsUpper(playerName[0]))
            return (false, "Player name must start with a capital letter");
            
        if (playerName.Skip(1).Any(char.IsUpper))
            return (false, "Player name can only have the first letter capitalized");
            
        // Check for reserved names (like original MUD)
        var reservedNames = new[] { "God", "Admin", "Immortal", "Player", "Guest", "Anonymous" };
        if (reservedNames.Any(reserved => string.Equals(reserved, playerName, StringComparison.OrdinalIgnoreCase)))
            return (false, "That name is reserved");
            
        return (true, string.Empty);
    }
    
    /// <summary>
    /// Validates password according to legacy MUD rules
    /// </summary>
    private (bool IsValid, string ErrorMessage) ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return (false, "Password cannot be empty");
            
        if (password.Length < 2)
            return (false, "Password must be at least 2 characters");
            
        if (password.Length > 10)
            return (false, "Password cannot exceed 10 characters");
            
        return (true, string.Empty);
    }
    
    /// <summary>
    /// Creates default legacy player data for new accounts
    /// </summary>
    private LegacyPlayerFileData CreateDefaultLegacyPlayerData(string username, string password)
    {
        var data = new LegacyPlayerFileData();
        
        // Set basic info
        data.SetName(username);
        data.SetPassword(_passwordHasher.HashPassword(password));
        
        // Set default values matching original MUD
        data.Level = 1;
        data.Sex = 1; // Default to male (original MUD behavior)
        data.Race = 1; // Default race
        data.Class = 1; // Default class
        data.Birth = DateTimeOffset.Now.ToUnixTimeSeconds();
        data.LastLogon = DateTimeOffset.Now.ToUnixTimeSeconds();
        data.Played = 0;
        
        // Initialize arrays
        data.Skills = new LegacyCharSkillData[200];
        data.Affected = new LegacyAffectedType[50];
        data.ApplySavingThrow = new short[5];
        data.Conditions = new short[3];
        
        // Set default stats
        data.Points = new LegacyCharPointData
        {
            Hit = 20,
            MaxHit = 20,
            Mana = 100,
            MaxMana = 100,
            Move = 100,
            MaxMove = 100,
            Experience = 0,
            Gold = 0,
            Bank = 0
        };
        
        // Set default abilities (like rolling stats)
        data.Abilities = new LegacyCharAbilityData
        {
            Strength = 16,
            Intelligence = 16,
            Wisdom = 16,
            Dexterity = 16,
            Constitution = 16,
            Charisma = 16
        };
        
        // Initialize strings
        data.Title = " the newbie";
        data.Description = "A new player stands here.";
        data.ImmortalEnter = "";
        data.ImmortalExit = "";
        data.EmailName = "";
        data.Filler = "";
        data.SpareString1 = "";
        data.SpareString2 = "";
        data.SpareString3 = "";
        data.SpareString4 = "";
        data.SpareString5 = "";
        
        return data;
    }
    
    /// <summary>
    /// Creates a modern Player object from legacy data
    /// </summary>
    private Player CreatePlayerFromLegacyData(LegacyPlayerFileData legacyData)
    {
        var playerId = Guid.NewGuid().ToString();
        var player = new Player(playerId)
        {
            Name = legacyData.Name,
            Level = legacyData.Level,
            // Add more properties as needed
        };
        
        return player;
    }
    
    /// <summary>
    /// Creates a default logger if none provided
    /// </summary>
    private static ILogger<AuthenticationService> CreateDefaultLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        return loggerFactory.CreateLogger<AuthenticationService>();
    }
}

/// <summary>
/// Interface for authentication service
/// </summary>
public interface IAuthenticationService
{
    Task<AuthenticationResult> AuthenticatePlayerAsync(string username, string password);
    Task<AuthenticationResult> CreatePlayerAccountAsync(string username, string password);
    Task<AuthenticationResult> ChangePlayerPasswordAsync(string username, string oldPassword, string newPassword);
    Task<bool> ValidatePlayerNameAsync(string playerName);
}