namespace C3Mud.Core.Players.Models;

/// <summary>
/// Result of player authentication attempt
/// </summary>
public class AuthenticationResult
{
    public bool IsSuccess { get; init; }
    public IPlayer? Player { get; init; }
    public string? ErrorMessage { get; init; }
    
    private AuthenticationResult(bool isSuccess, IPlayer? player = null, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        Player = player;
        ErrorMessage = errorMessage;
    }
    
    public static AuthenticationResult Success(IPlayer player)
    {
        return new AuthenticationResult(true, player);
    }
    
    public static AuthenticationResult Failure(string errorMessage)
    {
        return new AuthenticationResult(false, errorMessage: errorMessage);
    }
}