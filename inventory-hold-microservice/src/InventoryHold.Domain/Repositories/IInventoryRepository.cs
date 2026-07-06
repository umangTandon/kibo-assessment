using InventoryHold.Domain.Entities;

namespace InventoryHold.Domain.Repositories;

public interface IInventoryRepository
{
    Task<IReadOnlyList<InventoryItem>> GetAllAsync(CancellationToken ct = default);
    Task<InventoryItem?> TryDeductStockAsync(string productId, int quantity, CancellationToken ct = default);
    Task<InventoryItem> RestoreStockAsync(string productId, int quantity, CancellationToken ct = default);
    Task SeedAsync(IEnumerable<InventoryItem> items, CancellationToken ct = default);
}
