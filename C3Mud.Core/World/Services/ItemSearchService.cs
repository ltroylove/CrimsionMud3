using C3Mud.Core.Players;
using C3Mud.Core.World.Models;

namespace C3Mud.Core.World.Services;

/// <summary>
/// Service for searching items with CircleMUD-compatible functionality
/// Based on original ha2075_get_obj_list_vis and ha1975_get_number functions
/// </summary>
public static class ItemSearchService
{
    /// <summary>
    /// Search for an item in a list with visibility checks and dot notation support
    /// Supports "2.sword", "all.ring", etc. like original CircleMUD
    /// </summary>
    /// <param name="player">Player performing the search</param>
    /// <param name="searchString">Search string (supports dot notation)</param>
    /// <param name="items">List of items to search</param>
    /// <returns>Found item or null if not found</returns>
    public static WorldObject? FindItem(IPlayer player, string searchString, IEnumerable<WorldObject> items)
    {
        if (string.IsNullOrWhiteSpace(searchString) || !items.Any())
            return null;

        // Parse the search string for dot notation (e.g., "2.sword")
        var (targetNumber, itemName) = ParseSearchString(searchString.Trim());
        if (targetNumber <= 0)
            return null;

        int matchCount = 0;
        foreach (var item in items)
        {
            // Check if item name matches and is visible
            if (IsNameMatch(itemName, item.Name) && CanPlayerSeeObject(player, item))
            {
                matchCount++;
                if (matchCount == targetNumber)
                {
                    return item;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Find all items matching the search criteria
    /// Useful for "all.ring" type searches
    /// </summary>
    /// <param name="player">Player performing the search</param>
    /// <param name="searchString">Search string</param>
    /// <param name="items">List of items to search</param>
    /// <returns>List of matching items</returns>
    public static List<WorldObject> FindAllItems(IPlayer player, string searchString, IEnumerable<WorldObject> items)
    {
        if (string.IsNullOrWhiteSpace(searchString) || !items.Any())
            return new List<WorldObject>();

        var (targetNumber, itemName) = ParseSearchString(searchString.Trim());
        var results = new List<WorldObject>();

        // Handle "all.item" searches
        if (searchString.StartsWith("all.", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var item in items)
            {
                if (IsNameMatch(itemName, item.Name) && CanPlayerSeeObject(player, item))
                {
                    results.Add(item);
                }
            }
        }
        else
        {
            // Single item search
            var foundItem = FindItem(player, searchString, items);
            if (foundItem != null)
            {
                results.Add(foundItem);
            }
        }

        return results;
    }

    /// <summary>
    /// Parse search string to extract target number and item name
    /// Examples: "2.sword" -> (2, "sword"), "sword" -> (1, "sword"), "all.ring" -> (-1, "ring")
    /// Based on original ha1975_get_number function
    /// </summary>
    /// <param name="searchString">Raw search string</param>
    /// <returns>Tuple of (targetNumber, itemName)</returns>
    private static (int targetNumber, string itemName) ParseSearchString(string searchString)
    {
        var dotIndex = searchString.IndexOf('.');
        if (dotIndex == -1)
        {
            // No dot found, search for first match
            return (1, searchString);
        }

        var numberPart = searchString.Substring(0, dotIndex);
        var namePart = searchString.Substring(dotIndex + 1);

        // Handle "all.item" case
        if (numberPart.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return (-1, namePart);
        }

        // Try to parse the number
        if (int.TryParse(numberPart, out int number) && number > 0)
        {
            return (number, namePart);
        }

        // Invalid number format, treat as regular search
        return (1, searchString);
    }

    /// <summary>
    /// Check if the search name matches the item's keywords
    /// Based on original ha1150_isname function logic
    /// </summary>
    /// <param name="searchName">Name being searched for</param>
    /// <param name="itemKeywords">Item's keyword string</param>
    /// <returns>True if name matches</returns>
    private static bool IsNameMatch(string searchName, string itemKeywords)
    {
        if (string.IsNullOrWhiteSpace(searchName) || string.IsNullOrWhiteSpace(itemKeywords))
            return false;

        // Split keywords and check for partial matches (CircleMUD style)
        var keywords = itemKeywords.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var keyword in keywords)
        {
            if (keyword.StartsWith(searchName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if player can see the object
    /// Based on original CAN_SEE_OBJ macro
    /// </summary>
    /// <param name="player">Player attempting to see the object</param>
    /// <param name="item">Object to check visibility for</param>
    /// <returns>True if player can see the object</returns>
    private static bool CanPlayerSeeObject(IPlayer player, WorldObject item)
    {
        if (item == null)
            return false;

        // TODO: Implement full visibility logic including:
        // - Light requirements for dark rooms
        // - Invisible objects and detect invisible
        // - Player blindness/darkness effects
        // For now, return true (all objects visible)
        
        return true;
    }
}