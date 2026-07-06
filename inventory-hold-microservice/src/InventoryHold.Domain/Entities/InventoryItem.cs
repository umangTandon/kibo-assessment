namespace InventoryHold.Domain.Entities;

public sealed class InventoryItem
{
    public string ProductId { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public int TotalStock { get; set; }
    public int ReservedStock { get; set; }
    public int Version { get; set; }

    public int AvailableStock => TotalStock - ReservedStock;
}
