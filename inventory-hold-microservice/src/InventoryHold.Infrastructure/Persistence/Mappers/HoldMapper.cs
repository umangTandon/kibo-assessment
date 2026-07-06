using InventoryHold.Domain.Entities;
using InventoryHold.Infrastructure.Persistence.Documents;

using System.Reflection;
using InventoryHold.Domain.Entities;
using InventoryHold.Infrastructure.Persistence.Documents;

namespace InventoryHold.Infrastructure.Persistence.Mappers;

public static class HoldMapper
{
    public static Hold ToDomain(HoldDocument document)
    {
        var hold = (Hold)Activator.CreateInstance(typeof(Hold), nonPublic: true)!;
        var type = typeof(Hold);
        type.GetProperty(nameof(Hold.Id))!.SetValue(hold, document.Id);
        type.GetProperty(nameof(Hold.ProductId))!.SetValue(hold, document.ProductId);
        type.GetProperty(nameof(Hold.CustomerId))!.SetValue(hold, document.CustomerId);
        type.GetProperty(nameof(Hold.Quantity))!.SetValue(hold, document.Quantity);
        type.GetProperty(nameof(Hold.Status))!.SetValue(hold, document.Status);
        type.GetProperty(nameof(Hold.CreatedAt))!.SetValue(hold, document.CreatedAt);
        type.GetProperty(nameof(Hold.ExpiresAt))!.SetValue(hold, document.ExpiresAt);
        type.GetProperty(nameof(Hold.ReleasedAt))!.SetValue(hold, document.ReleasedAt);
        type.GetProperty(nameof(Hold.Version))!.SetValue(hold, document.Version);
        return hold;
    }

    public static HoldDocument ToDocument(Hold hold)
        => new()
        {
            Id = hold.Id,
            ProductId = hold.ProductId,
            CustomerId = hold.CustomerId,
            Quantity = hold.Quantity,
            Status = hold.Status,
            CreatedAt = hold.CreatedAt,
            ExpiresAt = hold.ExpiresAt,
            ReleasedAt = hold.ReleasedAt,
            Version = hold.Version
        };
}
