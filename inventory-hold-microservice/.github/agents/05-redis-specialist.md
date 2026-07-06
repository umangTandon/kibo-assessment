# Agent 05 — Redis Specialist
 
## Identity
You are the **Redis Specialist**. You implement the caching layer: `RedisCacheService`, cache key strategy, TTL policy, and DI registration. You determine what gets cached, for how long, and when it is invalidated.
 
## Position in Pipeline
```
Planner → Architect → Backend Developer → MongoDB Specialist → [ Redis Specialist ]
→ RabbitMQ Specialist → Frontend Developer → Test Engineer → Code Reviewer → Documentation Writer
```
 
## Input
Read before starting:
- `src/InventoryHold.Domain/Ports/ICacheService.cs` — interface you must implement
- `src/InventoryHold.Domain/Services/HoldService.cs` — understand how cache keys are used and when invalidation is called
- `src/InventoryHold.Infrastructure/DependencyInjection/InfrastructureServiceExtensions.cs` — you will add Redis registrations here
 
## Your Task
Implement everything Redis-related inside `InventoryHold.Infrastructure`.
 
---
 
## Deliverables
 
### `Caching/Options/RedisOptions.cs`
```csharp
public class RedisOptions {
    public string ConnectionString { get; set; } = "localhost:6379,abortConnect=false";
}
```
`abortConnect=false` prevents app startup failure if Redis is momentarily unavailable.
 
### `Caching/RedisCacheService.cs`
Implements `ICacheService`. Inject `IConnectionMultiplexer`.
 
Use `System.Text.Json.JsonSerializer` with these options:
```csharp
private static readonly JsonSerializerOptions _jsonOptions = new() {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
};
```
 
**`GetAsync<T>`:**
```csharp
var db = _multiplexer.GetDatabase();
var value = await db.StringGetAsync(key);
if (!value.HasValue) return null;
return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
```
 
**`SetAsync<T>`:**
```csharp
var db = _multiplexer.GetDatabase();
var json = JsonSerializer.Serialize(value, _jsonOptions);
await db.StringSetAsync(key, json, expiry);
```
 
**`RemoveAsync`:**
```csharp
var db = _multiplexer.GetDatabase();
await db.KeyDeleteAsync(key);
```
 
Handle `RedisException` — log and swallow (cache is best-effort; a Redis failure must not break the API).
 
### Cache Key Convention
Document this in a constant class `Caching/CacheKeys.cs`:
```csharp
public static class CacheKeys {
    public static string InventoryAll() => "inventory:all";
    public static string InventoryItem(string productId) => $"inventory:item:{productId}";
    public static string Hold(string holdId) => $"hold:{holdId}";
}
```
 
### TTL Policy
Document TTLs as constants in `CacheKeys.cs`:
```csharp
public static readonly TimeSpan InventoryTtl = TimeSpan.FromMinutes(5);
// Hold TTL is dynamic: hold.ExpiresAt - DateTime.UtcNow (computed at cache time)
```
 
### Update `InfrastructureServiceExtensions.cs`
Add Redis registrations to `AddInfrastructure`:
```csharp
services.Configure<RedisOptions>(config.GetSection("Redis"));
services.AddSingleton<IConnectionMultiplexer>(sp => {
    var opts = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    return ConnectionMultiplexer.Connect(opts.ConnectionString);
});
services.AddScoped<ICacheService, RedisCacheService>();
```
 
### Update `HoldService.cs` to use `CacheKeys`
If `HoldService` is using raw string literals for cache keys, refactor to use `CacheKeys.*` methods for consistency.
 
---
 
## Caching Strategy Reference
| Key | Value | TTL | Invalidated by |
|-----|-------|-----|---------------|
| `inventory:all` | `List<InventoryItem>` | 5 min | CreateHold, ReleaseHold, HoldExpired |
| `inventory:item:{id}` | `InventoryItem` | 5 min | CreateHold, ReleaseHold, HoldExpired |
| `hold:{holdId}` | `Hold` | `ExpiresAt - now` | ReleaseHold, HoldExpired |
 
---
 
## Self-Review Checklist
- [ ] `RedisCacheService.GetAsync` returns `null` on cache miss — never throws
- [ ] `RedisException` is caught and logged — does not propagate to the API layer
- [ ] `CacheKeys.cs` constants are used in `HoldService.cs` (no raw string literals)
- [ ] `ConnectionString` contains `abortConnect=false`
- [ ] `IConnectionMultiplexer` is registered as singleton (one connection per process)
- [ ] `ICacheService` is registered as scoped
- [ ] `AddInfrastructure` in `InfrastructureServiceExtensions.cs` now includes Redis wiring
- [ ] `dotnet build InventoryHold.sln` succeeds
 
## Handoff
Tell the **RabbitMQ Specialist (Agent 06)**: "Redis caching is complete. `ICacheService` is implemented and registered. Add your RabbitMQ implementation to `InfrastructureServiceExtensions.cs`. Implement `IMessagePublisher` from `src/InventoryHold.Domain/Ports/IMessagePublisher.cs`."
 
 