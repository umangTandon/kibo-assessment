using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace InventoryHold.Infrastructure.Messaging;

public sealed class RabbitMqTopologyInitializer : IHostedService
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqTopologyInitializer> _logger;
    private const string ExchangeName = "inventory-hold.events";

    public RabbitMqTopologyInitializer(IConnection connection, ILogger<RabbitMqTopologyInitializer> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var channel = await _connection.CreateChannelAsync(new CreateChannelOptions(publisherConfirmationsEnabled: false, publisherConfirmationTrackingEnabled: false), cancellationToken);
            await channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: "inventory-hold.hold-created",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);
            await channel.QueueBindAsync("inventory-hold.hold-created", ExchangeName, "hold.created", cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: "inventory-hold.hold-released",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);
            await channel.QueueBindAsync("inventory-hold.hold-released", ExchangeName, "hold.released", cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: "inventory-hold.hold-expired",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);
            await channel.QueueBindAsync("inventory-hold.hold-expired", ExchangeName, "hold.expired", cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ topology initialization failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
