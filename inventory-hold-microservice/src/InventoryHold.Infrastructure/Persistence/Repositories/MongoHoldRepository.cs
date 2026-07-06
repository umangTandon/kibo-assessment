using InventoryHold.Domain.Entities;
using InventoryHold.Domain.Repositories;
using InventoryHold.Infrastructure.Persistence.Documents;
using InventoryHold.Infrastructure.Persistence.Mappers;
using MongoDB.Driver;

namespace InventoryHold.Infrastructure.Persistence.Repositories;

public sealed class MongoHoldRepository : IHoldRepository
{
    private readonly IMongoCollection<HoldDocument> _collection;

    public MongoHoldRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<HoldDocument>("holds");
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        var statusExpiry = Builders<HoldDocument>.IndexKeys
            .Ascending(x => x.Status)
            .Ascending(x => x.ExpiresAt);
        _collection.Indexes.CreateOne(new CreateIndexModel<HoldDocument>(statusExpiry, new CreateIndexOptions { Name = "idx_status_expiry" }));

        var productId = Builders<HoldDocument>.IndexKeys.Ascending(x => x.ProductId);
        _collection.Indexes.CreateOne(new CreateIndexModel<HoldDocument>(productId, new CreateIndexOptions { Name = "idx_productId" }));
    }

    public async Task<Hold?> GetByIdAsync(string holdId, CancellationToken ct = default)
    {
        var filter = Builders<HoldDocument>.Filter.Eq(x => x.Id, holdId);
        var document = await _collection.Find(filter).FirstOrDefaultAsync(ct);
        return document is null ? null : HoldMapper.ToDomain(document);
    }

    public async Task<IReadOnlyList<Hold>> GetActiveExpiredHoldsAsync(CancellationToken ct = default)
    {
        var filter = Builders<HoldDocument>.Filter.And(
            Builders<HoldDocument>.Filter.Eq(x => x.Status, InventoryHold.Contracts.Enums.HoldStatus.Active),
            Builders<HoldDocument>.Filter.Lte(x => x.ExpiresAt, DateTime.UtcNow));

        var documents = await _collection.Find(filter).ToListAsync(ct);
        return documents.Select(HoldMapper.ToDomain).ToList();
    }

    public async Task CreateAsync(Hold hold, CancellationToken ct = default)
    {
        await _collection.InsertOneAsync(HoldMapper.ToDocument(hold), cancellationToken: ct);
    }

    public async Task UpdateAsync(Hold hold, CancellationToken ct = default)
    {
        await _collection.ReplaceOneAsync(x => x.Id == hold.Id, HoldMapper.ToDocument(hold), new ReplaceOptions(), ct);
    }
}
