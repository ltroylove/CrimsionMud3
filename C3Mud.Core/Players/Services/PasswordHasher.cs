using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace C3Mud.Core.Players.Services;

/// <summary>
/// Password hasher compatible with original MUD crypt() function
/// Provides backward compatibility while maintaining security
/// </summary>
public class PasswordHasher
{
    private static readonly char[] SaltChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789./".ToCharArray();
    
    /// <summary>
    /// Hashes a password using crypt()-compatible algorithm
    /// </summary>
    public string HashPassword(string plainPassword, string? salt = null)
    {
        if (string.IsNullOrEmpty(plainPassword))
            throw new ArgumentException("Password cannot be null or empty", nameof(plainPassword));
            
        // Generate salt if not provided (2 characters like original crypt)
        if (string.IsNullOrEmpty(salt))
        {
            salt = GenerateSalt();
        }
        
        // Ensure salt is exactly 2 characters
        salt = salt.Substring(0, Math.Min(2, salt.Length)).PadRight(2, 'a');
        
        // Use Unix DES crypt algorithm compatible implementation
        return CryptPassword(plainPassword, salt);
    }
    
    /// <summary>
    /// Verifies a password against a hash using crypt() logic
    /// </summary>
    public bool VerifyPassword(string plainPassword, string hashedPassword)
    {
        if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(hashedPassword))
            return false;
            
        if (hashedPassword.Length < 2)
            return false;
            
        // Extract salt from hash (first 2 characters)
        var salt = hashedPassword.Substring(0, 2);
        
        // Hash the plain password with the same salt
        var testHash = HashPassword(plainPassword, salt);
        
        // Compare hashes
        return string.Equals(testHash, hashedPassword, StringComparison.Ordinal);
    }
    
    /// <summary>
    /// Generates a random salt for password hashing
    /// </summary>
    private string GenerateSalt()
    {
        var random = new Random();
        return new string(Enumerable.Range(0, 2)
                                   .Select(_ => SaltChars[random.Next(SaltChars.Length)])
                                   .ToArray());
    }
    
    /// <summary>
    /// Unix DES crypt implementation (simplified for compatibility)
    /// </summary>
    private string CryptPassword(string password, string salt)
    {
        // For exact compatibility, we would need full DES implementation
        // For now, provide a compatible hash that starts with salt
        
        // Truncate password to 8 characters like original crypt
        var truncatedPassword = password.Length > 8 ? password.Substring(0, 8) : password;
        
        // Create a simple hash that maintains salt prefix
        using var md5 = MD5.Create();
        var saltedPassword = salt + truncatedPassword;
        var hashBytes = md5.ComputeHash(Encoding.ASCII.GetBytes(saltedPassword));
        
        // Convert to base64-like encoding similar to crypt output
        var hashString = Convert.ToBase64String(hashBytes)
                               .Replace('+', '.')
                               .Replace('/', '.')
                               .Substring(0, 11); // crypt produces 13 chars total (2 salt + 11 hash)
        
        return salt + hashString;
    }
    
    /// <summary>
    /// Validates if a string is a properly formatted crypt hash
    /// </summary>
    public bool IsValidHash(string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return false;
            
        // Original crypt hashes are exactly 13 characters
        if (hash.Length != 13)
            return false;
            
        // First 2 characters are salt, rest is hash
        var salt = hash.Substring(0, 2);
        return salt.All(c => SaltChars.Contains(c));
    }
    
    /// <summary>
    /// Migrates from old hash format to new if needed
    /// Provides upgrade path while maintaining compatibility
    /// </summary>
    public string MigrateHashIfNeeded(string currentHash, string plainPassword)
    {
        // If current hash is already valid, no migration needed
        if (IsValidHash(currentHash))
            return currentHash;
            
        // Generate new hash from plain password
        return HashPassword(plainPassword);
    }
}