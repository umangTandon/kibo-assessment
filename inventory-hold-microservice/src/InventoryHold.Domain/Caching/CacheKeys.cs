namespace InventoryHold.Domain.Caching;

public static class CacheKeys
{
    public static string ProductStock(string productId) => $"inventory:product:{productId}:stock";
    public static string InventorySummary => "inventory:summary";
}
