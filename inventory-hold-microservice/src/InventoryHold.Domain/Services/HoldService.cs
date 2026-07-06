using InventoryHold.Contracts.Events;
using InventoryHold.Domain.Caching;
using InventoryHold.Domain.Entities;
using InventoryHold.Domain.Exceptions;
using InventoryHold.Domain.Options;
using InventoryHold.Domain.Ports;
using InventoryHold.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventoryHold.Domain.Services;

public sealed class HoldService
{
    private readonly IHoldRepository _holdRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICacheService _cacheService;
    private readonly IMessagePublisher _publisher;
    private readonly HoldOptions _options;
    private readonly ILogger<HoldService> _logger;

    public HoldService(
        IHoldRepository holdRepository,
        IInventoryRepository inventoryRepository,
        ICacheService cacheService,
        IMessagePublisher publisher,
        ILogger<HoldService> logger,
        IOptions<HoldOptions> options)
    {
        _holdRepository = holdRepository;
        _inventoryRepository = inventoryRepository;
        _cacheService = cacheService;
        _publisher = publisher;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<Hold> CreateHoldAsync(
        string productId,
        string? customerId,
        int quantity,
        int? ttlSeconds,
        CancellationToken ct = default)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        var ttl = ttlSeconds ?? _options.DefaultTtlSeconds;
        var inventory = await _inventoryRepository.TryDeductStockAsync(productId, quantity, ct);
        if (inventory is null)
            throw new InsufficientStockException(productId, quantity, 0);

        var hold = Hold.Create(productId, customerId, quantity, ttl);
        await _holdRepository.CreateAsync(hold, ct);

        await _cacheService.RemoveAsync(CacheKeys.InventoryAll(), ct);
        await _cacheService.RemoveAsync(CacheKeys.InventoryItem(productId), ct);

        var @event = new HoldCreatedEvent(
            EventId: Guid.NewGuid().ToString(),
            HoldId: hold.Id,
            ProductId: hold.ProductId,
            CustomerId: hold.CustomerId,
            Quantity: hold.Quantity,
            RemainingStock: inventory.AvailableStock,
            OccurredAt: DateTime.UtcNow,
            ExpiresAt: hold.ExpiresAt);

        await _publisher.PublishAsync("hold.created", @event, ct);

        return hold;
    }

    public async Task<Hold> GetHoldAsync(string holdId, CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.Hold(holdId);
        var cachedHold = await _cacheService.GetAsync<Hold>(cacheKey, ct);
        if (cachedHold is not null)
            return cachedHold;

        var hold = await _holdRepository.GetByIdAsync(holdId, ct);
        if (hold is null)
            throw new HoldNotFoundException(holdId);

        if (hold.IsExpired())
        {
            hold.MarkExpired();
            await _holdRepository.UpdateAsync(hold, ct);
            var inventory = await _inventoryRepository.RestoreStockAsync(hold.ProductId, hold.Quantity, ct);
            await _cacheService.RemoveAsync(cacheKey, ct);
            await _cacheService.RemoveAsync(CacheKeys.InventoryAll(), ct);
            await _cacheService.RemoveAsync(CacheKeys.InventoryItem(hold.ProductId), ct);

            var @event = new HoldExpiredEvent(
                EventId: Guid.NewGuid().ToString(),
                HoldId: hold.Id,
                ProductId: hold.ProductId,
                QuantityRestored: hold.Quantity,
                OriginalExpiresAt: hold.ExpiresAt,
                OccurredAt: DateTime.UtcNow);

            await _publisher.PublishAsync("hold.expired", @event, ct);
            return hold;
        }

        var ttl = hold.ExpiresAt - DateTime.UtcNow;
        if (ttl > TimeSpan.Zero)
            await _cacheService.SetAsync(cacheKey, hold, ttl, ct);

        return hold;
    }

    public async Task<Hold> ReleaseHoldAsync(string holdId, CancellationToken ct = default)
    {
        var hold = await GetHoldAsync(holdId, ct);

        if (hold.Status == Contracts.Enums.HoldStatus.Released)
            throw new HoldAlreadyReleasedException(holdId);

        if (hold.Status == Contracts.Enums.HoldStatus.Expired)
            throw new HoldExpiredException(holdId);

        hold.Release();
        await _holdRepository.UpdateAsync(hold, ct);
        var restoredInventory = await _inventoryRepository.RestoreStockAsync(hold.ProductId, hold.Quantity, ct);
        await _cacheService.RemoveAsync(CacheKeys.Hold(hold.Id), ct);
        await _cacheService.RemoveAsync(CacheKeys.InventoryAll(), ct);
        await _cacheService.RemoveAsync(CacheKeys.InventoryItem(hold.ProductId), ct);

        var @event = new HoldReleasedEvent(
            EventId: Guid.NewGuid().ToString(),
            HoldId: hold.Id,
            ProductId: hold.ProductId,
            CustomerId: hold.CustomerId,
            QuantityRestored: hold.Quantity,
            RemainingStock: restoredInventory.AvailableStock,
            OccurredAt: DateTime.UtcNow);

        await _publisher.PublishAsync("hold.released", @event, ct);
        return hold;
    }

    public async Task<IReadOnlyList<InventoryItem>> GetInventoryAsync(CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.InventoryAll();
        var cached = await _cacheService.GetAsync<List<InventoryItem>>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var inventory = await _inventoryRepository.GetAllAsync(ct);
        await _cacheService.SetAsync(cacheKey, inventory.ToList(), TimeSpan.FromMinutes(5), ct);
        return inventory;
    }
}
