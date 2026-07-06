using InventoryHold.Contracts.Responses;
using InventoryHold.Domain.Entities;

namespace InventoryHold.WebApi.Mappers;

public static class HoldMapper
{
    public static HoldResponse ToResponse(Hold hold)
    {
        var ttl = hold.ExpiresAt - DateTime.UtcNow;
        return new HoldResponse(
            HoldId: hold.Id,
            ProductId: hold.ProductId,
            CustomerId: hold.CustomerId,
            Quantity: hold.Quantity,
            Status: (InventoryHold.Contracts.Enums.HoldStatus)hold.Status,
            CreatedAt: hold.CreatedAt,
            ExpiresAt: hold.ExpiresAt,
            ReleasedAt: hold.ReleasedAt,
            MinutesRemaining: ttl > TimeSpan.Zero ? (int)Math.Ceiling(ttl.TotalMinutes) : 0);
    }
}
