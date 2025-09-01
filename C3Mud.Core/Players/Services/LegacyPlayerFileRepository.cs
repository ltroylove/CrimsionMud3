using C3Mud.Core.Players.Models;
using System.Runtime.InteropServices;

namespace C3Mud.Core.Players.Services;

/// <summary>
/// Repository for reading/writing legacy MUD player files
/// Maintains exact binary compatibility with original player file format
/// </summary>
public class LegacyPlayerFileRepository
{
    private readonly string _playerFilesDirectory;
    private const string PlayerFileExtension = ".plr";
    
    public LegacyPlayerFileRepository(string playerFilesDirectory)
    {
        _playerFilesDirectory = playerFilesDirectory ?? throw new ArgumentNullException(nameof(playerFilesDirectory));
        
        // TODO: FILE SYSTEM INTEGRATION RISK - Directory creation may fail
        // POTENTIAL ISSUES:
        // 1. Insufficient permissions to create directory
        // 2. Invalid path characters or reserved names
        // 3. Path too long for filesystem
        // 4. Disk space issues
        // 5. Network drive connectivity issues
        // MISSING: Proper error handling and logging for directory operations
        
        // Ensure directory exists
        if (!Directory.Exists(_playerFilesDirectory))
        {
            Directory.CreateDirectory(_playerFilesDirectory);
        }
    }
    
    /// <summary>
    /// Loads a player from the legacy binary file format
    /// </summary>
    public async Task<LegacyPlayerFileData?> LoadPlayerAsync(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            throw new ArgumentException("Player name cannot be null or empty", nameof(playerName));
            
        var filePath = GetPlayerFilePath(playerName);
        
        if (!File.Exists(filePath))
            return null;
            
        try
        {
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            
            // TODO: BINARY COMPATIBILITY RISK - Marshaling may fail on different platforms
            // POTENTIAL ISSUES:
            // 1. Struct layout differences between C and C# (padding, alignment)
            // 2. Endianness differences on different platforms
            // 3. File corruption or partial reads
            // 4. Version differences in struct layout
            // 5. Size mismatches between file and expected struct size
            // MISSING: Validation of file size, magic numbers, version checking
            
            // Convert byte array to struct using marshaling
            var handle = GCHandle.Alloc(fileBytes, GCHandleType.Pinned);
            try
            {
                var playerData = Marshal.PtrToStructure<LegacyPlayerFileData>(handle.AddrOfPinnedObject());
                return playerData;
            }
            finally
            {
                handle.Free();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load player {playerName}: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Saves a player to the legacy binary file format
    /// </summary>
    public async Task SavePlayerAsync(LegacyPlayerFileData playerData)
    {
        if (string.IsNullOrEmpty(playerData.Name))
            throw new ArgumentException("Player data must have a valid name");
            
        var filePath = GetPlayerFilePath(playerData.Name);
        
        try
        {
            // Convert struct to byte array using marshaling
            var size = Marshal.SizeOf<LegacyPlayerFileData>();
            var buffer = new byte[size];
            
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                Marshal.StructureToPtr(playerData, handle.AddrOfPinnedObject(), false);
                await File.WriteAllBytesAsync(filePath, buffer);
            }
            finally
            {
                handle.Free();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save player {playerData.Name}: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Checks if a player file exists
    /// </summary>
    public bool PlayerExists(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            return false;
            
        var filePath = GetPlayerFilePath(playerName);
        return File.Exists(filePath);
    }
    
    /// <summary>
    /// Deletes a player file
    /// </summary>
    public async Task DeletePlayerAsync(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            throw new ArgumentException("Player name cannot be null or empty", nameof(playerName));
            
        var filePath = GetPlayerFilePath(playerName);
        
        if (File.Exists(filePath))
        {
            await Task.Run(() => File.Delete(filePath));
        }
    }
    
    /// <summary>
    /// Lists all player names in the directory
    /// </summary>
    public async Task<IEnumerable<string>> GetAllPlayerNamesAsync()
    {
        return await Task.Run(() =>
        {
            if (!Directory.Exists(_playerFilesDirectory))
                return Enumerable.Empty<string>();
                
            return Directory.GetFiles(_playerFilesDirectory, $"*{PlayerFileExtension}")
                           .Select(Path.GetFileNameWithoutExtension)
                           .Where(name => !string.IsNullOrEmpty(name))
                           .ToList();
        });
    }
    
    /// <summary>
    /// Gets the file path for a player's data file
    /// Normalizes name to match original MUD file naming (uppercase first letter)
    /// </summary>
    private string GetPlayerFilePath(string playerName)
    {
        // Normalize name like original MUD (capitalize first letter, lowercase rest)
        var normalizedName = char.ToUpper(playerName[0]) + playerName.Substring(1).ToLower();
        return Path.Combine(_playerFilesDirectory, normalizedName + PlayerFileExtension);
    }
    
    /// <summary>
    /// Creates a backup of a player file before overwriting
    /// Matches original MUD backup behavior
    /// </summary>
    private async Task CreateBackupAsync(string playerName)
    {
        var originalPath = GetPlayerFilePath(playerName);
        
        if (!File.Exists(originalPath))
            return;
            
        var backupPath = originalPath + ".bak";
        
        try
        {
            if (File.Exists(backupPath))
                File.Delete(backupPath);
                
            await Task.Run(() => File.Copy(originalPath, backupPath));
        }
        catch
        {
            // Backup failure should not prevent saving
            // Original MUD continued operation even if backup failed
        }
    }
    
    /// <summary>
    /// Validates the binary file format structure
    /// </summary>
    public async Task<bool> ValidatePlayerFileAsync(string playerName)
    {
        try
        {
            var playerData = await LoadPlayerAsync(playerName);
            
            if (playerData == null)
                return false;
                
            // Basic validation checks
            if (string.IsNullOrEmpty(playerData.Value.Name))
                return false;
                
            if (playerData.Value.Level < 1 || playerData.Value.Level > 200) // Reasonable level range
                return false;
                
            return true;
        }
        catch
        {
            return false;
        }
    }
}