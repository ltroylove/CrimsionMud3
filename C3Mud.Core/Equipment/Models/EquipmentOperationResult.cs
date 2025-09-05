namespace C3Mud.Core.Equipment.Models;

/// <summary>
/// Result of equipment operations (equip, unequip, inventory management)
/// Provides success status and descriptive messages for player feedback
/// </summary>
public class EquipmentOperationResult
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Message to display to the player about the operation result
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional additional data from the operation
    /// </summary>
    public object? Data { get; set; }
    
    /// <summary>
    /// Create a successful result
    /// </summary>
    /// <param name="message">Success message</param>
    /// <param name="data">Optional result data</param>
    /// <returns>Successful operation result</returns>
    public static EquipmentOperationResult CreateSuccess(string message, object? data = null)
    {
        return new EquipmentOperationResult
        {
            Success = true,
            Message = message,
            Data = data
        };
    }
    
    /// <summary>
    /// Create a failed result
    /// </summary>
    /// <param name="message">Failure message</param>
    /// <param name="data">Optional error data</param>
    /// <returns>Failed operation result</returns>
    public static EquipmentOperationResult CreateFailure(string message, object? data = null)
    {
        return new EquipmentOperationResult
        {
            Success = false,
            Message = message,
            Data = data
        };
    }
}