using InventoryHold.Contracts.Enums;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InventoryHold.UnitTests")]

namespace InventoryHold.Domain.Entities;

public sealed class Hold
{
    private Hold() { }

    public string Id { get; private set; } = default!;
    public string ProductId { get; private set; } = default!;
    public string? CustomerId { get; private set; }
    public int Quantity { get; private set; }
    public HoldStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public int Version { get; private set; }

    public static Hold Create(string productId, string? customerId, int quantity, int ttlSeconds)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("ProductId is required.", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        return new Hold
        {
            Id = Guid.NewGuid().ToString(),
            ProductId = productId,
            CustomerId = customerId,
            Quantity = quantity,
            Status = HoldStatus.Active,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(ttlSeconds),
            ReleasedAt = null,
            Version = 1
        };
    }

    public bool IsExpired() => Status == HoldStatus.Active && DateTime.UtcNow > ExpiresAt;

    public void Release()
    {
        if (Status != HoldStatus.Active)
            throw new InvalidOperationException("Only active holds can be released.");

        Status = HoldStatus.Released;
        ReleasedAt = DateTime.UtcNow;
        Version++;
    }

    public void MarkExpired()
    {
        if (Status != HoldStatus.Active)
            throw new InvalidOperationException("Only active holds can be marked expired.");

        Status = HoldStatus.Expired;
        ReleasedAt = DateTime.UtcNow;
        Version++;
    }
}
