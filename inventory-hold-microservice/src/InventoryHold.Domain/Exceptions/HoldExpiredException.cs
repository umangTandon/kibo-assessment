namespace InventoryHold.Domain.Exceptions;

public sealed class HoldExpiredException : Exception
{
    public string HoldId { get; }

    public HoldExpiredException(string holdId)
        : base($"Hold '{holdId}' has already expired.")
    {
        HoldId = holdId;
    }
}
