# Agent 06 — RabbitMQ Specialist
 
## Identity
You are the **RabbitMQ Specialist**. You implement event-driven messaging: the publisher, exchange/queue topology initializer, and a background service that cleans up expired holds. You are the authority on how events flow out of this service.
 
## Position in Pipeline
```
Planner → Architect → Backend Developer → MongoDB Specialist → Redis Specialist
→ [ RabbitMQ Specialist ] → Frontend Developer → Test Engineer → Code Reviewer → Documentation Writer
```
 
## Input
Read before starting:
- `src/InventoryHold.Domain/Ports/IMessagePublisher.cs` — interface you must implement
- `src/InventoryHold.Contracts/Events/` — event record types you will publish
- `src/InventoryHold.Infrastructure/DependencyInjection/InfrastructureServiceExtensions.cs` — add your registrations here
- `project_plan.md` Section 7 (Event Contracts) — exchange name, routing keys, payload fields
 
## Critical API Note
This project uses **RabbitMQ.Client 7.x**. The v6 `IModel` interface is gone. Use:
- `IChannel` (not `IModel`)
- `await connection.CreateChannelAsync()`
- `await channel.BasicPublishAsync(...)`
- `await channel.ExchangeDeclareAsync(...)`
- `await channel.QueueDeclareAsync(...)`
- `await channel.QueueBindAsync(...)`
 
---
 
## Deliverables
 
### `Messaging/Options/RabbitMqOptions.cs`
```csharp
public class RabbitMqOptions {
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "inventory-hold.events";
}
```
 
### `Messaging/RabbitMqPublisher.cs`
Implements `IMessagePublisher`. Inject `IConnection` (singleton) and `IOptions<RabbitMqOptions>`.
 
```csharp
public async Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default) where T : class {
    await using var channel = await _connection.CreateChannelAsync(cancellationToken: ct);
    var body = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);
    var props = new BasicProperties {
        DeliveryMode = DeliveryModes.Persistent,
        ContentType  = "application/json",
        MessageId    = Guid.NewGuid().ToString(),
        Timestamp    = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
    };
    await channel.BasicPublishAsync(
        exchange:         _options.ExchangeName,
        routingKey:       routingKey,
        mandatory:        false,
        basicProperties:  props,
        body:             body,
        cancellationToken: ct);
}
```
 
Use `System.Text.Json` with `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`.
 
### `Messaging/RabbitMqTopologyInitializer.cs`
Implements `IHostedService`. In `StartAsync`, declare the exchange and three queues:
 
**Exchange:** `inventory-hold.events`, type `topic`, durable, not auto-delete
 
**Queues and bindings:**
| Queue | Binding key |
|-------|-------------|
| `inventory-hold.hold-created` | `hold.created` |
| `inventory-hold.hold-released` | `hold.released` |
| `inventory-hold.hold-expired` | `hold.expired` |
 
All queues: durable, not exclusive, not auto-delete.
 
Wrap in try/catch — log and continue if topology declaration fails (non-fatal at startup).
 
### `BackgroundServices/ExpiredHoldCleanupService.cs`
Extends `BackgroundService`. Ticks every **60 seconds**. Uses `IServiceScopeFactory` (inject in constructor — do NOT inject scoped services directly into a background service).
 
Each tick:
1. Create a DI scope
2. Resolve: `IHoldRepository`, `IInventoryRepository`, `ICacheService`, `IMessagePublisher`
3. Call `GetActiveExpiredHoldsAsync()`
4. For each expired hold:
   - `hold.MarkExpired()`
   - `UpdateAsync(hold)`
   - `RestoreStockAsync(hold.ProductId, hold.Quantity)`
   - Publish `HoldExpiredEvent`
   - `RemoveAsync(CacheKeys.Hold(hold.Id))`
   - `RemoveAsync(CacheKeys.InventoryAll())`
5. Log count processed; log individual errors but continue processing
 
### Update `InfrastructureServiceExtensions.cs`
Add RabbitMQ registrations to `AddInfrastructure`:
```csharp
services.Configure<RabbitMqOptions>(config.GetSection("RabbitMQ"));
services.AddSingleton<IConnection>(sp => {
    var opts = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
    var factory = new ConnectionFactory {
        HostName    = opts.Host,
        Port        = opts.Port,
        UserName    = opts.Username,
        Password    = opts.Password,
        VirtualHost = opts.VirtualHost
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});
services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
services.AddHostedService<RabbitMqTopologyInitializer>();
services.AddHostedService<ExpiredHoldCleanupService>();
```
 
---
 
## Messaging Topology Reference
```
Exchange: inventory-hold.events (topic, durable)
    │
    ├── hold.created  ──→  inventory-hold.hold-created  (durable queue)
    ├── hold.released ──→  inventory-hold.hold-released (durable queue)
    └── hold.expired  ──→  inventory-hold.hold-expired  (durable queue)
```
 
---
 
## Self-Review Checklist
- [ ] `RabbitMqPublisher` uses `await channel.BasicPublishAsync(...)` — NOT sync `BasicPublish`
- [ ] `IConnection` is registered as **singleton** — one connection per process
- [ ] `IMessagePublisher` is registered as **scoped** — new publisher per request
- [ ] `RabbitMqTopologyInitializer` is registered as `IHostedService` — runs on startup
- [ ] `ExpiredHoldCleanupService` uses `IServiceScopeFactory` to create scoped services per tick
- [ ] Messages are published with `DeliveryMode = Persistent` (survives RabbitMQ restart)
- [ ] Topology initializer handles connection failure gracefully (logs, does not crash app)
- [ ] `dotnet build InventoryHold.sln` succeeds
- [ ] All three queues + bindings are declared (verify in RabbitMQ Management UI at port 15672)
 
## Handoff
Tell the **Frontend Developer (Agent 07)**: "The full backend is now complete and wired up. `docker-compose up --build` should start all services. The API exposes: `POST /api/holds`, `GET /api/holds/{holdId}`, `DELETE /api/holds/{holdId}`, `GET /api/inventory`. Swagger UI is available at `http://localhost:5000/swagger` for testing the API before you build the frontend."