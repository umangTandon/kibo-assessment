namespace InventoryHold.Contracts.Events;

public sealed record HoldExpiredEvent(
    string EventId,
    string HoldId,
    string ProductId,
    int QuantityRestored,
    DateTime OriginalExpiresAt,
    DateTime OccurredAt
);
