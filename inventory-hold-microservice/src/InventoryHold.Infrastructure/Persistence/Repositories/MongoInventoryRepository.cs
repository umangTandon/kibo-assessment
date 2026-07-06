using InventoryHold.Domain.Entities;
using InventoryHold.Domain.Repositories;
using InventoryHold.Infrastructure.Persistence.Documents;
using InventoryHold.Infrastructure.Persistence.Mappers;
using MongoDB.Driver;

namespace InventoryHold.Infrastructure.Persistence.Repositories;

public sealed class MongoInventoryRepository : IInventoryRepository
{
    private readonly IMongoCollection<InventoryDocument> _collection;

    public MongoInventoryRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<InventoryDocument>("inventoryItems");
    }

    public async Task<IReadOnlyList<InventoryItem>> GetAllAsync(CancellationToken ct = default)
    {
        var documents = await _collection.Find(FilterDefinition<InventoryDocument>.Empty).ToListAsync(ct);
        return documents.Select(InventoryMapper.ToDomain).ToList();
    }

    public async Task<InventoryItem?> TryDeductStockAsync(string productId, int quantity, CancellationToken ct = default)
    {
        var filter = Builders<InventoryDocument>.Filter.And(
            Builders<InventoryDocument>.Filter.Eq(x => x.ProductId, productId),
            Builders<InventoryDocument>.Filter.Gte(x => x.AvailableStock, quantity));

        var update = Builders<InventoryDocument>.Update
            .Inc(x => x.ReservedStock, quantity)
            .Inc(x => x.AvailableStock, -quantity)
            .Inc(x => x.Version, 1);

        var options = new FindOneAndUpdateOptions<InventoryDocument> { ReturnDocument = ReturnDocument.After };
        var result = await _collection.FindOneAndUpdateAsync(filter, update, options, ct);
        return result is null ? null : InventoryMapper.ToDomain(result);
    }

    public async Task<InventoryItem> RestoreStockAsync(string productId, int quantity, CancellationToken ct = default)
    {
        var filter = Builders<InventoryDocument>.Filter.Eq(x => x.ProductId, productId);
        var update = Builders<InventoryDocument>.Update
            .Inc(x => x.ReservedStock, -quantity)
            .Inc(x => x.AvailableStock, quantity)
            .Inc(x => x.Version, 1);

        var options = new FindOneAndUpdateOptions<InventoryDocument> { ReturnDocument = ReturnDocument.After };
        var result = await _collection.FindOneAndUpdateAsync(filter, update, options, ct);
        return InventoryMapper.ToDomain(result!);
    }

    public async Task SeedAsync(IEnumerable<InventoryItem> items, CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            var filter = Builders<InventoryDocument>.Filter.Eq(x => x.ProductId, item.ProductId);
            var document = InventoryMapper.ToDocument(item);
            await _collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true }, ct);
        }
    }
}
