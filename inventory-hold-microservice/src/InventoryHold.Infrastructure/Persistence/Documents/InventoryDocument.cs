using MongoDB.Bson.Serialization.Attributes;

namespace InventoryHold.Infrastructure.Persistence.Documents;

public sealed class InventoryDocument
{
    [BsonId]
    public string ProductId { get; set; } = default!;

    [BsonElement("productName")]
    public string ProductName { get; set; } = default!;

    [BsonElement("totalStock")]
    public int TotalStock { get; set; }

    [BsonElement("reservedStock")]
    public int ReservedStock { get; set; }

    [BsonElement("availableStock")]
    public int AvailableStock { get; set; }

    [BsonElement("version")]
    public int Version { get; set; }
}
