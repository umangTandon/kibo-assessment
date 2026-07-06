namespace InventoryHold.Contracts.Responses;

public sealed record InventoryItemResponse(
    string ProductId,
    string ProductName,
    int AvailableStock,
    int ReservedStock,
    int TotalStock
);
