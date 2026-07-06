using InventoryHold.Domain.Caching;
using InventoryHold.Domain.Ports;
using InventoryHold.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InventoryHold.Infrastructure.BackgroundServices;

public sealed class ExpiredHoldCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredHoldCleanupService> _logger;

    public ExpiredHoldCleanupService(IServiceScopeFactory scopeFactory, ILogger<ExpiredHoldCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessExpiredHoldsAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }

    private async Task ProcessExpiredHoldsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var holdRepository = scope.ServiceProvider.GetRequiredService<IHoldRepository>();
        var inventoryRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        var expiredHolds = await holdRepository.GetActiveExpiredHoldsAsync(ct);
        if (!expiredHolds.Any())
            return;

        foreach (var hold in expiredHolds)
        {
            try
            {
                hold.MarkExpired();
                await holdRepository.UpdateAsync(hold, ct);
                await inventoryRepository.RestoreStockAsync(hold.ProductId, hold.Quantity, ct);
                await cacheService.RemoveAsync(CacheKeys.Hold(hold.Id), ct);
                await cacheService.RemoveAsync(CacheKeys.InventoryAll(), ct);
                await cacheService.RemoveAsync(CacheKeys.InventoryItem(hold.ProductId), ct);

                var @event = new InventoryHold.Contracts.Events.HoldExpiredEvent(
                    EventId: Guid.NewGuid().ToString(),
                    HoldId: hold.Id,
                    ProductId: hold.ProductId,
                    QuantityRestored: hold.Quantity,
                    OriginalExpiresAt: hold.ExpiresAt,
                    OccurredAt: DateTime.UtcNow);

                await publisher.PublishAsync("hold.expired", @event, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process expired hold {HoldId}", hold.Id);
            }
        }

        _logger.LogInformation("Expired hold cleanup processed {Count} holds.", expiredHolds.Count);
    }
}
