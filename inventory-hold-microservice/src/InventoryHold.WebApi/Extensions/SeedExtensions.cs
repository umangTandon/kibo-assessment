using InventoryHold.Domain.Entities;
using Microsoft.AspNetCore.Builder;

namespace InventoryHold.WebApi.Extensions;

public static class SeedExtensions
{
    public static async Task SeedInventoryAsync(this WebApplication app)
    {
        var serviceScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
        using var scope = serviceScopeFactory.CreateScope();
        var inventoryRepository = scope.ServiceProvider.GetRequiredService<InventoryHold.Domain.Repositories.IInventoryRepository>();

        var seedItems = new List<InventoryItem>
        {
            new() { ProductId = "prod-001", ProductName = "Widget Alpha", TotalStock = 100, ReservedStock = 0, Version = 1 },
            new() { ProductId = "prod-002", ProductName = "Gadget Beta", TotalStock = 50, ReservedStock = 0, Version = 1 },
            new() { ProductId = "prod-003", ProductName = "Doohickey Gamma", TotalStock = 200, ReservedStock = 0, Version = 1 },
            new() { ProductId = "prod-004", ProductName = "Thingamajig", TotalStock = 75, ReservedStock = 0, Version = 1 },
            new() { ProductId = "prod-005", ProductName = "Whatchamacallit", TotalStock = 30, ReservedStock = 0, Version = 1 }
        };

        await inventoryRepository.SeedAsync(seedItems);
    }
}
