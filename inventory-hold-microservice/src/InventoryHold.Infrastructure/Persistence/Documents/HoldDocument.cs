using InventoryHold.Contracts.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace InventoryHold.Infrastructure.Persistence.Documents;

public sealed class HoldDocument
{
    [BsonId]
    public string Id { get; set; } = default!;

    [BsonElement("productId")]
    public string ProductId { get; set; } = default!;

    [BsonElement("customerId")]
    public string? CustomerId { get; set; }

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    [BsonElement("status")]
    public HoldStatus Status { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("releasedAt")]
    public DateTime? ReleasedAt { get; set; }

    [BsonElement("version")]
    public int Version { get; set; }
}
