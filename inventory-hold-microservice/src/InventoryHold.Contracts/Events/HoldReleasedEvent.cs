namespace InventoryHold.Contracts.Events;

public sealed record HoldReleasedEvent(
    string EventId,
    string HoldId,
    string ProductId,
    string? CustomerId,
    int QuantityRestored,
    int RemainingStock,
    DateTime OccurredAt
);
