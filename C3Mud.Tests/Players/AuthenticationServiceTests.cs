using Xunit;
using FluentAssertions;
using C3Mud.Core.Players;
using C3Mud.Core.Players.Services;
using C3Mud.Core.Players.Models;
using C3Mud.Core.Networking;
using Moq;

namespace C3Mud.Tests.Players;

/// <summary>
/// Tests for authentication system with legacy compatibility
/// Ensures exact matching with original MUD authentication behavior
/// </summary>
public class AuthenticationServiceTests
{
    [Fact]
    public void AuthenticationService_Constructor_ShouldInitialize()
    {
        // ARRANGE & ACT - Should fail as AuthenticationService doesn't exist
        var act = () => new AuthenticationService();
        
        // ASSERT
        act.Should().NotThrow("AuthenticationService should be constructible");
    }
    
    [Fact]
    public async Task AuthenticatePlayerAsync_ValidCredentials_ShouldReturnAuthenticatedPlayer()
    {
        // ARRANGE - Should fail as service doesn't exist
        var authService = new AuthenticationService();
        var username = "TestPlayer";
        var password = "validpass";
        
        // ACT
        var act = async () => await authService.AuthenticatePlayerAsync(username, password);
        
        // ASSERT
        act.Should().NotThrowAsync("AuthenticationService should authenticate valid players");
        
        var result = await authService.AuthenticatePlayerAsync(username, password);
        result.IsSuccess.Should().BeTrue("valid credentials should authenticate successfully");
        result.Player.Should().NotBeNull();
        result.Player.Name.Should().Be(username);
    }
    
