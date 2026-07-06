namespace InventoryHold.Infrastructure.Caching.Options;

public sealed class RedisOptions
{
    public string ConnectionString { get; set; } = "localhost:6379,abortConnect=false";
}
