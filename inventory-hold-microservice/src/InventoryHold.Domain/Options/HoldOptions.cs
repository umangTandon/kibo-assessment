namespace InventoryHold.Domain.Options;

public sealed class HoldOptions
{
    public int DefaultTtlSeconds { get; set; } = 900;
}
