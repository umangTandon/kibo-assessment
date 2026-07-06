namespace InventoryHold.Domain.Exceptions;

public sealed class HoldNotFoundException : Exception
{
    public string HoldId { get; }

    public HoldNotFoundException(string holdId)
        : base($"Hold '{holdId}' was not found.")
    {
        HoldId = holdId;
    }
}
