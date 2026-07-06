using InventoryHold.Domain.Ports;
using InventoryHold.Domain.Repositories;
using InventoryHold.Infrastructure.BackgroundServices;
using InventoryHold.Infrastructure.Caching;
using InventoryHold.Infrastructure.Caching.Options;
using InventoryHold.Infrastructure.Messaging;
using InventoryHold.Infrastructure.Messaging.Options;
using InventoryHold.Infrastructure.Persistence.Options;
using InventoryHold.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using StackExchange.Redis;
using RabbitMQ.Client;

namespace InventoryHold.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<MongoDbOptions>(config.GetSection("MongoDB"));
        services.AddSingleton<IMongoClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            return new MongoClient(opts.ConnectionString);
        });
        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            return sp.GetRequiredService<IMongoClient>().GetDatabase(opts.DatabaseName);
        });
        services.AddScoped<IHoldRepository, MongoHoldRepository>();
        services.AddScoped<IInventoryRepository, MongoInventoryRepository>();

        services.Configure<RedisOptions>(config.GetSection("Redis"));
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(opts.ConnectionString);
        });
        services.AddScoped<ICacheService, RedisCacheService>();

        services.Configure<RabbitMqOptions>(config.GetSection("RabbitMQ"));
        services.AddSingleton<IConnection>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
            var factory = new ConnectionFactory
            {
                HostName = opts.Host,
                Port = opts.Port,
                UserName = opts.Username,
                Password = opts.Password,
                VirtualHost = opts.VirtualHost
            };
            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });
        services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
        services.AddHostedService<RabbitMqTopologyInitializer>();
        services.AddHostedService<ExpiredHoldCleanupService>();

        return services;
    }
}
