namespace InventoryHold.Contracts.Requests;

public sealed record CreateHoldRequest(
    string ProductId,
    int Quantity,
    string? CustomerId,
    int? TtlSeconds
);
