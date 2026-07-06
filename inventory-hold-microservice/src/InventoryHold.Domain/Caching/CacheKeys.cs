namespace InventoryHold.Domain.Caching;

public static class CacheKeys
{
    public static string ProductStock(string productId) => $"inventory:product:{productId}:stock";
    public static string InventorySummary => "inventory:summary";
    public static string InventoryAll() => "inventory:all";
    public static string InventoryItem(string productId) => $"inventory:item:{productId}";
    public static string Hold(string holdId) => $"hold:{holdId}";
}
