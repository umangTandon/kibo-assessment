using InventoryHold.Domain.Entities;
using InventoryHold.Infrastructure.Persistence.Documents;

namespace InventoryHold.Infrastructure.Persistence.Mappers;

public static class InventoryMapper
{
    public static InventoryItem ToDomain(InventoryDocument document)
        => new()
        {
            ProductId = document.ProductId,
            ProductName = document.ProductName,
            TotalStock = document.TotalStock,
            ReservedStock = document.ReservedStock,
            Version = document.Version
        };

    public static InventoryDocument ToDocument(InventoryItem item)
        => new()
        {
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            TotalStock = item.TotalStock,
            ReservedStock = item.ReservedStock,
            AvailableStock = item.AvailableStock,
            Version = item.Version
        };
}
