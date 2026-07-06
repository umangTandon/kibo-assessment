using System.Text.Json;
using InventoryHold.Domain.Ports;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace InventoryHold.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IConnectionMultiplexer _multiplexer;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer multiplexer, ILogger<RedisCacheService> logger)
    {
        _multiplexer = multiplexer;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var db = _multiplexer.GetDatabase();
            var value = await db.StringGetAsync(key);
            if (!value.HasValue)
                return null;

            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis cache get failed for key {CacheKey}", key);
            return null;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _multiplexer.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis cache remove failed for key {CacheKey}", key);
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var db = _multiplexer.GetDatabase();
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            await db.StringSetAsync(key, json, expiry);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis cache set failed for key {CacheKey}", key);
        }
    }
}