    [Theory]
    [InlineData("TestPlayer", "wrongpass")]
    [InlineData("NonExistentPlayer", "anypass")]
    [InlineData("", "validpass")]
    [InlineData("TestPlayer", "")]
    public async Task AuthenticatePlayerAsync_InvalidCredentials_ShouldReturnFailure(string username, string password)
    {
        // ARRANGE - Should fail as service doesn't exist
        var authService = new AuthenticationService();
        
        // ACT
        var act = async () => await authService.AuthenticatePlayerAsync(username, password);
        
        // ASSERT
        act.Should().NotThrowAsync("should handle invalid credentials gracefully");
        
        var result = await authService.AuthenticatePlayerAsync(username, password);
        result.IsSuccess.Should().BeFalse("invalid credentials should fail authentication");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public void PasswordHasher_HashPassword_ShouldUseCryptCompatibleHashing()
    {
        // ARRANGE - Should fail as PasswordHasher doesn't exist
        // Must match original crypt() function behavior from parser.c line ~882
        var hasher = new PasswordHasher();
        var plainPassword = "testpass";
        var salt = "ab"; // 2-character salt like original
        
        // ACT
        var act = () => hasher.HashPassword(plainPassword, salt);
        
        // ASSERT
        act.Should().NotThrow("PasswordHasher should be implemented");
        
        var hashedPassword = hasher.HashPassword(plainPassword, salt);
        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Length.Should().Be(13, "crypt() produces 13-character hash");
        hashedPassword.Should().StartWith(salt, "hash should start with salt");
    }
    
    [Fact]
    public void PasswordHasher_VerifyPassword_ShouldMatchOriginalCryptBehavior()
    {
        // ARRANGE - Should test exact compatibility with original crypt verification
        var hasher = new PasswordHasher();
        var plainPassword = "testpass";
        var salt = "ab";
        var expectedHash = hasher.HashPassword(plainPassword, salt);
        
        // ACT - Should match original logic: crypt(input, stored_hash) == stored_hash
        var act1 = () => hasher.VerifyPassword(plainPassword, expectedHash);
        var act2 = () => hasher.VerifyPassword("wrongpass", expectedHash);
        
        // ASSERT
        act1.Should().NotThrow("should verify correct passwords");
        act2.Should().NotThrow("should handle incorrect passwords");
        
        hasher.VerifyPassword(plainPassword, expectedHash).Should().BeTrue();
        hasher.VerifyPassword("wrongpass", expectedHash).Should().BeFalse();
    }
    
    [Fact]
    public async Task CreatePlayerAccountAsync_NewPlayer_ShouldCreateWithHashedPassword()
    {
        // ARRANGE - Should fail as service doesn't exist
        var authService = new AuthenticationService();
        var username = "NewPlayer";
        var password = "newpass";
        
        // ACT
        var act = async () => await authService.CreatePlayerAccountAsync(username, password);
        
        // ASSERT
        act.Should().NotThrowAsync("should create new player accounts");
        
        var result = await authService.CreatePlayerAccountAsync(username, password);
        result.IsSuccess.Should().BeTrue("should successfully create new accounts");
        result.Player.Should().NotBeNull();
        result.Player.Name.Should().Be(username);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("ThisNameIsTooLong")]
    [InlineData("Invalid@Name")]
    [InlineData("ALLCAPS")]
    [InlineData("123numeric")]
    public async Task CreatePlayerAccountAsync_InvalidUsername_ShouldRejectCreation(string invalidUsername)
    {
        // ARRANGE - Should enforce original MUD naming rules from parser.c
        var authService = new AuthenticationService();
        var password = "validpass";
        
        // ACT
        var act = async () => await authService.CreatePlayerAccountAsync(invalidUsername, password);
        
        // ASSERT
        act.Should().NotThrowAsync("should handle invalid usernames gracefully");
        
        var result = await authService.CreatePlayerAccountAsync(invalidUsername, password);
        result.IsSuccess.Should().BeFalse("should reject invalid usernames");
        result.ErrorMessage.Should().ContainEquivalentOf("invalid");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("x")]
    [InlineData("verylongpasswordthatexceedslimits")]
    public async Task CreatePlayerAccountAsync_InvalidPassword_ShouldRejectCreation(string invalidPassword)
    {
        // ARRANGE - Should enforce original password rules (2-10 characters)
        var authService = new AuthenticationService();
        var username = "ValidName";
        
        // ACT
        var result = await authService.CreatePlayerAccountAsync(username, invalidPassword);
        
        // ASSERT
        result.IsSuccess.Should().BeFalse("should reject invalid passwords");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public async Task ChangePlayerPasswordAsync_ValidOldPassword_ShouldUpdatePassword()
    {
        // ARRANGE - Should fail as service doesn't exist
        var authService = new AuthenticationService();
        var username = "TestPlayer";
        var oldPassword = "oldpass";
        var newPassword = "newpass";
        
        // First create the account
        await authService.CreatePlayerAccountAsync(username, oldPassword);
        
        // ACT
        var act = async () => await authService.ChangePlayerPasswordAsync(username, oldPassword, newPassword);
        
        // ASSERT
        act.Should().NotThrowAsync("should change passwords");
        
        var result = await authService.ChangePlayerPasswordAsync(username, oldPassword, newPassword);
        result.IsSuccess.Should().BeTrue("should successfully change password with valid old password");
        
        // Verify old password no longer works
        var oldAuthResult = await authService.AuthenticatePlayerAsync(username, oldPassword);
        oldAuthResult.IsSuccess.Should().BeFalse("old password should no longer work");
        
        // Verify new password works
        var newAuthResult = await authService.AuthenticatePlayerAsync(username, newPassword);
        newAuthResult.IsSuccess.Should().BeTrue("new password should work");
    }
    
    [Fact]
    public async Task ChangePlayerPasswordAsync_InvalidOldPassword_ShouldRejectChange()
    {
        // ARRANGE
        var authService = new AuthenticationService();
        var username = "TestPlayer";
        var correctPassword = "correctpass";
        var wrongPassword = "wrongpass";
        var newPassword = "newpass";
        
        await authService.CreatePlayerAccountAsync(username, correctPassword);
        
        // ACT
        var result = await authService.ChangePlayerPasswordAsync(username, wrongPassword, newPassword);
        
        // ASSERT
        result.IsSuccess.Should().BeFalse("should reject password change with wrong old password");
        
        // Verify original password still works
        var authResult = await authService.AuthenticatePlayerAsync(username, correctPassword);
        authResult.IsSuccess.Should().BeTrue("original password should still work");
    }
    
    [Fact]
    public async Task ValidatePlayerNameAsync_ExistingPlayer_ShouldReturnTrue()
    {
        // ARRANGE - Should fail as service doesn't exist
        var authService = new AuthenticationService();
        var username = "ExistingPlayer";
        var password = "testpass";
        
        await authService.CreatePlayerAccountAsync(username, password);
        
        // ACT
        var act = async () => await authService.ValidatePlayerNameAsync(username);
        
        // ASSERT
        act.Should().NotThrowAsync("should validate player names");
        
        var exists = await authService.ValidatePlayerNameAsync(username);
        exists.Should().BeTrue("should find existing player");
    }
    
    [Fact]
    public async Task ValidatePlayerNameAsync_NonExistentPlayer_ShouldReturnFalse()
    {
        // ARRANGE
        var authService = new AuthenticationService();
        var nonExistentName = "DoesNotExist";
        
        // ACT
        var exists = await authService.ValidatePlayerNameAsync(nonExistentName);
        
        // ASSERT
        exists.Should().BeFalse("should not find non-existent player");
    }
    
    [Fact]
    public void AuthenticationResult_SuccessResult_ShouldContainPlayer()
    {
        // ARRANGE - Should fail as result types don't exist
        var player = new Player("test-id") { Name = "TestPlayer" };
        
        // ACT
        var act = () => AuthenticationResult.Success(player);
        
        // ASSERT
        act.Should().NotThrow("AuthenticationResult should be implemented");
        
        var result = AuthenticationResult.Success(player);
        result.IsSuccess.Should().BeTrue();
        result.Player.Should().Be(player);
        result.ErrorMessage.Should().BeNull();
    }
    
    [Fact]
    public void AuthenticationResult_FailureResult_ShouldContainErrorMessage()
    {
        // ARRANGE
        var errorMessage = "Invalid credentials";
        
        // ACT
        var result = AuthenticationResult.Failure(errorMessage);
        
        // ASSERT
        result.IsSuccess.Should().BeFalse();
        result.Player.Should().BeNull();
        result.ErrorMessage.Should().Be(errorMessage);
    }
}