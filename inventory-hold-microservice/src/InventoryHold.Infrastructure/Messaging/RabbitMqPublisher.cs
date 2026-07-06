using System.Text.Json;
using InventoryHold.Domain.Ports;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace InventoryHold.Infrastructure.Messaging;

public sealed class RabbitMqPublisher : IMessagePublisher
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IConnection _connection;
    private readonly RabbitMqOptions _options;

    public RabbitMqPublisher(IConnection connection, IOptions<RabbitMqOptions> options)
    {
        _connection = connection;
        _options = options.Value;
    }

    public async Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default) where T : class
    {
        await using var channel = await _connection.CreateChannelAsync(cancellationToken: ct);

        var body = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);
        var props = channel.CreateBasicProperties();
        props.DeliveryMode = 2;
        props.ContentType = "application/json";
        props.MessageId = Guid.NewGuid().ToString();
        props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        await channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }
}
