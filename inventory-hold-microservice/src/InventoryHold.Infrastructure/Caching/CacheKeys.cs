namespace InventoryHold.Infrastructure.Caching;

public static class CacheKeys
{
    public static string InventoryAll() => "inventory:all";
    public static string InventoryItem(string productId) => $"inventory:item:{productId}";
    public static string Hold(string holdId) => $"hold:{holdId}";
    public static readonly TimeSpan InventoryTtl = TimeSpan.FromMinutes(5);
}
