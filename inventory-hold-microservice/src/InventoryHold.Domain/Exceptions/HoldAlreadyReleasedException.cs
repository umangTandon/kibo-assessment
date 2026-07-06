namespace InventoryHold.Domain.Exceptions;

public sealed class HoldAlreadyReleasedException : Exception
{
    public string HoldId { get; }

    public HoldAlreadyReleasedException(string holdId)
        : base($"Hold '{holdId}' has already been released.")
    {
        HoldId = holdId;
    }
}
