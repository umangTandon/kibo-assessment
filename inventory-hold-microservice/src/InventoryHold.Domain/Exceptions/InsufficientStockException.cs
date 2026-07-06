namespace InventoryHold.Domain.Exceptions;

public sealed class InsufficientStockException : Exception
{
    public string ProductId { get; }
    public int Requested { get; }
    public int Available { get; }

    public InsufficientStockException(string productId, int requested, int available)
        : base($"Insufficient stock for product '{productId}'. Requested {requested}, available {available}.")
    {
        ProductId = productId;
        Requested = requested;
        Available = available;
    }
}
