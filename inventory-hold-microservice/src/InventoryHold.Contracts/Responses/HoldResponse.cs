using InventoryHold.Contracts.Enums;

namespace InventoryHold.Contracts.Responses;

public sealed record HoldResponse(
    string HoldId,
    string ProductId,
    string? CustomerId,
    int Quantity,
    HoldStatus Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? ReleasedAt,
    int MinutesRemaining
);
