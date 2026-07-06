using InventoryHold.Contracts.Responses;
using InventoryHold.Domain.Entities;

namespace InventoryHold.WebApi.Mappers;

public static class InventoryMapper
{
    public static InventoryItemResponse ToResponse(InventoryItem item)
        => new(
            ProductId: item.ProductId,
            ProductName: item.ProductName,
            AvailableStock: item.AvailableStock,
            ReservedStock: item.ReservedStock,
            TotalStock: item.TotalStock);
}
