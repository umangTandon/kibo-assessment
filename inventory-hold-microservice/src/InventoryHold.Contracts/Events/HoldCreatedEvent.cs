namespace InventoryHold.Contracts.Events;

public sealed record HoldCreatedEvent(
    string EventId,
    string HoldId,
    string ProductId,
    string? CustomerId,
    int Quantity,
    int RemainingStock,
    DateTime OccurredAt,
    DateTime ExpiresAt
);
