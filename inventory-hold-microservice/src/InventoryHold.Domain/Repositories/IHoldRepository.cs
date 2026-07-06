using InventoryHold.Domain.Entities;

namespace InventoryHold.Domain.Repositories;

public interface IHoldRepository
{
    Task<Hold?> GetByIdAsync(string holdId, CancellationToken ct = default);
    Task<IReadOnlyList<Hold>> GetActiveExpiredHoldsAsync(CancellationToken ct = default);
    Task CreateAsync(Hold hold, CancellationToken ct = default);
    Task UpdateAsync(Hold hold, CancellationToken ct = default);
}
